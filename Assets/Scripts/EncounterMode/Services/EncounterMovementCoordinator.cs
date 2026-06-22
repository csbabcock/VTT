using System;
using System.Collections.Generic;
using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Owns encounter movement state and the single move pipeline: budget tracking,
    /// reachable-cell calculation, local validation/deduction, network dispatch, and
    /// driving the local avatar through <see cref="IEncounterLocomotion"/>.
    ///
    /// Unity-bound lookups (player cell, locomotion, network participant) and session
    /// flags (network/turn) are injected as delegates so this class stays a plain,
    /// focused coordinator owned by <see cref="EncounterModeManager"/>.
    /// </summary>
    public sealed class EncounterMovementCoordinator
    {
        private readonly MovementTracker _movementTracker;
        private readonly IGridGenerator _gridGenerator;
        private readonly EncounterGridPresentation _presentation;
        private readonly EncounterUIAdapter _ui;

        private readonly Func<GridCell> _getPlayerCurrentCell;
        private readonly Func<IEncounterLocomotion> _getLocomotion;
        private readonly Func<IEncounterMovementClient> _getParticipant;
        private readonly Func<bool> _usesNetworkEncounter;
        private readonly Func<bool> _isLocalTurnActive;

        private HashSet<GridCell> _reachableCells = new HashSet<GridCell>();
        private bool _isMovementModeActive;

        public EncounterMovementCoordinator(
            MovementTracker movementTracker,
            IGridGenerator gridGenerator,
            EncounterGridPresentation presentation,
            EncounterUIAdapter ui,
            Func<GridCell> getPlayerCurrentCell,
            Func<IEncounterLocomotion> getLocomotion,
            Func<IEncounterMovementClient> getParticipant,
            Func<bool> usesNetworkEncounter,
            Func<bool> isLocalTurnActive)
        {
            _movementTracker = movementTracker;
            _gridGenerator = gridGenerator;
            _presentation = presentation;
            _ui = ui;
            _getPlayerCurrentCell = getPlayerCurrentCell;
            _getLocomotion = getLocomotion;
            _getParticipant = getParticipant;
            _usesNetworkEncounter = usesNetworkEncounter;
            _isLocalTurnActive = isLocalTurnActive;
        }

        public bool IsMovementModeActive => _isMovementModeActive;

        public bool IsCellReachable(GridCell cell) => cell != null && _reachableCells.Contains(cell);

        /// <summary>Resets movement state when entering encounter mode (no grid selection yet).</summary>
        public void ResetForEncounterStart()
        {
            _movementTracker.ResetDash();
            _movementTracker.ResetMovement();
            _movementTracker.SetStartingCell(null);
            _isMovementModeActive = false;
            _reachableCells.Clear();

            UpdateMovementDisplay();
            UpdateMovementButtonState();
        }

        /// <summary>Tears down grid selection visuals when leaving encounter mode.</summary>
        public void TearDownVisuals()
        {
            _presentation.DisableSelection();
            ClearReachableCells();
        }

        public void EnableGridSelection()
        {
            if (!_isLocalTurnActive())
                return;

            _movementTracker.ResetMovement();
            _isMovementModeActive = true;

            GridCell startCell = _getPlayerCurrentCell();
            _movementTracker.SetStartingCell(startCell);

            if (_usesNetworkEncounter())
            {
                _getParticipant()?.RequestBeginMovePhase();
                SyncMovementFromParticipant();
            }

            EnableGridComponents();

            UpdateMovementDisplay();
            UpdateMovementButtonState();
            RefreshReachableCells();
        }

        public void DisableMovementMode()
        {
            _isMovementModeActive = false;
            _presentation.DisableSelection();
            ClearReachableCells();
            UpdateMovementButtonState();
        }

        public void SetDashActive(bool isActive)
        {
            if (!_isLocalTurnActive())
                return;

            if (_usesNetworkEncounter() && isActive)
            {
                _getParticipant()?.RequestDash();
                return;
            }

            _movementTracker.SetDashActive(isActive, _isMovementModeActive);

            if (_isMovementModeActive && !_movementTracker.IsMovementExhausted)
            {
                EnableGridComponents();
                RefreshReachableCells();
            }
            else if (_isMovementModeActive)
            {
                _presentation.SetMaxElevation(GridDistanceRules.FeetToCells(_movementTracker.EffectiveMaxSpeed));
            }

            UpdateMovementDisplay();

            if (_isMovementModeActive)
            {
                RefreshReachableCells();
                UpdateMovementButtonState();
            }
        }

        /// <summary>Handles a grid cell selection: validates and deducts locally, or dispatches to the server.</summary>
        public void HandleCellSelected(GridCell cell, int elevation)
        {
            if (cell == null || _gridGenerator == null || !_isLocalTurnActive())
                return;

            if (_usesNetworkEncounter())
            {
                _getParticipant()?.RequestMoveTo(cell, elevation);
                return;
            }

            if (_movementTracker.IsMovementExhausted)
                return;

            GridCell startCell = _movementTracker.LastSelectedCell ?? _getPlayerCurrentCell();
            if (startCell == null)
                return;

            int distanceFeet = _movementTracker.CalculateDistanceFeet(startCell, cell);
            if (!_movementTracker.TryDeductMovement(distanceFeet, cell))
                return;

            ApplyMoveResult(cell, elevation);
        }

        /// <summary>Applies server-approved movement state on the owning client after validation.</summary>
        public void ApplyApprovedNetworkMove(GridCell cell, int elevation, int remainingFeet, bool dashActive)
        {
            // Apply the server's authoritative budget verbatim. The server already accounts for
            // Dash (it adds one base move to the remaining), so we must NOT recompute here or the
            // dash bonus gets applied twice (e.g. dashing from 0 would show 60 instead of 30).
            _movementTracker.ApplyAuthoritativeState(remainingFeet, dashActive);

            if (cell != null)
                _movementTracker.SetStartingCell(cell);

            ApplyMoveResult(cell, elevation);
        }

        private void ApplyMoveResult(GridCell cell, int elevation)
        {
            // Single move pipeline: the coordinator is the only place that drives the
            // local avatar after a validated (local) or approved (server) move.
            if (cell != null)
                _getLocomotion()?.ApplyMove(cell, elevation);

            UpdateMovementDisplay();
            RefreshReachableCells();

            if (_movementTracker.IsMovementExhausted)
            {
                _presentation.DisableSelection();
                ClearReachableCells();
            }
        }

        private void SyncMovementFromParticipant()
        {
            var participant = _getParticipant();
            if (participant == null)
                return;

            // Replicated state is authoritative; take it verbatim (no local dash recompute).
            _movementTracker.ApplyAuthoritativeState(
                participant.RemainingMovementFeet, participant.IsDashActive);
        }

        private void EnableGridComponents()
        {
            _presentation.EnableSelection(GridDistanceRules.FeetToCells(_movementTracker.EffectiveMaxSpeed));
        }

        private void RefreshReachableCells()
        {
            if (_movementTracker.IsMovementExhausted || _gridGenerator == null)
            {
                ClearReachableCells();
                return;
            }

            GridCell startCell = _movementTracker.LastSelectedCell ?? _getPlayerCurrentCell();
            if (startCell == null)
                return;

            _reachableCells = ReachableCellsCalculator.CalculateReachableCells(
                _gridGenerator, startCell, _movementTracker.RemainingMovementFeet);

            _presentation.UpdateReachableCells(_reachableCells);
        }

        private void ClearReachableCells()
        {
            _reachableCells.Clear();
            _presentation.ClearReachableCells();
        }

        /// <summary>
        /// Re-applies the live movement budget to the UI. Needed because the character sheet
        /// re-render (e.g. switching tabs) rebinds the shared speed label to the static base
        /// speed, which would otherwise clobber the remaining-movement display mid-turn.
        /// </summary>
        public void RefreshMovementDisplay()
        {
            UpdateMovementDisplay();
            UpdateMovementButtonState();
        }

        private void UpdateMovementDisplay()
            => _ui.UpdateSpeedDisplay(_movementTracker.RemainingMovementFeet, _movementTracker.EffectiveMaxSpeed);

        private void UpdateMovementButtonState()
            => _ui.UpdateMovementButtonState(_isMovementModeActive);
    }
}

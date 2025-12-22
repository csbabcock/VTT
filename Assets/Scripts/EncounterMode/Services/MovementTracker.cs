using UnityEngine;
using GameCore.EncounterMode.Grid;
using System.Collections.Generic;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Tracks movement state and calculates movement-related values.
    /// Follows Single Responsibility Principle.
    /// </summary>
    public class MovementTracker
    {
        private int _baseMovementSpeedFeet;
        private int _remainingMovementFeet;
        private GridCell _lastSelectedCell;
        private bool _isDashActive;

        public int RemainingMovementFeet => _remainingMovementFeet;
        public GridCell LastSelectedCell => _lastSelectedCell;
        public bool IsDashActive => _isDashActive;
        public int EffectiveMaxSpeed => _isDashActive ? _baseMovementSpeedFeet * 2 : _baseMovementSpeedFeet;

        public MovementTracker(int baseMovementSpeedFeet)
        {
            _baseMovementSpeedFeet = baseMovementSpeedFeet;
            _remainingMovementFeet = baseMovementSpeedFeet;
        }

        /// <summary>
        /// Resets movement to full speed (accounting for Dash).
        /// </summary>
        public void ResetMovement()
        {
            _remainingMovementFeet = EffectiveMaxSpeed;
        }

        /// <summary>
        /// Sets the last selected cell (starting position for movement).
        /// </summary>
        public void SetStartingCell(GridCell cell)
        {
            _lastSelectedCell = cell;
        }

        /// <summary>
        /// Calculates distance in feet between two cells using Manhattan distance.
        /// </summary>
        public int CalculateDistanceFeet(GridCell fromCell, GridCell toCell)
        {
            if (fromCell == null || toCell == null)
                return 0;

            int deltaX = Mathf.Abs(toCell.X - fromCell.X);
            int deltaZ = Mathf.Abs(toCell.Z - fromCell.Z);
            int cellsMoved = Mathf.Max(deltaX, deltaZ); // Diagonal = 1 cell
            return cellsMoved * 5; // Each cell is 5 feet
        }

        /// <summary>
        /// Deducts movement and updates last selected cell.
        /// </summary>
        public bool TryDeductMovement(int distanceFeet, GridCell targetCell)
        {
            if (distanceFeet > _remainingMovementFeet)
                return false;

            _remainingMovementFeet = Mathf.Max(0, _remainingMovementFeet - distanceFeet);
            _lastSelectedCell = targetCell;
            return true;
        }

        /// <summary>
        /// Sets Dash state and recalculates remaining movement.
        /// </summary>
        public void SetDashActive(bool isActive, bool wasInMovementMode)
        {
            int oldMax = _isDashActive ? _baseMovementSpeedFeet * 2 : _baseMovementSpeedFeet;
            int newMax = isActive ? _baseMovementSpeedFeet * 2 : _baseMovementSpeedFeet;

            // Special case: If movement is exhausted and activating Dash, add back full base movement
            if (isActive && !_isDashActive && _remainingMovementFeet <= 0)
            {
                _remainingMovementFeet = _baseMovementSpeedFeet;
            }
            else if (wasInMovementMode)
            {
                // Recalculate remaining movement with new max
                int usedMovement = oldMax - _remainingMovementFeet;
                _remainingMovementFeet = Mathf.Max(0, newMax - usedMovement);
            }

            _isDashActive = isActive;
        }

        /// <summary>
        /// Resets Dash state.
        /// </summary>
        public void ResetDash()
        {
            _isDashActive = false;
        }

        /// <summary>
        /// Checks if movement is exhausted.
        /// </summary>
        public bool IsMovementExhausted => _remainingMovementFeet <= 0;
    }
}


using UnityEngine;
using GameCore.Actors;
using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using GameCore.UI.InGame;
using System.Collections.Generic;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Manages the encounter mode state and coordinates grid-based combat systems.
    /// Follows SOLID principles with interface-based design.
    /// </summary>
    public class EncounterModeManager : MonoBehaviour, IEncounterModeManager
    {
        [Header("Encounter Mode Settings")]
        [Tooltip("Whether encounter mode is currently active")]
        public bool IsEncounterModeActive { get; private set; } = false;

        [Header("Grid Settings")]
        [Tooltip("Grid cell size in Unity units (default: 1.524 meters = 5 feet)")]
        public float GridCellSize = 1.524f; // 5 feet in meters

        [Tooltip("Number of grid cells in X direction")]
        public int GridWidth = 20;

        [Tooltip("Number of grid cells in Z direction")]
        public int GridHeight = 20;

        [Tooltip("Fixed world position for grid origin")]
        public Vector3 GridOriginPosition = Vector3.zero;

        [Tooltip("Layer mask for ground detection")]
        public LayerMask GroundLayerMask = 1; // Default layer

        [Header("References")]
        [Tooltip("Grid generator component")]
        public GridGenerator GridGenerator;

        [Tooltip("Grid renderer component")]
        public GridRenderer GridRenderer;

        [Tooltip("Grid selector component")]
        public GridSelector GridSelector;

        [Tooltip("Grid selection visualizer component")]
        public GridSelectionVisualizer GridSelectionVisualizer;

        [Tooltip("Grid column visualizer component")]
        public GridColumnVisualizer GridColumnVisualizer;

        [Tooltip("Grid reachable cells visualizer component")]
        public GridReachableCellsVisualizer GridReachableCellsVisualizer;

        [Tooltip("Player controller reference")]
        public PlayerController PlayerController;

        [Tooltip("In-game UI presenter reference (for showing character sheet on encounter mode)")]
        public InGameUIPresenter InGameUIPresenter;

        [Header("Movement Settings")]
        [Tooltip("Player's movement speed in feet (for calculating max elevation). Default: 30 feet = 6 cells")]
        public int PlayerMovementSpeedFeet = 30;

        /// <summary>
        /// Whether movement mode is currently active (grid selection enabled).
        /// </summary>
        public bool IsMovementModeActive => _isMovementModeActive;

        /// <summary>
        /// True when the local player may take encounter actions (move, dash). When no turn
        /// order is active, all participants may act.
        /// </summary>
        public bool IsLocalTurnActive
        {
            get
            {
                var authority = EncounterSessionLocator.Authority;
                if (authority == null || !authority.HasActiveTurnOrder)
                    return true;

                var localActor = ActorRegistry.LocalActor;
                return localActor != null && localActor.OwnerId == authority.CurrentTurnOwnerId;
            }
        }

        /// <summary>
        /// True when a networked encounter session is driving state (server-validated moves).
        /// </summary>
        public bool UsesNetworkEncounter =>
            EncounterSessionLocator.Authority != null && IsEncounterModeActive;

        private IGridGenerator _gridGenerator;
        private IGridRenderer _gridRenderer;
        private IGridSelector _gridSelector;
        
        // Services
        private MovementTracker _movementTracker;
        private bool _isMovementModeActive = false;
        private HashSet<GridCell> _reachableCells = new HashSet<GridCell>();

        #region Events
        /// <summary>
        /// Raised when encounter mode is toggled on or off.
        /// </summary>
        public System.Action<bool> OnEncounterModeToggled;
        #endregion

        private void Awake()
        {
            // Auto-find components if not assigned
            if (GridGenerator == null)
                GridGenerator = GetComponent<GridGenerator>();
            
            if (GridRenderer == null)
                GridRenderer = GetComponent<GridRenderer>();

            if (GridSelector == null)
                GridSelector = GetComponent<GridSelector>();

            if (GridSelectionVisualizer == null)
                GridSelectionVisualizer = GetComponent<GridSelectionVisualizer>();

            if (GridColumnVisualizer == null)
                GridColumnVisualizer = GetComponent<GridColumnVisualizer>();

            if (GridReachableCellsVisualizer == null)
            {
                GridReachableCellsVisualizer = GetComponent<GridReachableCellsVisualizer>();
                if (GridReachableCellsVisualizer == null && transform.parent != null)
                    GridReachableCellsVisualizer = transform.parent.GetComponent<GridReachableCellsVisualizer>();
                if (GridReachableCellsVisualizer == null)
                    GridReachableCellsVisualizer = FindAnyObjectByType<GridReachableCellsVisualizer>();
            }

            if (PlayerController == null)
                PlayerController = FindAnyObjectByType<PlayerController>();

            if (InGameUIPresenter == null)
                InGameUIPresenter = FindAnyObjectByType<InGameUIPresenter>();

            // Set interface references
            _gridGenerator = GridGenerator;
            _gridRenderer = GridRenderer;
            _gridSelector = GridSelector;

            // Initialize services
            _movementTracker = new MovementTracker(PlayerMovementSpeedFeet);
        }

        private void Start()
        {
            // Initialize grid generator
            if (_gridGenerator != null && GridGenerator != null)
            {
                GridGenerator.GenerateGrid(GridOriginPosition, GridWidth, GridHeight, GridCellSize, GroundLayerMask);
            }

            // Subscribe to grid selection events for movement tracking
            if (GridSelector != null)
            {
                GridSelector.OnCellSelected += OnCellSelected;
            }

            // Initially hide grid and disable grid selection
            _gridRenderer?.SetVisible(false);
            if (GridSelector != null)
                GridSelector.enabled = false;
            if (GridSelectionVisualizer != null)
                GridSelectionVisualizer.enabled = false;

            Initialize();
        }

        private void OnDestroy()
        {
            if (GridSelector != null)
            {
                GridSelector.OnCellSelected -= OnCellSelected;
            }
        }

        /// <summary>
        /// Initializes the encounter mode system.
        /// Called after grid generation in Start().
        /// </summary>
        public void Initialize()
        {
            // Initialization handled in Start()
        }

        /// <summary>
        /// Toggles encounter mode on or off. When a networked authority is present, delegates to it.
        /// </summary>
        public void ToggleEncounterMode()
        {
            var authority = EncounterSessionLocator.Authority;
            if (authority != null)
            {
                authority.RequestToggleEncounter();
                return;
            }

            ApplyEncounterActive(!IsEncounterModeActive);
        }

        /// <summary>
        /// Applies encounter active state from the local toggle or replicated authority.
        /// </summary>
        public void ApplyEncounterActive(bool isActive)
        {
            if (IsEncounterModeActive == isActive)
                return;

            IsEncounterModeActive = isActive;
            Debug.Log($"[EncounterCameraDebug] EncounterModeManager encounterActive={IsEncounterModeActive}, manager={name}");

            if (IsEncounterModeActive)
                EnableEncounterMode();
            else
                DisableEncounterMode();

            OnEncounterModeToggled?.Invoke(IsEncounterModeActive);
        }

        private void EnableEncounterMode()
        {
            // Show grid
            if (_gridRenderer != null)
            {
                _gridRenderer.SetVisible(true);
            }

            // Do NOT enable grid selection by default - only enable after movement button is selected
            // Grid selection will be enabled via EnableGridSelection() when movement action is clicked

            // Reset movement tracking (but don't enable grid selection yet)
            _movementTracker.ResetDash();
            _movementTracker.ResetMovement();
            _movementTracker.SetStartingCell(null);
            _isMovementModeActive = false;
            _reachableCells.Clear();
            
            UpdateMovementDisplay();
            UpdateMovementButtonState();

            // Show character sheet by default when entering encounter mode
            InGameUIPresenter?.Model?.SetCharacterSheetOpen(true);
        }

        /// <summary>
        /// Enables grid selection and visualizers. Called when a movement action is selected from the character sheet.
        /// Resets movement tracking to full speed (doubled if Dash is active).
        /// </summary>
        public void EnableGridSelection()
        {
            if (!IsEncounterModeActive || !IsLocalTurnActive)
                return;

            // Reset movement tracking
            _movementTracker.ResetMovement();
            _isMovementModeActive = true;

            // Get current player position and set as starting cell
            GridCell startCell = GetPlayerCurrentCell();
            _movementTracker.SetStartingCell(startCell);

            if (UsesNetworkEncounter)
            {
                GetLocalParticipant()?.RequestBeginMovePhase();
                SyncMovementFromParticipant();
            }

            // Enable grid components
            EnableGridComponents();

            // Update UI and visualization
            UpdateMovementDisplay();
            UpdateMovementButtonState();
            RefreshReachableCells();
        }

        /// <summary>
        /// Disables movement mode, clearing grid selection and visualizers.
        /// </summary>
        public void DisableMovementMode()
        {
            _isMovementModeActive = false;
            DisableGridComponents();
            ClearReachableCells();
            UpdateMovementButtonState();
        }

        /// <summary>
        /// Sets Dash as active, doubling movement speed for the next movement action.
        /// If movement is exhausted (0 remaining) and Dash is activated, it adds back the full base movement speed.
        /// </summary>
        public void SetDashActive(bool isActive)
        {
            if (!IsLocalTurnActive)
                return;

            if (UsesNetworkEncounter && isActive)
            {
                GetLocalParticipant()?.RequestDash();
                return;
            }

            _movementTracker.SetDashActive(isActive, _isMovementModeActive);
            
            // If we're in movement mode and now have movement remaining, re-enable grid selection
            if (_isMovementModeActive && !_movementTracker.IsMovementExhausted)
            {
                EnableGridComponents();
                RefreshReachableCells();
            }
            else if (_isMovementModeActive && GridSelector != null)
            {
                // Update max elevation even if no movement
                int maxElevation = Mathf.FloorToInt(_movementTracker.EffectiveMaxSpeed / 5f);
                GridSelector.SetMaxElevation(maxElevation);
            }
            
            UpdateMovementDisplay();
            
            if (_isMovementModeActive)
            {
                RefreshReachableCells();
                UpdateMovementButtonState();
            }
        }

        /// <summary>
        /// Handles cell selection and tracks movement distance.
        /// </summary>
        private void OnCellSelected(GridCell cell, int elevation)
        {
            if (cell == null || _gridGenerator == null || !IsLocalTurnActive)
                return;

            if (UsesNetworkEncounter)
            {
                GetLocalParticipant()?.RequestMoveTo(cell, elevation);
                return;
            }

            if (_movementTracker.IsMovementExhausted)
                return;

            // Get starting cell for distance calculation
            GridCell startCell = _movementTracker.LastSelectedCell ?? GetPlayerCurrentCell();
            if (startCell == null)
                return;

            // Calculate and deduct movement
            int distanceFeet = _movementTracker.CalculateDistanceFeet(startCell, cell);
            if (!_movementTracker.TryDeductMovement(distanceFeet, cell))
                return;

            ApplyLocalMoveResult(cell);
        }

        /// <summary>
        /// Applies server-approved movement state on the owning client after validation.
        /// </summary>
        public void ApplyApprovedNetworkMove(GridCell cell, int elevation, int remainingFeet, bool dashActive)
        {
            _movementTracker.SetRemainingMovementFeet(remainingFeet);
            _movementTracker.SetDashActive(dashActive, _isMovementModeActive);

            if (cell != null)
                _movementTracker.SetStartingCell(cell);

            ApplyLocalMoveResult(cell);
        }

        private void ApplyLocalMoveResult(GridCell cell)
        {
            UpdateMovementDisplay();
            RefreshReachableCells();

            if (_movementTracker.IsMovementExhausted)
            {
                DisableGridComponents();
                ClearReachableCells();
            }
        }

        /// <summary>
        /// Updates the movement display in the UI.
        /// </summary>
        private void UpdateMovementDisplay()
        {
            if (InGameUIPresenter?.View != null)
            {
                InGameUIPresenter.View.UpdateSpeedDisplay(
                    _movementTracker.RemainingMovementFeet, 
                    _movementTracker.EffectiveMaxSpeed);
            }
        }

        /// <summary>
        /// Updates the movement button state (Move/Cancel) and visual indicator.
        /// </summary>
        private void UpdateMovementButtonState()
        {
            InGameUIPresenter?.View?.UpdateMovementButtonState(_isMovementModeActive);
        }

        /// <summary>
        /// Refreshes reachable cells calculation and visualization.
        /// </summary>
        private void RefreshReachableCells()
        {
            if (_movementTracker.IsMovementExhausted || _gridGenerator == null)
            {
                ClearReachableCells();
                return;
            }

            GridCell startCell = _movementTracker.LastSelectedCell ?? GetPlayerCurrentCell();
            if (startCell == null)
                return;

            _reachableCells = ReachableCellsCalculator.CalculateReachableCells(
                _gridGenerator,
                startCell,
                _movementTracker.RemainingMovementFeet);

            if (GridReachableCellsVisualizer != null && GridReachableCellsVisualizer.enabled)
            {
                GridReachableCellsVisualizer.UpdateReachableCells(_reachableCells);
            }
        }

        /// <summary>
        /// Clears reachable cells visualization.
        /// </summary>
        private void ClearReachableCells()
        {
            _reachableCells.Clear();
            if (GridReachableCellsVisualizer != null)
            {
                GridReachableCellsVisualizer.ClearReachableCells();
            }
        }

        /// <summary>
        /// Checks if a cell is reachable with current remaining movement.
        /// </summary>
        public bool IsCellReachable(GridCell cell)
        {
            return cell != null && _reachableCells.Contains(cell);
        }

        #region Helper Methods

        private void SyncMovementFromParticipant()
        {
            var participant = GetLocalParticipant();
            if (participant == null)
                return;

            _movementTracker.SetRemainingMovementFeet(participant.RemainingMovementFeet);
            _movementTracker.SetDashActive(participant.IsDashActive, _isMovementModeActive);
        }

        private static IEncounterMovementClient GetLocalParticipant()
        {
            var localActor = ActorRegistry.LocalActor;
            if (localActor?.Transform == null)
                return null;

            var components = localActor.Transform.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IEncounterMovementClient client)
                    return client;
            }

            return null;
        }

        /// <summary>
        /// Gets the player's current grid cell position.
        /// </summary>
        private GridCell GetPlayerCurrentCell()
        {
            if (GridGenerator == null)
                return null;

            // Prefer the registered local actor's position; fall back to the directly
            // referenced PlayerController so this keeps working before actors are wired up.
            Transform playerTransform = GameCore.Actors.ActorRegistry.LocalActor?.Transform
                ?? (PlayerController != null ? PlayerController.transform : null);
            if (playerTransform == null)
                return null;

            return GridGenerator.GetCellAtWorldPosition(playerTransform.position);
        }

        /// <summary>
        /// Enables all grid selection components.
        /// </summary>
        private void EnableGridComponents()
        {
            int maxElevation = Mathf.FloorToInt(_movementTracker.EffectiveMaxSpeed / 5f);

            if (GridSelector != null)
            {
                GridSelector.SetMaxElevation(maxElevation);
                GridSelector.enabled = true;
            }

            if (GridSelectionVisualizer != null)
                GridSelectionVisualizer.enabled = true;

            if (GridColumnVisualizer != null)
                GridColumnVisualizer.enabled = true;

            if (GridReachableCellsVisualizer != null)
                GridReachableCellsVisualizer.enabled = true;
        }

        /// <summary>
        /// Disables all grid selection components.
        /// </summary>
        private void DisableGridComponents()
        {
            if (GridSelector != null)
            {
                GridSelector.ClearSelection();
                GridSelector.enabled = false;
            }

            if (GridSelectionVisualizer != null)
            {
                GridSelectionVisualizer.HideAllIndicators();
                GridSelectionVisualizer.enabled = false;
            }

            if (GridColumnVisualizer != null)
                GridColumnVisualizer.enabled = false;

            if (GridReachableCellsVisualizer != null)
            {
                GridReachableCellsVisualizer.ClearReachableCells();
                GridReachableCellsVisualizer.enabled = false;
            }
        }

        #endregion

        private void DisableEncounterMode()
        {
            // Hide grid
            _gridRenderer?.SetVisible(false);

            // Disable grid selection and clear state
            DisableGridComponents();
            ClearReachableCells();

            // Close character sheet by default when exiting encounter mode
            InGameUIPresenter?.Model?.SetCharacterSheetOpen(false);
        }
    }
}


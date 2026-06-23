using UnityEngine;
using GameCore.Actors;
using GameCore.Combat.Targeting;
using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using GameCore.UI.InGame;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Thin MonoBehaviour wiring layer for encounter mode. Owns the encounter on/off state
    /// machine and turn-gating, and delegates movement, grid presentation, and UI updates
    /// to focused services (<see cref="EncounterMovementCoordinator"/>,
    /// <see cref="EncounterGridPresentation"/>, <see cref="EncounterUIAdapter"/>).
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

        /// <summary>Whether movement mode is currently active (grid selection enabled).</summary>
        public bool IsMovementModeActive => _coordinator != null && _coordinator.IsMovementModeActive;

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

        /// <summary>True when a networked encounter session is driving state (server-validated moves).</summary>
        public bool UsesNetworkEncounter =>
            EncounterSessionLocator.Authority != null && IsEncounterModeActive;

        private IGridGenerator _gridGenerator;
        private IGridRenderer _gridRenderer;

        private MovementTracker _movementTracker;
        private EncounterGridPresentation _presentation;
        private EncounterUIAdapter _ui;
        private EncounterMovementCoordinator _coordinator;

        /// <summary>Raised when encounter mode is toggled on or off.</summary>
        public System.Action<bool> OnEncounterModeToggled;

        private void Awake()
        {
            ResolveComponents();

            _gridGenerator = GridGenerator;
            _gridRenderer = GridRenderer;

            _movementTracker = new MovementTracker(PlayerMovementSpeedFeet);
            _presentation = new EncounterGridPresentation(
                _gridRenderer, GridSelector, GridSelectionVisualizer,
                GridColumnVisualizer, GridReachableCellsVisualizer);
            _ui = new EncounterUIAdapter(InGameUIPresenter);
            _coordinator = new EncounterMovementCoordinator(
                _movementTracker,
                _gridGenerator,
                _presentation,
                _ui,
                GetPlayerCurrentCell,
                GetLocalLocomotion,
                GetLocalParticipant,
                () => UsesNetworkEncounter,
                () => IsLocalTurnActive);
        }

        private void ResolveComponents()
        {
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
        }

        private void Start()
        {
            EnsureGridGenerated();

            if (GridSelector != null)
                GridSelector.OnCellSelected += HandleCellSelected;

            _presentation.ApplyStartupState(IsEncounterModeActive);
        }

        /// <summary>
        /// Generates the encounter grid when it has not been built yet. Safe to call before
        /// <see cref="Start"/> (e.g. when replicated encounter state arrives during scene sync).
        /// </summary>
        public void EnsureGridGenerated()
        {
            if (GridGenerator == null)
                ResolveComponents();

            if (GridGenerator == null || GridGenerator.Grid != null)
            {
                RefreshGridPresentationIfActive();
                return;
            }

            GridGenerator.GenerateGrid(GridOriginPosition, GridWidth, GridHeight, GridCellSize, GroundLayerMask);
            RefreshGridPresentationIfActive();
        }

        private void RefreshGridPresentationIfActive()
        {
            if (!EncounterGridStartupPolicy.ShouldRefreshPresentation(IsEncounterModeActive)
                || _presentation == null)
            {
                return;
            }

            _presentation.SetGridVisible(true);
        }

        /// <summary>
        /// Places an actor on the nearest grid cell ground. Used when a player joins mid-encounter.
        /// </summary>
        public void SnapTransformToGridGround(Transform target)
        {
            if (target == null)
                return;

            EnsureGridGenerated();
            if (GridGenerator == null)
                return;

            Vector3? snapped = EncounterGridSnapUtility.ResolveSnapPosition(
                GridGenerator,
                target.position,
                GridWidth / 2,
                GridHeight / 2);

            if (!snapped.HasValue)
                return;

            var characterController = target.GetComponent<CharacterController>();
            if (characterController != null)
                characterController.enabled = false;

            target.position = snapped.Value;

            if (characterController != null)
                characterController.enabled = true;
        }

        private void OnDestroy()
        {
            if (GridSelector != null)
                GridSelector.OnCellSelected -= HandleCellSelected;
        }

        /// <summary>Toggles encounter mode. When a networked authority is present, delegates to it.</summary>
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

        /// <summary>Applies encounter active state from the local toggle or replicated authority.</summary>
        public void ApplyEncounterActive(bool isActive)
        {
            if (IsEncounterModeActive == isActive)
                return;

            IsEncounterModeActive = isActive;

            if (IsEncounterModeActive)
                EnableEncounterMode();
            else
                DisableEncounterMode();

            OnEncounterModeToggled?.Invoke(IsEncounterModeActive);
        }

        private void EnableEncounterMode()
        {
            EnsureGridGenerated();
            _presentation.SetGridVisible(true);

            // Grid selection is enabled later via EnableGridSelection() when the move action is chosen.
            _coordinator.ResetForEncounterStart();

            var localActor = ActorRegistry.LocalActor;
            if (localActor?.Transform != null)
                SnapTransformToGridGround(localActor.Transform);

            // Show character sheet by default when entering encounter mode.
            _ui.SetCharacterSheetOpen(true);
        }

        private void DisableEncounterMode()
        {
            _presentation.SetGridVisible(false);
            _coordinator.TearDownVisuals();
            _ui.SetCharacterSheetOpen(false);
        }

        /// <summary>Enables grid selection. Called when a movement action is chosen from the character sheet.</summary>
        public void EnableGridSelection()
        {
            if (!IsEncounterModeActive)
                return;

            _coordinator.EnableGridSelection();
        }

        public void DisableMovementMode() => _coordinator.DisableMovementMode();

        public void SetDashActive(bool isActive) => _coordinator.SetDashActive(isActive);

        /// <summary>Applies server-approved movement state on the owning client after validation.</summary>
        public void ApplyApprovedNetworkMove(GridCell cell, int elevation, int remainingFeet, bool dashActive)
            => _coordinator.ApplyApprovedNetworkMove(cell, elevation, remainingFeet, dashActive);

        public bool IsCellReachable(GridCell cell) => _coordinator != null && _coordinator.IsCellReachable(cell);

        public void RefreshMovementDisplay() => _coordinator?.RefreshMovementDisplay();

        public bool TryApproachMeleeRange(Transform targetTransform)
        {
            if (!IsEncounterModeActive || GridGenerator == null || targetTransform == null)
                return false;

            GridCell targetCell = GridGenerator.GetCellAtWorldPosition(targetTransform.position);
            if (targetCell == null)
                return false;

            return _coordinator != null && _coordinator.TryApproachMeleeRange(targetCell, targetTransform);
        }

        public bool IsWithinMeleeRange(Transform attacker, Transform target)
        {
            if (attacker == null || target == null)
                return false;

            return MeleeRangeQuery.IsWithinMeleeReach(attacker, target, GridGenerator, GridCellSize);
        }

        private void HandleCellSelected(GridCell cell, int elevation)
            => _coordinator.HandleCellSelected(cell, elevation);

        /// <summary>Resolves the locomotion target for the local player's avatar.</summary>
        private IEncounterLocomotion GetLocalLocomotion()
        {
            var localActor = ActorRegistry.LocalActor;
            if (localActor?.Transform != null)
            {
                var locomotion = localActor.Transform.GetComponent<IEncounterLocomotion>();
                if (locomotion != null)
                    return locomotion;
            }

            return PlayerController;
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

        /// <summary>Gets the player's current grid cell position.</summary>
        private GridCell GetPlayerCurrentCell()
        {
            if (GridGenerator == null)
                return null;

            Transform playerTransform = ActorRegistry.LocalActor?.Transform
                ?? (PlayerController != null ? PlayerController.transform : null);
            if (playerTransform == null)
                return null;

            return GridGenerator.GetCellAtWorldPosition(playerTransform.position);
        }
    }
}

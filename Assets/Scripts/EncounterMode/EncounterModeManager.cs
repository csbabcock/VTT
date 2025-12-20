using UnityEngine;
using GameCore.EncounterMode.Grid;

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

        [Tooltip("Player controller reference")]
        public PlayerController PlayerController;

        private PlayerInputs _playerInputs;
        private IGridGenerator _gridGenerator;
        private IGridRenderer _gridRenderer;

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

            if (PlayerController == null)
                PlayerController = FindFirstObjectByType<PlayerController>();

            _playerInputs = PlayerController?.GetComponent<PlayerInputs>();

            // Set interface references
            _gridGenerator = GridGenerator;
            _gridRenderer = GridRenderer;
        }

        private void Start()
        {
            // Subscribe to input events
            if (_playerInputs != null)
            {
                _playerInputs.OnToggleEncounterMode += ToggleEncounterMode;
            }

            // Initialize grid generator
            if (_gridGenerator != null && GridGenerator != null)
            {
                GridGenerator.GenerateGrid(GridOriginPosition, GridWidth, GridHeight, GridCellSize, GroundLayerMask);
            }

            // Initially hide grid
            if (_gridRenderer != null)
            {
                _gridRenderer.SetVisible(false);
            }

            Initialize();
        }

        private void OnDestroy()
        {
            if (_playerInputs != null)
            {
                _playerInputs.OnToggleEncounterMode -= ToggleEncounterMode;
            }
        }

        /// <summary>
        /// Initializes the encounter mode system.
        /// </summary>
        public void Initialize()
        {
            // Grid is generated in Start()
            // This method exists for interface compliance
        }

        /// <summary>
        /// Toggles encounter mode on or off.
        /// </summary>
        public void ToggleEncounterMode()
        {
            IsEncounterModeActive = !IsEncounterModeActive;
            
            if (IsEncounterModeActive)
            {
                EnableEncounterMode();
            }
            else
            {
                DisableEncounterMode();
            }

            OnEncounterModeToggled?.Invoke(IsEncounterModeActive);
        }

        private void EnableEncounterMode()
        {
            // Show grid
            if (_gridRenderer != null)
            {
                _gridRenderer.SetVisible(true);
            }

            Debug.Log("Encounter mode enabled");
        }

        private void DisableEncounterMode()
        {
            // Hide grid
            if (_gridRenderer != null)
            {
                _gridRenderer.SetVisible(false);
            }

            Debug.Log("Encounter mode disabled");
        }
    }
}


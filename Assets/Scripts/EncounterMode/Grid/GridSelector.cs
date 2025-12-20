using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Handles grid cell selection using plane projection for accurate far-away detection.
    /// </summary>
    public class GridSelector : MonoBehaviour, IGridSelector
    {
        [Header("Selection Settings")]
        [Tooltip("Camera used for mouse projection")]
        public Camera SelectionCamera;

        [Tooltip("Plane height offset from grid origin (for elevation support)")]
        public float PlaneHeightOffset = 0f;

        [Header("Elevation Settings")]
        [Tooltip("Maximum elevation levels (based on movement speed). Each level = 1 cell height (5 feet)")]
        public int MaxElevation = 6; // Default: 30 feet (6 cells * 5 feet)

        private IGridGenerator _gridGenerator;
        private GridCell _selectedCell;
        private GridCell _hoveredCell;
        private int _selectedElevation = 0; // Current selected elevation level (0 = ground)
#if ENABLE_INPUT_SYSTEM
        private UnityEngine.InputSystem.Mouse _mouse; // Cached mouse reference
#endif

        public GridCell SelectedCell => _selectedCell;
        public GridCell HoveredCell => _hoveredCell;
        public int SelectedElevation => _selectedElevation;
        int IGridSelector.MaxElevation => MaxElevation;

        #region Events
        /// <summary>
        /// Raised when a grid cell is selected (clicked).
        /// Parameters: selected cell, elevation level
        /// </summary>
        public System.Action<GridCell, int> OnCellSelected;
        #endregion

        private void Awake()
        {
            // Find grid generator
            _gridGenerator = GetComponent<IGridGenerator>();
            if (_gridGenerator == null && transform.parent != null)
                _gridGenerator = transform.parent.GetComponent<IGridGenerator>();
            if (_gridGenerator == null)
                _gridGenerator = FindFirstObjectByType<GridGenerator>();

            // Find camera if not assigned
            if (SelectionCamera == null)
            {
                SelectionCamera = Camera.main;
                if (SelectionCamera == null)
                    SelectionCamera = FindFirstObjectByType<Camera>();
            }

#if ENABLE_INPUT_SYSTEM
            // Cache mouse reference for performance
            _mouse = UnityEngine.InputSystem.Mouse.current;
#endif
        }

        private void Update()
        {
            if (enabled)
            {
                UpdateSelection();
            }
            else
            {
                // Clear selection when disabled
                _hoveredCell = null;
                _selectedCell = null;
                _selectedElevation = 0;
            }
        }

        /// <summary>
        /// Updates the hovered cell based on mouse position.
        /// Should be called every frame when selection is active.
        /// </summary>
        public void UpdateSelection()
        {
            if (_gridGenerator == null || SelectionCamera == null)
                return;

            // Get mouse position
            Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
            if (_mouse != null)
            {
                mousePosition = _mouse.position.ReadValue();
            }
            else
            {
                _hoveredCell = null;
                return;
            }
#else
            mousePosition = Input.mousePosition;
#endif

            // Project mouse to plane at grid Y level (ground level)
            float gridY = _gridGenerator.GridOrigin.y + PlaneHeightOffset;
            Vector3 worldPos = ProjectMouseToPlane(mousePosition, gridY);

            // Get cell at projected position (ground level)
            GridCell groundCell = _gridGenerator.GetCellAtWorldPosition(worldPos);
            
            // Handle mouse wheel for elevation selection
            float scrollDelta = 0f;
#if ENABLE_INPUT_SYSTEM
            if (_mouse != null)
            {
                scrollDelta = _mouse.scroll.ReadValue().y;
            }
#else
            scrollDelta = Input.mouseScrollDelta.y;
#endif

            if (scrollDelta > 0f)
            {
                // Scroll up - increase elevation
                _selectedElevation = Mathf.Min(_selectedElevation + 1, MaxElevation);
            }
            else if (scrollDelta < 0f)
            {
                // Scroll down - decrease elevation
                _selectedElevation = Mathf.Max(_selectedElevation - 1, 0);
            }

            // Update hovered cell with elevation
            if (groundCell != null)
            {
                _hoveredCell = groundCell;
                // Set elevation level on the hovered cell
                _hoveredCell.ElevationLevel = _selectedElevation;
            }
            else
            {
                _hoveredCell = null;
            }

            // Handle mouse click to select
            bool mouseClicked = false;
#if ENABLE_INPUT_SYSTEM
            if (_mouse != null && _mouse.leftButton.wasPressedThisFrame)
            {
                mouseClicked = true;
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                mouseClicked = true;
            }
#endif

            if (mouseClicked && _hoveredCell != null)
            {
                SelectHoveredCell();
            }
        }

        /// <summary>
        /// Selects the currently hovered cell at the current elevation.
        /// </summary>
        public void SelectHoveredCell()
        {
            if (_hoveredCell != null)
            {
                _selectedCell = _hoveredCell;
                // Ensure selected cell has the elevation level
                _selectedCell.ElevationLevel = _selectedElevation;
                
                // Raise event for movement system
                OnCellSelected?.Invoke(_selectedCell, _selectedElevation);
            }
        }

        /// <summary>
        /// Clears the current selection and hover state.
        /// </summary>
        public void ClearSelection()
        {
            _selectedCell = null;
            _hoveredCell = null;
            _selectedElevation = 0;
        }

        /// <summary>
        /// Sets the maximum elevation based on movement speed.
        /// </summary>
        public void SetMaxElevation(int maxElevation)
        {
            MaxElevation = Mathf.Max(0, maxElevation);
            _selectedElevation = Mathf.Min(_selectedElevation, MaxElevation);
        }

        private void OnDisable()
        {
            // Clear selection when component is disabled
            ClearSelection();
        }

        /// <summary>
        /// Projects mouse position onto a plane at the specified Y height.
        /// This provides accurate selection even at far distances.
        /// </summary>
        private Vector3 ProjectMouseToPlane(Vector2 mousePosition, float planeY)
        {
            // Create a ray from camera through mouse position
            Ray ray = SelectionCamera.ScreenPointToRay(mousePosition);

            // Create a plane at the grid Y level
            Plane plane = new Plane(Vector3.up, new Vector3(0, planeY, 0));

            // Find intersection point
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            // Fallback: if ray doesn't intersect (camera looking away from plane),
            // return a point on the plane directly below the camera
            Vector3 cameraPos = SelectionCamera.transform.position;
            return new Vector3(cameraPos.x, planeY, cameraPos.z);
        }
    }
}


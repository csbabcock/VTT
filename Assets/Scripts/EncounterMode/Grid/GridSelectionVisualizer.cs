using UnityEngine;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Provides visual feedback for grid cell selection and hover states.
    /// </summary>
    public class GridSelectionVisualizer : MonoBehaviour
    {
        [Header("Visual Settings")]
        [Tooltip("Material for hovered cell highlight")]
        public Material HoveredCellMaterial;

        [Tooltip("Material for selected cell highlight")]
        public Material SelectedCellMaterial;

        [Tooltip("Color for hovered cell")]
        public Color HoveredCellColor = new Color(0.3f, 0.5f, 1f, 0.5f); // Light blue

        [Tooltip("Color for selected cell")]
        public Color SelectedCellColor = new Color(0.2f, 1f, 0.2f, 0.7f); // Light green

        [Tooltip("Height offset for visual indicators above grid (increased to prevent z-fighting)")]
        public float VisualHeightOffset = 0.08f;

        [Tooltip("Size of the visual indicator relative to cell size")]
        [Range(0.8f, 1.0f)]
        public float IndicatorSize = 0.95f;

        private IGridSelector _gridSelector;
        private IGridGenerator _gridGenerator;
        private EncounterModeManager _encounterModeManager;
        private GameObject _hoveredIndicator;
        private GameObject _selectedIndicator;

        private void Awake()
        {
            // Find grid selector
            _gridSelector = GetComponent<IGridSelector>();
            if (_gridSelector == null && transform.parent != null)
                _gridSelector = transform.parent.GetComponent<IGridSelector>();
            if (_gridSelector == null)
                _gridSelector = FindFirstObjectByType<GridSelector>();

            // Find grid generator
            _gridGenerator = GetComponent<IGridGenerator>();
            if (_gridGenerator == null && transform.parent != null)
                _gridGenerator = transform.parent.GetComponent<IGridGenerator>();
            if (_gridGenerator == null)
                _gridGenerator = FindFirstObjectByType<GridGenerator>();

            // Find encounter mode manager for reachability checks
            _encounterModeManager = FindFirstObjectByType<EncounterModeManager>();
        }

        private void Update()
        {
            // Always hide indicators when component is disabled
            if (!enabled)
            {
                if (_hoveredIndicator != null)
                    _hoveredIndicator.SetActive(false);
                if (_selectedIndicator != null)
                    _selectedIndicator.SetActive(false);
                return;
            }

            if (_gridSelector == null || _gridGenerator == null)
            {
                if (_hoveredIndicator != null)
                    _hoveredIndicator.SetActive(false);
                if (_selectedIndicator != null)
                    _selectedIndicator.SetActive(false);
                return;
            }

            UpdateHoveredIndicator();
            UpdateSelectedIndicator();
        }

        private void UpdateHoveredIndicator()
        {
            GridCell hoveredCell = _gridSelector?.HoveredCell;

            // Only show hover indicator if cell is reachable (when in movement mode)
            bool shouldShowHover = hoveredCell != null;
            if (shouldShowHover && _encounterModeManager != null && _encounterModeManager.IsMovementModeActive)
            {
                shouldShowHover = _encounterModeManager.IsCellReachable(hoveredCell);
            }

            if (shouldShowHover)
            {
                if (_hoveredIndicator == null)
                {
                    _hoveredIndicator = CreateCellIndicator("HoveredCellIndicator", HoveredCellColor, HoveredCellMaterial);
                }

                UpdateIndicatorPosition(_hoveredIndicator, hoveredCell);
                _hoveredIndicator.SetActive(true);
            }
            else
            {
                if (_hoveredIndicator != null)
                {
                    _hoveredIndicator.SetActive(false);
                }
            }
        }

        private void UpdateSelectedIndicator()
        {
            GridCell selectedCell = _gridSelector?.SelectedCell;

            if (selectedCell != null)
            {
                if (_selectedIndicator == null)
                {
                    _selectedIndicator = CreateCellIndicator("SelectedCellIndicator", SelectedCellColor, SelectedCellMaterial);
                }

                UpdateIndicatorPosition(_selectedIndicator, selectedCell);
                _selectedIndicator.SetActive(true);
            }
            else
            {
                if (_selectedIndicator != null)
                {
                    _selectedIndicator.SetActive(false);
                }
            }
        }

        private GameObject CreateCellIndicator(string name, Color color, Material material)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = name;
            indicator.transform.SetParent(transform);
            indicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Face up

            // Remove collider (we don't need it)
            Collider collider = indicator.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            // Set material
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (material != null)
                {
                    renderer.material = material;
                }
                else
                {
                    // Use transparent material for proper alpha blending and render-on-top visibility
                    Material newMaterial = MaterialHelper.CreateMaterial(color, true);
                    if (newMaterial != null)
                    {
                        renderer.material = newMaterial;
                    }
                }

                // Ensure color is set
                if (renderer.material != null)
                {
                    renderer.material.color = color;
                    // Transparent materials need explicit render queue for proper draw order
                    renderer.material.renderQueue = 3000;
                }
            }

            return indicator;
        }

        private void UpdateIndicatorPosition(GameObject indicator, GridCell cell)
        {
            if (indicator == null || cell == null || _gridGenerator == null)
                return;

            float cellSize = _gridGenerator.CellSize;
            Vector3 position = cell.WorldPosition;
            position.y += VisualHeightOffset;

            indicator.transform.position = position;
            indicator.transform.localScale = Vector3.one * (cellSize * IndicatorSize);
        }

        /// <summary>
        /// Hides all visual indicators. Called when encounter mode is disabled.
        /// </summary>
        public void HideAllIndicators()
        {
            if (_hoveredIndicator != null)
                _hoveredIndicator.SetActive(false);
            if (_selectedIndicator != null)
                _selectedIndicator.SetActive(false);
        }

        private void OnDisable()
        {
            // Hide indicators when component is disabled
            HideAllIndicators();
        }

        private void OnDestroy()
        {
            if (_hoveredIndicator != null)
                Destroy(_hoveredIndicator);
            if (_selectedIndicator != null)
                Destroy(_selectedIndicator);
        }
    }
}


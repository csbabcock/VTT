using UnityEngine;
using System.Collections.Generic;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Visualizes all cells that are currently reachable with remaining movement.
    /// </summary>
    public class GridReachableCellsVisualizer : MonoBehaviour
    {
        [Header("Visual Settings")]
        [Tooltip("Material for reachable cells highlight")]
        public Material ReachableCellMaterial;

        [Tooltip("Color for reachable cells")]
        public Color ReachableCellColor = new Color(0.2f, 0.8f, 0.2f, 0.4f); // Light green, semi-transparent (increased alpha for better visibility)

        [Tooltip("Height offset for visual indicators above grid")]
        public float VisualHeightOffset = 0.02f; // Increased for better visibility

        [Tooltip("Size of the visual indicator relative to cell size")]
        [Range(0.8f, 1.0f)]
        public float IndicatorSize = 0.95f;

        private IGridGenerator _gridGenerator;
        private Dictionary<GridCell, GameObject> _reachableIndicators = new Dictionary<GridCell, GameObject>();
        private HashSet<GridCell> _currentReachableCells = new HashSet<GridCell>();

        private void Awake()
        {
            // Find grid generator
            _gridGenerator = GetComponent<IGridGenerator>();
            if (_gridGenerator == null && transform.parent != null)
                _gridGenerator = transform.parent.GetComponent<IGridGenerator>();
            if (_gridGenerator == null)
                _gridGenerator = FindAnyObjectByType<GridGenerator>();
        }

        /// <summary>
        /// Updates the visualization to show only the specified reachable cells.
        /// </summary>
        public void UpdateReachableCells(HashSet<GridCell> reachableCells)
        {
            if (_gridGenerator == null)
                return;

            // Remove indicators for cells that are no longer reachable
            var cellsToRemove = new List<GridCell>();
            foreach (var cell in _currentReachableCells)
            {
                if (!reachableCells.Contains(cell))
                {
                    cellsToRemove.Add(cell);
                }
            }

            foreach (var cell in cellsToRemove)
            {
                if (_reachableIndicators.TryGetValue(cell, out GameObject indicator))
                {
                    if (indicator != null)
                    {
                        indicator.SetActive(false);
                    }
                    _reachableIndicators.Remove(cell);
                }
                _currentReachableCells.Remove(cell);
            }

            // Add indicators for newly reachable cells
            foreach (var cell in reachableCells)
            {
                if (!_currentReachableCells.Contains(cell))
                {
                    GameObject indicator = CreateCellIndicator(cell);
                    if (indicator != null)
                    {
                        _reachableIndicators[cell] = indicator;
                        _currentReachableCells.Add(cell);
                    }
                }
                else if (_reachableIndicators.TryGetValue(cell, out GameObject existingIndicator))
                {
                    // Ensure existing indicator is active
                    if (existingIndicator != null)
                    {
                        existingIndicator.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Clears all reachable cell indicators.
        /// </summary>
        public void ClearReachableCells()
        {
            foreach (var indicator in _reachableIndicators.Values)
            {
                if (indicator != null)
                {
                    indicator.SetActive(false);
                }
            }
            _reachableIndicators.Clear();
            _currentReachableCells.Clear();
        }

        private GameObject CreateCellIndicator(GridCell cell)
        {
            if (cell == null || _gridGenerator == null)
                return null;

            float cellSize = _gridGenerator.CellSize;
            Vector3 position = cell.WorldPosition;
            position.y += VisualHeightOffset;

            // Create a quad to show the reachable cell
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = $"ReachableCell_{cell.X}_{cell.Z}";
            indicator.transform.SetParent(transform); // Parent to visualizer for organization
            indicator.transform.position = position;
            indicator.transform.rotation = Quaternion.Euler(90, 0, 0); // Face up
            indicator.transform.localScale = Vector3.one * (cellSize * IndicatorSize);

            // Set material and color
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (ReachableCellMaterial != null)
                {
                    renderer.material = ReachableCellMaterial;
                }
                else
                {
                    // Create a transparent material using MaterialHelper
                    Material newMaterial = MaterialHelper.CreateMaterial(ReachableCellColor, true);
                    if (newMaterial != null)
                    {
                        renderer.material = newMaterial;
                    }
                }

                // Set color
                if (renderer.material != null)
                {
                    renderer.material.color = ReachableCellColor;
                }
            }

            // Remove collider (we don't need it for visualization)
            Collider collider = indicator.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return indicator;
        }

        private void OnDisable()
        {
            ClearReachableCells();
        }

        private void OnDestroy()
        {
            foreach (var indicator in _reachableIndicators.Values)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
            _reachableIndicators.Clear();
            _currentReachableCells.Clear();
        }
    }
}


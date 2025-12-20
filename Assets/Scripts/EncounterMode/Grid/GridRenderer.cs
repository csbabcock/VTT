using UnityEngine;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Renders the grid visualization for encounter mode.
    /// Uses hide/show approach for performance - grid persists in memory.
    /// </summary>
    public class GridRenderer : MonoBehaviour, IGridRenderer
    {
        [Header("Rendering Settings")]
        [Tooltip("Material for grid lines")]
        public Material GridLineMaterial;

        [Tooltip("Color of grid lines")]
        public Color GridLineColor = new Color(1f, 1f, 1f, 0.8f);

        [Tooltip("Line width for grid rendering")]
        public float LineWidth = 0.05f;

        private IGridGenerator _gridGenerator;
        private LineRenderer[] _lineRenderers;
        private bool _isVisible = false;

        private void Awake()
        {
            // Try to find grid generator
            _gridGenerator = GetComponent<IGridGenerator>();
            if (_gridGenerator == null && transform.parent != null)
                _gridGenerator = transform.parent.GetComponent<IGridGenerator>();
            if (_gridGenerator == null)
                _gridGenerator = FindFirstObjectByType<GridGenerator>();
        }

        private void Start()
        {
            // Create default material if none assigned
            if (GridLineMaterial == null)
            {
                // Try to use a simple unlit shader that works in URP
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                
                if (shader != null)
                {
                    GridLineMaterial = new Material(shader);
                    GridLineMaterial.color = GridLineColor;
                }
            }
        }

        /// <summary>
        /// Sets the visibility of the grid.
        /// </summary>
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            UpdateVisualization();
        }

        /// <summary>
        /// Updates the grid visualization based on current grid state.
        /// </summary>
        public void UpdateVisualization()
        {
            if (_gridGenerator == null)
            {
                Debug.LogWarning("GridRenderer: GridGenerator is null!");
                return;
            }
            
            if (_gridGenerator.Grid == null)
            {
                Debug.LogWarning("GridRenderer: Grid is null! Make sure grid is generated first.");
                return;
            }

            if (!_isVisible)
            {
                HideGridLines();
                return;
            }

            DrawGrid();
        }

        private void DrawGrid()
        {
            var grid = _gridGenerator.Grid;
            if (grid == null)
            {
                Debug.LogWarning("GridRenderer: Cannot draw grid - grid is null!");
                return;
            }

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            float cellSize = _gridGenerator.CellSize;
            Vector3 gridOrigin = _gridGenerator.GridOrigin;

            // Calculate grid bounds
            float gridWidthWorld = width * cellSize;
            float gridHeightWorld = height * cellSize;
            Vector3 bottomLeft = gridOrigin - new Vector3(gridWidthWorld * 0.5f, 0, gridHeightWorld * 0.5f);

            // Calculate total number of lines needed
            int horizontalLines = height + 1;
            int verticalLines = width + 1;
            int totalLines = horizontalLines + verticalLines;

            // Clear existing lines
            ClearGridLines();

            // Create line renderers
            _lineRenderers = new LineRenderer[totalLines];
            int lineIndex = 0;

            // Draw horizontal lines (along X axis)
            for (int z = 0; z <= height; z++)
            {
                // Get the Y position from the first cell in this row if available
                float yPos = 0.01f;
                if (z < height && width > 0)
                {
                    var cell = grid[0, z];
                    if (cell != null)
                    {
                        yPos = cell.WorldPosition.y + 0.01f;
                    }
                }

                Vector3 startPos = bottomLeft + new Vector3(0, yPos, z * cellSize);
                Vector3 endPos = startPos + new Vector3(gridWidthWorld, 0, 0);

                _lineRenderers[lineIndex] = CreateLineRenderer($"GridLine_H_{z}", startPos, endPos);
                lineIndex++;
            }

            // Draw vertical lines (along Z axis)
            for (int x = 0; x <= width; x++)
            {
                // Get the Y position from the first cell in this column if available
                float yPos = 0.01f;
                if (x < width && height > 0)
                {
                    var cell = grid[x, 0];
                    if (cell != null)
                    {
                        yPos = cell.WorldPosition.y + 0.01f;
                    }
                }

                Vector3 startPos = bottomLeft + new Vector3(x * cellSize, yPos, 0);
                Vector3 endPos = startPos + new Vector3(0, 0, gridHeightWorld);

                _lineRenderers[lineIndex] = CreateLineRenderer($"GridLine_V_{x}", startPos, endPos);
                lineIndex++;
            }
        }

        private LineRenderer CreateLineRenderer(string name, Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(transform);
            lineObj.transform.position = Vector3.zero;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            
            // Set material
            if (GridLineMaterial != null)
            {
                lr.material = GridLineMaterial;
            }
            else
            {
                // Fallback: create a simple material
                Material fallbackMaterial = new Material(Shader.Find("Unlit/Color"));
                if (fallbackMaterial != null)
                {
                    fallbackMaterial.color = GridLineColor;
                    lr.material = fallbackMaterial;
                }
            }
            
            lr.startColor = GridLineColor;
            lr.endColor = GridLineColor;
            lr.startWidth = LineWidth;
            lr.endWidth = LineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.alignment = LineAlignment.View; // Makes lines face the camera

            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            return lr;
        }

        private void HideGridLines()
        {
            if (_lineRenderers != null)
            {
                foreach (var lr in _lineRenderers)
                {
                    if (lr != null && lr.gameObject != null)
                    {
                        lr.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void ClearGridLines()
        {
            if (_lineRenderers != null)
            {
                foreach (var lr in _lineRenderers)
                {
                    if (lr != null && lr.gameObject != null)
                    {
                        Destroy(lr.gameObject);
                    }
                }
                _lineRenderers = null;
            }

            // Also clean up any remaining child objects
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (transform.GetChild(i).name.StartsWith("GridLine_"))
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            ClearGridLines();
        }
    }
}


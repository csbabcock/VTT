using UnityEngine;
using GameCore.UI.InGame;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Visualizes a vertical column above the hovered grid cell to show available elevation levels.
    /// </summary>
    public class GridColumnVisualizer : MonoBehaviour
    {
        [Header("Column Visual Settings")]
        [Tooltip("Material for column lines")]
        public Material ColumnLineMaterial;

        [Tooltip("Color for column lines")]
        public Color ColumnLineColor = new Color(1f, 1f, 0f, 0.6f); // Yellow

        [Tooltip("Color for selected elevation level (3D cube)")]
        public Color SelectedElevationColor = new Color(0f, 1f, 0f, 0.4f); // Green with transparency

        [Tooltip("Line width for column")]
        public float LineWidth = 0.03f;

        [Tooltip("Height offset above grid cells")]
        public float HeightOffset = 0.01f;

        [Header("UI References")]
        [Tooltip("In-game UI view reference (for checking if character sheet is open)")]
        public InGameUIView InGameUIView;

        private IGridSelector _gridSelector;
        private IGridGenerator _gridGenerator;
        private LineRenderer[] _columnLines;
        private GameObject _selectedElevationIndicator;
        private GridCell _lastHoveredCell; // Cache to avoid unnecessary redraws
        private int _lastMaxElevation; // Cache to detect changes

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

            // Find InGameUIView if not assigned
            if (InGameUIView == null)
            {
                InGameUIView = FindFirstObjectByType<InGameUIView>();
            }
        }

        private void Update()
        {
            if (!enabled || _gridSelector == null || _gridGenerator == null)
            {
                ClearColumn();
                return;
            }

            UpdateColumnVisualization();
        }

        private void UpdateColumnVisualization()
        {
            // Check if mouse is over character sheet UI - if so, clear the column
            // But allow column visualization when character sheet is open but mouse is not over it
            bool isMouseOverUI = InGameUIView != null && InGameUIView.IsMouseOverCharacterSheet();

            if (isMouseOverUI)
            {
                // Clear column when mouse is over UI to prevent it from showing
                if (_lastHoveredCell != null || _selectedElevationIndicator != null)
                {
                    ClearColumn();
                    _lastHoveredCell = null;
                    _lastMaxElevation = 0;
                }
                return;
            }

            GridCell hoveredCell = _gridSelector.HoveredCell;
            int maxElevation = _gridSelector.MaxElevation;

            // Only show column if we have a valid hovered cell and max elevation
            // If hoveredCell is null, clear the column immediately
            if (hoveredCell != null && maxElevation > 0)
            {
                // Only redraw if cell or max elevation changed
                if (hoveredCell != _lastHoveredCell || maxElevation != _lastMaxElevation)
                {
                    DrawColumn(hoveredCell);
                    _lastHoveredCell = hoveredCell;
                    _lastMaxElevation = maxElevation;
                }
                UpdateSelectedElevationIndicator(hoveredCell);
            }
            else
            {
                // Clear column when no cell is hovered
                // This ensures the column doesn't show at a default position
                if (_lastHoveredCell != null || _selectedElevationIndicator != null)
                {
                    ClearColumn();
                    _lastHoveredCell = null;
                    _lastMaxElevation = 0;
                }
            }
        }

        private void DrawColumn(GridCell cell)
        {
            if (_gridGenerator == null)
                return;

            float cellSize = _gridGenerator.CellSize;
            Vector3 cellCenter = cell.WorldPosition;
            int maxElevation = _gridSelector.MaxElevation;

            // Calculate column height
            float columnHeight = maxElevation * cellSize;

            // Clear existing lines
            ClearColumnLines();

            float halfSize = cellSize * 0.5f;

            Vector3[] corners = new Vector3[]
            {
                new Vector3(-halfSize, 0, -halfSize), // Bottom-left
                new Vector3(halfSize, 0, -halfSize),  // Bottom-right
                new Vector3(halfSize, 0, halfSize),   // Top-right
                new Vector3(-halfSize, 0, halfSize)   // Top-left
            };

            // Calculate total number of lines needed:
            // 4 vertical lines + 4 horizontal lines per elevation level (including ground level)
            int verticalLines = 4;
            int horizontalLinesPerLevel = 4; // One line for each side of the square
            int totalLevels = maxElevation + 1; // Include ground level (elevation 0)
            int totalHorizontalLines = totalLevels * horizontalLinesPerLevel;
            int totalLines = verticalLines + totalHorizontalLines;

            _columnLines = new LineRenderer[totalLines];
            int lineIndex = 0;

            // Create 4 vertical lines for the column (one at each corner of the cell)
            for (int i = 0; i < 4; i++)
            {
                Vector3 bottomPos = cellCenter + corners[i];
                bottomPos.y += HeightOffset;
                Vector3 topPos = bottomPos + Vector3.up * columnHeight;

                _columnLines[lineIndex] = CreateColumnLine($"ColumnLine_V_{i}", bottomPos, topPos);
                lineIndex++;
            }

            // Create horizontal lines at each elevation level
            for (int level = 0; level <= maxElevation; level++)
            {
                float levelY = cellCenter.y + (level * cellSize) + HeightOffset;

                // Create 4 horizontal lines forming a square at this elevation level
                // Front edge (Z = -halfSize, from left to right)
                Vector3 frontLeft = new Vector3(cellCenter.x - halfSize, levelY, cellCenter.z - halfSize);
                Vector3 frontRight = new Vector3(cellCenter.x + halfSize, levelY, cellCenter.z - halfSize);
                _columnLines[lineIndex] = CreateColumnLine($"ColumnLine_H_Front_{level}", frontLeft, frontRight);
                lineIndex++;

                // Right edge (X = halfSize, from front to back)
                Vector3 rightFront = new Vector3(cellCenter.x + halfSize, levelY, cellCenter.z - halfSize);
                Vector3 rightBack = new Vector3(cellCenter.x + halfSize, levelY, cellCenter.z + halfSize);
                _columnLines[lineIndex] = CreateColumnLine($"ColumnLine_H_Right_{level}", rightFront, rightBack);
                lineIndex++;

                // Back edge (Z = halfSize, from right to left)
                Vector3 backRight = new Vector3(cellCenter.x + halfSize, levelY, cellCenter.z + halfSize);
                Vector3 backLeft = new Vector3(cellCenter.x - halfSize, levelY, cellCenter.z + halfSize);
                _columnLines[lineIndex] = CreateColumnLine($"ColumnLine_H_Back_{level}", backRight, backLeft);
                lineIndex++;

                // Left edge (X = -halfSize, from back to front)
                Vector3 leftBack = new Vector3(cellCenter.x - halfSize, levelY, cellCenter.z + halfSize);
                Vector3 leftFront = new Vector3(cellCenter.x - halfSize, levelY, cellCenter.z - halfSize);
                _columnLines[lineIndex] = CreateColumnLine($"ColumnLine_H_Left_{level}", leftBack, leftFront);
                lineIndex++;
            }
        }

        private LineRenderer CreateColumnLine(string name, Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(transform);
            lineObj.transform.position = Vector3.zero;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            
            if (ColumnLineMaterial != null)
            {
                lr.material = ColumnLineMaterial;
            }
            else
            {
                // Create default material using helper
                Material newMaterial = MaterialHelper.CreateMaterial(ColumnLineColor, false);
                if (newMaterial != null)
                {
                    lr.material = newMaterial;
                }
            }

            lr.startColor = ColumnLineColor;
            lr.endColor = ColumnLineColor;
            lr.startWidth = LineWidth;
            lr.endWidth = LineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            return lr;
        }

        private void UpdateSelectedElevationIndicator(GridCell cell)
        {
            if (_gridGenerator == null)
                return;

            int selectedElevation = _gridSelector.SelectedElevation;
            float cellSize = _gridGenerator.CellSize;
            Vector3 cellCenter = cell.WorldPosition;

            // Calculate position at selected elevation (center of the cube)
            Vector3 elevationPos = cellCenter;
            elevationPos.y += (selectedElevation * cellSize) + (cellSize * 0.5f); // Center the cube at the elevation level

            if (_selectedElevationIndicator == null)
            {
                // Create a 3D cube instead of a quad
                _selectedElevationIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _selectedElevationIndicator.name = "SelectedElevationIndicator";
                _selectedElevationIndicator.transform.SetParent(transform);

                // Remove collider (we don't need physics)
                Collider collider = _selectedElevationIndicator.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                // Set material with proper transparency
                Renderer renderer = _selectedElevationIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material newMaterial = MaterialHelper.CreateMaterial(SelectedElevationColor, true);
                    if (newMaterial != null)
                    {
                        renderer.material = newMaterial;
                    }
                }
            }

            // Position cube at the center of the cell at selected elevation
            _selectedElevationIndicator.transform.position = elevationPos;
            // Scale to full cell size (5 feet = cellSize in all dimensions)
            _selectedElevationIndicator.transform.localScale = Vector3.one * cellSize;
            _selectedElevationIndicator.SetActive(true);
        }

        private void ClearColumnLines()
        {
            if (_columnLines != null)
            {
                foreach (var lr in _columnLines)
                {
                    if (lr != null && lr.gameObject != null)
                    {
                        Destroy(lr.gameObject);
                    }
                }
                _columnLines = null;
            }

            // Clean up any remaining column line objects
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("ColumnLine_"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void ClearColumn()
        {
            ClearColumnLines();
            
            if (_selectedElevationIndicator != null)
            {
                _selectedElevationIndicator.SetActive(false);
            }
        }

        private void OnDisable()
        {
            // Clear column when component is disabled
            ClearColumn();
            _lastHoveredCell = null;
            _lastMaxElevation = 0;
        }

        private void OnDestroy()
        {
            ClearColumn();
            if (_selectedElevationIndicator != null)
                Destroy(_selectedElevationIndicator);
        }
    }
}


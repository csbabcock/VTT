using UnityEngine;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Generates and manages the grid of cells for encounter mode.
    /// MonoBehaviour component that implements IGridGenerator.
    /// </summary>
    public class GridGenerator : MonoBehaviour, IGridGenerator
    {
        private GridCell[,] _grid;
        private int _gridWidth;
        private int _gridHeight;
        private float _cellSize;
        private Vector3 _gridOrigin;

        public GridCell[,] Grid => _grid;
        public float CellSize => _cellSize;
        public Vector3 GridOrigin => _gridOrigin;

        /// <summary>
        /// Generates a grid centered at the specified world position.
        /// </summary>
        public void GenerateGrid(Vector3 origin, int width, int height, float cellSize, LayerMask groundLayer)
        {
            _gridWidth = width;
            _gridHeight = height;
            _cellSize = cellSize;
            _gridOrigin = origin;

            _grid = new GridCell[width, height];

            // Calculate the bottom-left corner of the grid
            float gridWidthWorld = width * cellSize;
            float gridHeightWorld = height * cellSize;
            Vector3 bottomLeft = origin - new Vector3(gridWidthWorld * 0.5f, 0, gridHeightWorld * 0.5f);

            // Generate cells
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    // Calculate cell center position
                    Vector3 cellPosition = bottomLeft + new Vector3(
                        x * cellSize + cellSize * 0.5f,
                        0,
                        z * cellSize + cellSize * 0.5f
                    );

                    // Raycast down to find ground level
                    if (Physics.Raycast(cellPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                    {
                        cellPosition.y = hit.point.y;
                    }

                    _grid[x, z] = new GridCell(x, z, cellPosition);
                }
            }
        }

        /// <summary>
        /// Gets the grid cell at the specified grid coordinates.
        /// </summary>
        public GridCell GetCell(int x, int z)
        {
            if (x < 0 || x >= _gridWidth || z < 0 || z >= _gridHeight)
                return null;

            return _grid[x, z];
        }

        /// <summary>
        /// Gets the grid cell containing the specified world position.
        /// </summary>
        public GridCell GetCellAtWorldPosition(Vector3 worldPos)
        {
            if (_grid == null)
                return null;

            // Calculate grid coordinates from world position
            float gridWidthWorld = _gridWidth * _cellSize;
            float gridHeightWorld = _gridHeight * _cellSize;
            Vector3 bottomLeft = _gridOrigin - new Vector3(gridWidthWorld * 0.5f, 0, gridHeightWorld * 0.5f);

            Vector3 localPos = worldPos - bottomLeft;
            int x = Mathf.FloorToInt(localPos.x / _cellSize);
            int z = Mathf.FloorToInt(localPos.z / _cellSize);

            return GetCell(x, z);
        }
    }
}


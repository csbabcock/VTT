using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class EncounterGridSnapUtilityTests
    {
        private sealed class FakeGridGenerator : IGridGenerator
        {
            private readonly GridCell[,] _grid;

            public FakeGridGenerator(GridCell[,] grid, float cellSize = 5f)
            {
                _grid = grid;
                CellSize = cellSize;
            }

            public GridCell[,] Grid => _grid;
            public float CellSize { get; }
            public Vector3 GridOrigin => Vector3.zero;

            public void GenerateGrid(Vector3 origin, int width, int height, float cellSize, LayerMask groundLayer) { }

            public GridCell GetCell(int x, int z)
            {
                if (_grid == null || x < 0 || z < 0 || x >= _grid.GetLength(0) || z >= _grid.GetLength(1))
                    return null;

                return _grid[x, z];
            }

            public GridCell GetCellAtWorldPosition(Vector3 worldPos) => null;
        }

        [Test]
        public void ResolveSnapCell_UsesFallback_WhenPositionMissesGrid()
        {
            var fallback = new GridCell(1, 1, new Vector3(5f, 2f, 5f));
            var grid = new GridCell[2, 2];
            grid[1, 1] = fallback;
            var generator = new FakeGridGenerator(grid);

            GridCell cell = EncounterGridSnapUtility.ResolveSnapCell(generator, Vector3.zero, 1, 1);

            Assert.AreSame(fallback, cell);
        }

        [Test]
        public void ResolveSnapPosition_ReturnsNull_WhenGridMissing()
        {
            Assert.IsNull(EncounterGridSnapUtility.ResolveSnapPosition(null, Vector3.zero, 0, 0));
            Assert.IsNull(EncounterGridSnapUtility.ResolveSnapPosition(new FakeGridGenerator(null), Vector3.zero, 0, 0));
        }

        [Test]
        public void ResolveSnapPosition_UsesCellGroundHeight()
        {
            var cell = new GridCell(0, 0, new Vector3(0f, 3.5f, 0f));
            var grid = new GridCell[1, 1];
            grid[0, 0] = cell;
            var generator = new FakeGridGenerator(grid, cellSize: 5f);

            Vector3? snapped = EncounterGridSnapUtility.ResolveSnapPosition(generator, Vector3.zero, 0, 0);

            Assert.IsTrue(snapped.HasValue);
            Assert.AreEqual(3.5f, snapped.Value.y, 0.001f);
        }
    }
}

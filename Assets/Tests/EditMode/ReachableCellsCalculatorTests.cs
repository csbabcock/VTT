using System.Collections.Generic;
using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Locks in the current behavior of <see cref="ReachableCellsCalculator"/> before refactoring.
    /// </summary>
    public class ReachableCellsCalculatorTests
    {
        /// <summary>Minimal in-memory grid for exercising the calculator without a scene.</summary>
        private sealed class FakeGridGenerator : IGridGenerator
        {
            public GridCell[,] Grid { get; private set; }
            public float CellSize => 1.524f;
            public Vector3 GridOrigin => Vector3.zero;

            public FakeGridGenerator(int width, int height)
            {
                Grid = new GridCell[width, height];
                for (int x = 0; x < width; x++)
                    for (int z = 0; z < height; z++)
                        Grid[x, z] = new GridCell(x, z, new Vector3(x, 0, z));
            }

            public void GenerateGrid(Vector3 origin, int width, int height, float cellSize, LayerMask groundLayer) { }
            public GridCell GetCell(int x, int z) => Grid[x, z];
            public GridCell GetCellAtWorldPosition(Vector3 worldPos) => null;
        }

        [Test]
        public void OneCellOfBudget_ReturnsChebyshevNeighborhood()
        {
            var grid = new FakeGridGenerator(5, 5);
            GridCell start = grid.GetCell(2, 2);

            HashSet<GridCell> reachable = ReachableCellsCalculator.CalculateReachableCells(grid, start, 5);

            Assert.AreEqual(9, reachable.Count); // 3x3 block centered on start
            Assert.IsTrue(reachable.Contains(grid.GetCell(1, 1)));
            Assert.IsFalse(reachable.Contains(grid.GetCell(0, 2)));
        }

        [Test]
        public void LargeBudget_ReturnsEntireWalkableGrid()
        {
            var grid = new FakeGridGenerator(5, 5);
            GridCell start = grid.GetCell(2, 2);

            HashSet<GridCell> reachable = ReachableCellsCalculator.CalculateReachableCells(grid, start, 10);

            Assert.AreEqual(25, reachable.Count);
        }

        [Test]
        public void UnwalkableCells_AreExcluded()
        {
            var grid = new FakeGridGenerator(5, 5);
            GridCell start = grid.GetCell(2, 2);
            grid.GetCell(2, 3).IsWalkable = false;

            HashSet<GridCell> reachable = ReachableCellsCalculator.CalculateReachableCells(grid, start, 5);

            Assert.AreEqual(8, reachable.Count);
            Assert.IsFalse(reachable.Contains(grid.GetCell(2, 3)));
        }

        [Test]
        public void ZeroBudget_ReturnsEmpty()
        {
            var grid = new FakeGridGenerator(5, 5);
            GridCell start = grid.GetCell(2, 2);

            HashSet<GridCell> reachable = ReachableCellsCalculator.CalculateReachableCells(grid, start, 0);

            Assert.AreEqual(0, reachable.Count);
        }

        [Test]
        public void NullGenerator_ReturnsEmpty()
        {
            HashSet<GridCell> reachable = ReachableCellsCalculator.CalculateReachableCells(
                null, new GridCell(0, 0, Vector3.zero), 30);

            Assert.AreEqual(0, reachable.Count);
        }

        [Test]
        public void NullStartCell_ReturnsEmpty()
        {
            var grid = new FakeGridGenerator(3, 3);

            HashSet<GridCell> reachable = ReachableCellsCalculator.CalculateReachableCells(grid, null, 30);

            Assert.AreEqual(0, reachable.Count);
        }
    }
}

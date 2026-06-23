using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class AttackApproachPlannerTests
    {
        [Test]
        public void TryFindMeleeApproachCell_ReturnsCurrentCell_WhenAlreadyInRange()
        {
            var from = new GridCell(0, 0, Vector3.zero);
            var target = new GridCell(1, 0, Vector3.zero);
            var grid = BuildGrid(3, 3);

            Assert.IsTrue(AttackApproachPlanner.TryFindMeleeApproachCell(
                grid, from, target, remainingMovementFeet: 0, out GridCell approach));
            Assert.AreSame(from, approach);
        }

        [Test]
        public void TryFindMeleeApproachCell_FindsReachableAdjacentCell()
        {
            var from = new GridCell(0, 0, Vector3.zero);
            var target = new GridCell(2, 0, Vector3.zero);
            var grid = BuildGrid(4, 3);

            Assert.IsTrue(AttackApproachPlanner.TryFindMeleeApproachCell(
                grid, from, target, remainingMovementFeet: 10, out GridCell approach));
            Assert.AreEqual(1, approach.X);
            Assert.AreEqual(0, approach.Z);
        }

        [Test]
        public void TryFindMeleeApproachCell_Fails_WhenNotEnoughMovement()
        {
            var from = new GridCell(0, 0, Vector3.zero);
            var target = new GridCell(3, 0, Vector3.zero);
            var grid = BuildGrid(5, 3);

            Assert.IsFalse(AttackApproachPlanner.TryFindMeleeApproachCell(
                grid, from, target, remainingMovementFeet: 5, out _));
        }

        private static FakeGridGenerator BuildGrid(int width, int height)
        {
            var cells = new GridCell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                    cells[x, z] = new GridCell(x, z, new Vector3(x, 0, z));
            }

            return new FakeGridGenerator(cells);
        }

        private sealed class FakeGridGenerator : IGridGenerator
        {
            public FakeGridGenerator(GridCell[,] grid) => Grid = grid;

            public GridCell[,] Grid { get; }
            public float CellSize => 1f;
            public Vector3 GridOrigin => Vector3.zero;

            public void GenerateGrid(Vector3 origin, int width, int height, float cellSize, LayerMask groundLayer) { }

            public GridCell GetCell(int x, int z) => Grid[x, z];

            public GridCell GetCellAtWorldPosition(Vector3 worldPos) => Grid[0, 0];
        }
    }
}

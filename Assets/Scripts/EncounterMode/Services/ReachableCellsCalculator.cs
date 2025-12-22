using UnityEngine;
using GameCore.EncounterMode.Grid;
using System.Collections.Generic;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Calculates reachable cells based on remaining movement.
    /// Follows Single Responsibility Principle.
    /// </summary>
    public static class ReachableCellsCalculator
    {
        private const int FEET_PER_CELL = 5;

        /// <summary>
        /// Calculates all cells reachable from the starting cell with the given remaining movement.
        /// </summary>
        public static HashSet<GridCell> CalculateReachableCells(
            IGridGenerator gridGenerator,
            GridCell startCell,
            int remainingMovementFeet)
        {
            HashSet<GridCell> reachableCells = new HashSet<GridCell>();

            if (gridGenerator == null || startCell == null || remainingMovementFeet <= 0)
                return reachableCells;

            int maxCells = Mathf.FloorToInt(remainingMovementFeet / FEET_PER_CELL);
            GridCell[,] grid = gridGenerator.Grid;

            if (grid == null)
                return reachableCells;

            int gridWidth = grid.GetLength(0);
            int gridHeight = grid.GetLength(1);

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    GridCell cell = grid[x, z];
                    if (cell == null || !cell.IsWalkable)
                        continue;

                    int deltaX = Mathf.Abs(cell.X - startCell.X);
                    int deltaZ = Mathf.Abs(cell.Z - startCell.Z);
                    int cellsAway = Mathf.Max(deltaX, deltaZ); // Manhattan distance

                    if (cellsAway <= maxCells)
                    {
                        reachableCells.Add(cell);
                    }
                }
            }

            return reachableCells;
        }
    }
}


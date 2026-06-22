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

            int maxCells = GridDistanceRules.FeetToCells(remainingMovementFeet);
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

                    int cellsAway = GridDistanceRules.CellsBetween(
                        startCell.X, startCell.Z, cell.X, cell.Z);

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


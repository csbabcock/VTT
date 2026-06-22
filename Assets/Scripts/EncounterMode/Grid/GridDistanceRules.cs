using UnityEngine;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Single source of truth for grid distance math (D&D 5e style: diagonals cost the
    /// same as orthogonals, each cell is 5 feet). Shared by the move validator, movement
    /// tracker, and reachable-cell calculator so the rules can never drift apart.
    /// </summary>
    public static class GridDistanceRules
    {
        public const int FeetPerCell = 5;

        /// <summary>Chebyshev distance in cells between two grid coordinates.</summary>
        public static int CellsBetween(int fromX, int fromZ, int toX, int toZ)
        {
            int deltaX = Mathf.Abs(toX - fromX);
            int deltaZ = Mathf.Abs(toZ - fromZ);
            return Mathf.Max(deltaX, deltaZ);
        }

        /// <summary>Distance in feet between two grid coordinates.</summary>
        public static int DistanceFeet(int fromX, int fromZ, int toX, int toZ)
        {
            return CellsBetween(fromX, fromZ, toX, toZ) * FeetPerCell;
        }

        /// <summary>Distance in feet between two cells; returns 0 if either is null.</summary>
        public static int DistanceFeet(GridCell fromCell, GridCell toCell)
        {
            if (fromCell == null || toCell == null)
                return 0;

            return DistanceFeet(fromCell.X, fromCell.Z, toCell.X, toCell.Z);
        }

        /// <summary>Number of whole cells reachable with the given feet of movement.</summary>
        public static int FeetToCells(int feet)
        {
            return feet / FeetPerCell;
        }
    }
}

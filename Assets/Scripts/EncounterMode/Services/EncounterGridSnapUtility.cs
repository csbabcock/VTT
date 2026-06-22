using GameCore.EncounterMode.Grid;
using UnityEngine;

namespace GameCore.EncounterMode.Services
{
    /// <summary>Resolves snap targets for actors joining or entering encounter mode.</summary>
    public static class EncounterGridSnapUtility
    {
        public static GridCell ResolveSnapCell(
            IGridGenerator gridGenerator,
            Vector3 worldPosition,
            int fallbackGridX,
            int fallbackGridZ)
        {
            if (gridGenerator?.Grid == null)
                return null;

            GridCell cell = gridGenerator.GetCellAtWorldPosition(worldPosition);
            return cell ?? gridGenerator.GetCell(fallbackGridX, fallbackGridZ);
        }

        public static Vector3? ResolveSnapPosition(
            IGridGenerator gridGenerator,
            Vector3 worldPosition,
            int fallbackGridX,
            int fallbackGridZ)
        {
            GridCell cell = ResolveSnapCell(gridGenerator, worldPosition, fallbackGridX, fallbackGridZ);
            if (cell == null)
                return null;

            return EncounterPathPlanner.CalculateTargetPosition(cell, elevation: 0, gridGenerator.CellSize);
        }
    }
}

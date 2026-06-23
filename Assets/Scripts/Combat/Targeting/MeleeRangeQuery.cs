using GameCore.Actors;
using GameCore.EncounterMode.Grid;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>Shared melee reach checks for combat targeting and approach movement.</summary>
    public static class MeleeRangeQuery
    {
        public static bool IsWithinMeleeReach(
            Transform attacker,
            Transform target,
            IGridGenerator grid,
            float feetPerWorldUnit)
        {
            if (attacker == null || target == null)
                return false;

            if (MeleeReachValidator.IsWithinMeleeReachWorld(
                    attacker.position,
                    target.position,
                    feetPerWorldUnit))
            {
                return true;
            }

            if (grid?.Grid != null)
            {
                GridCell fromCell = grid.GetCellAtWorldPosition(attacker.position);
                GridCell toCell = grid.GetCellAtWorldPosition(target.position);
                if (fromCell != null && toCell != null)
                    return MeleeReachValidator.IsWithinMeleeReachCells(fromCell, toCell);
            }

            return false;
        }

        public static bool IsWithinMeleeReach(IActor attacker, IActor target, IGridGenerator grid, float feetPerWorldUnit) =>
            IsWithinMeleeReach(attacker?.Transform, target?.Transform, grid, feetPerWorldUnit);
    }
}

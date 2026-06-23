using GameCore.EncounterMode.Grid;
using GameCore.Combat.Targeting;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Finds a reachable grid cell within melee range of a target for attack approach movement.
    /// </summary>
    public static class AttackApproachPlanner
    {
        public static bool TryFindMeleeApproachCell(
            IGridGenerator gridGenerator,
            GridCell attackerCell,
            GridCell targetCell,
            int remainingMovementFeet,
            out GridCell approachCell)
        {
            approachCell = null;
            if (gridGenerator?.Grid == null || attackerCell == null || targetCell == null)
                return false;

            if (MeleeReachValidator.IsWithinMeleeReachCells(attackerCell, targetCell))
            {
                approachCell = attackerCell;
                return true;
            }

            if (remainingMovementFeet <= 0)
                return false;

            var reachable = ReachableCellsCalculator.CalculateReachableCells(
                gridGenerator,
                attackerCell,
                remainingMovementFeet);

            int bestDistanceFeet = int.MaxValue;
            GridCell bestCell = null;
            GridCell[,] grid = gridGenerator.Grid;
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    GridCell candidate = grid[x, z];
                    if (candidate == null
                        || !candidate.IsWalkable
                        || !reachable.Contains(candidate))
                    {
                        continue;
                    }

                    if (!MeleeReachValidator.IsWithinMeleeReachCells(candidate, targetCell))
                        continue;

                    int distanceFeet = GridDistanceRules.DistanceFeet(attackerCell, candidate);
                    if (distanceFeet >= bestDistanceFeet)
                        continue;

                    bestDistanceFeet = distanceFeet;
                    bestCell = candidate;
                }
            }

            if (bestCell == null)
                return false;

            approachCell = bestCell;
            return true;
        }
    }
}

using System.Collections;
using GameCore.Actors;
using GameCore.EncounterMode;
using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using GameCore;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>Moves the attacker into melee range before resolving an attack.</summary>
    public sealed class CombatMeleeApproachService
    {
        private readonly System.Func<IEncounterModeManager> _getEncounterManager;
        private readonly System.Func<IGridGenerator> _getGridGenerator;
        private readonly System.Func<float> _getFeetPerWorldUnit;

        public CombatMeleeApproachService(
            System.Func<IEncounterModeManager> getEncounterManager,
            System.Func<IGridGenerator> getGridGenerator,
            System.Func<float> getFeetPerWorldUnit)
        {
            _getEncounterManager = getEncounterManager;
            _getGridGenerator = getGridGenerator;
            _getFeetPerWorldUnit = getFeetPerWorldUnit;
        }

        public bool TryApproach(IActor attacker, IActor target)
        {
            if (attacker?.Transform == null || target?.Transform == null)
                return false;

            IEncounterModeManager encounter = _getEncounterManager?.Invoke();
            if (encounter != null && encounter.IsEncounterModeActive)
                return encounter.TryApproachMeleeRange(target.Transform);

            return TryApproachFree(attacker, target);
        }

        public IEnumerator WaitForApproachComplete(IActor attacker)
        {
            if (attacker?.Transform == null)
                yield break;

            IEncounterModeManager encounter = _getEncounterManager?.Invoke();
            if (encounter == null || !encounter.IsEncounterModeActive)
                yield break;

            var controller = attacker.Transform.GetComponent<PlayerController>();
            if (controller == null)
            {
                yield return null;
                yield break;
            }

            const float timeoutSeconds = 3f;
            float elapsed = 0f;
            while (controller.IsEncounterGridMoving && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private bool TryApproachFree(IActor attacker, IActor target)
        {
            Transform attackerTransform = attacker.Transform;
            Transform targetTransform = target.Transform;
            float meleeRange = _getFeetPerWorldUnit();

            IGridGenerator grid = _getGridGenerator?.Invoke();
            if (grid?.Grid != null)
            {
                GridCell fromCell = grid.GetCellAtWorldPosition(attackerTransform.position);
                GridCell targetCell = grid.GetCellAtWorldPosition(targetTransform.position);
                if (fromCell != null
                    && targetCell != null
                    && AttackApproachPlanner.TryFindMeleeApproachCell(
                        grid,
                        fromCell,
                        targetCell,
                        remainingMovementFeet: int.MaxValue / GridDistanceRules.FeetPerCell,
                        out GridCell approachCell)
                    && approachCell != null)
                {
                    if (approachCell == fromCell)
                        return true;

                    float cellSize = grid.CellSize;
                    Vector3 destination = EncounterPathPlanner.CalculateTargetPosition(approachCell, 0, cellSize);
                    SnapTransform(attackerTransform, destination);
                    return true;
                }
            }

            return WorldMeleeApproach.TrySnapIntoRange(attackerTransform, targetTransform, meleeRange);
        }

        private static void SnapTransform(Transform attacker, Vector3 destination)
        {
            var controller = attacker.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                attacker.position = destination;
                controller.enabled = true;
                return;
            }

            attacker.position = destination;
        }
    }
}

using System;
using System.Collections;
using GameCore.Actors;
using GameCore.EncounterMode;
using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>Moves the attacker into melee range before resolving an attack.</summary>
    public sealed class CombatMeleeApproachService
    {
        private readonly Func<IEncounterModeManager> _getEncounterManager;
        private readonly Func<IGridGenerator> _getGridGenerator;
        private readonly Func<float> _getFeetPerWorldUnit;

        public CombatMeleeApproachService(
            Func<IEncounterModeManager> getEncounterManager,
            Func<IGridGenerator> getGridGenerator,
            Func<float> getFeetPerWorldUnit)
        {
            _getEncounterManager = getEncounterManager;
            _getGridGenerator = getGridGenerator;
            _getFeetPerWorldUnit = getFeetPerWorldUnit;
        }

        public bool TryApproach(IActor attacker, IActor target)
        {
            if (attacker?.Transform == null || target?.Transform == null)
                return false;

            IGridGenerator grid = _getGridGenerator?.Invoke();
            float feetPerWorldUnit = _getFeetPerWorldUnit();

            if (MeleeRangeQuery.IsWithinMeleeReach(attacker, target, grid, feetPerWorldUnit))
                return true;

            IEncounterModeManager encounter = _getEncounterManager?.Invoke();
            if (encounter != null && encounter.IsEncounterModeActive)
            {
                if (encounter.TryApproachMeleeRange(target.Transform))
                    return true;

                if (!encounter.UsesNetworkEncounter)
                    return TryAnimatedApproach(attacker, target);

                return false;
            }

            return TryAnimatedApproach(attacker, target);
        }

        public IEnumerator FinalizeMeleeRange(IActor attacker, IActor target)
        {
            if (attacker?.Transform == null || target?.Transform == null)
                yield break;

            // A network-approved move may start after the initial movement wait.
            // Being in attack range does not mean that locomotion has finished.
            yield return WaitForApproachComplete(attacker);

            // Correct a remaining gap through locomotion, never by teleporting.
            if (!MeleeRangeQuery.IsWithinMeleeReach(
                    attacker, target, _getGridGenerator?.Invoke(), _getFeetPerWorldUnit())
                && TryApproach(attacker, target))
                yield return WaitForApproachComplete(attacker);
        }

        public IEnumerator WaitForApproachComplete(IActor attacker)
        {
            if (attacker?.Transform == null)
                yield break;

            var controller = attacker.Transform.GetComponent<GameCore.PlayerController>();
            if (controller == null)
            {
                yield return null;
                yield break;
            }

            const float timeoutSeconds = 6f;
            float elapsed = 0f;
            while (controller.IsEncounterGridMoving && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (controller.IsEncounterGridMoving)
                controller.CancelEncounterGridMovement();
        }

        public IEnumerator CoWaitUntilInMeleeRange(
            IActor attacker,
            IActor target,
            Func<bool> isInRange,
            float timeoutSeconds = 6f)
        {
            if (attacker == null || target == null || isInRange == null)
                yield break;

            float elapsed = 0f;
            while (!isInRange() && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private bool TryAnimatedApproach(IActor attacker, IActor target)
        {
            Transform attackerTransform = attacker.Transform;
            Transform targetTransform = target.Transform;
            float meleeRange = _getFeetPerWorldUnit();
            float standoff = MeleeStandoff.ComputeApproachStandoff(
                attackerTransform,
                targetTransform,
                meleeRange);
            var playerController = attackerTransform.GetComponent<GameCore.PlayerController>();

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
                    if (playerController != null)
                        return playerController.BeginCombatApproachMove(approachCell, 0);

                    return false;
                }
            }

            Vector3 freeApproachWorld = MeleeApproachPositions.ResolveFreeMeleeApproachPosition(
                attackerTransform.position,
                targetTransform.position,
                standoff);

            if (playerController != null && grid?.Grid != null)
            {
                GridCell destinationCell = grid.GetCellAtWorldPosition(freeApproachWorld);
                if (destinationCell != null)
                    return playerController.BeginCombatApproachMove(destinationCell, 0);
            }

            return false;
        }
    }
}

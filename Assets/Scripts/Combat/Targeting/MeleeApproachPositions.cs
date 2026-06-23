using GameCore.EncounterMode.Grid;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>World-space melee approach destinations.</summary>
    public static class MeleeApproachPositions
    {
        public static Vector3 ResolveGridMeleeApproachPosition(
            Vector3 attackerPosition,
            Vector3 targetPosition,
            GridCell approachCell,
            float standoffWorldUnits)
        {
            if (approachCell == null)
                return targetPosition;

            Vector3 destination = ResolveFreeMeleeApproachPosition(
                attackerPosition,
                targetPosition,
                standoffWorldUnits);
            destination.y = approachCell.WorldPosition.y;
            return destination;
        }

        public static Vector3 ResolveFreeMeleeApproachPosition(
            Vector3 attackerPosition,
            Vector3 targetPosition,
            float standoffWorldUnits)
        {
            Vector3 towardAttacker = attackerPosition - targetPosition;
            towardAttacker.y = 0f;

            if (towardAttacker.sqrMagnitude < 0.0001f)
                towardAttacker = Vector3.forward;

            Vector3 destination = targetPosition + towardAttacker.normalized * standoffWorldUnits;
            destination.y = attackerPosition.y;
            return destination;
        }
    }
}

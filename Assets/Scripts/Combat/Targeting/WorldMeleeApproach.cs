using UnityEngine;

namespace GameCore.Combat.Targeting
{
    public static class WorldMeleeApproach
    {
        private const float SnapToleranceWorldUnits = 0.08f;

        public static bool TrySnapIntoRange(Transform attacker, Transform target, float meleeRangeWorldUnits)
        {
            if (attacker == null || target == null)
                return false;

            float standoff = MeleeStandoff.ComputeApproachStandoff(attacker, target, meleeRangeWorldUnits);
            Vector3 destination = MeleeApproachPositions.ResolveFreeMeleeApproachPosition(
                attacker.position,
                target.position,
                standoff);

            if (HorizontalDistance(attacker.position, destination) <= SnapToleranceWorldUnits)
                return true;

            SnapTransform(attacker, destination);
            return true;
        }

        public static bool TrySnapToWorldPosition(Transform attacker, Vector3 destination)
        {
            if (attacker == null)
                return false;

            SnapTransform(attacker, destination);
            return true;
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            float dx = from.x - to.x;
            float dz = from.z - to.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static void SnapTransform(Transform attacker, Vector3 destination)
        {
            var controller = attacker.GetComponent<CharacterController>();
            if (controller != null)
            {
                var playerController = attacker.GetComponent<GameCore.PlayerController>();
                playerController?.CancelEncounterGridMovement();

                controller.enabled = false;
                attacker.position = destination;
                controller.enabled = true;
                return;
            }

            attacker.position = destination;
        }
    }
}

using UnityEngine;

namespace GameCore.Combat.Targeting
{
    public static class WorldMeleeApproach
    {
        public static bool TrySnapIntoRange(Transform attacker, Transform target, float meleeRangeWorldUnits)
        {
            if (attacker == null || target == null)
                return false;

            Vector3 toAttacker = attacker.position - target.position;
            toAttacker.y = 0f;
            float distance = toAttacker.magnitude;
            if (distance <= meleeRangeWorldUnits + 0.05f)
                return true;

            if (distance < 0.001f)
                toAttacker = attacker.forward;

            Vector3 direction = toAttacker.normalized;
            Vector3 destination = target.position + direction * meleeRangeWorldUnits;
            destination.y = attacker.position.y;

            var controller = attacker.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                attacker.position = destination;
                controller.enabled = true;
            }
            else
            {
                attacker.position = destination;
            }

            return true;
        }
    }
}

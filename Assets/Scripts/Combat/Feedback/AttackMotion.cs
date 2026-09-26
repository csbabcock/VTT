using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>Geometry for a visual-only melee approach.</summary>
    public static class AttackMotion
    {
        public static Vector3 CalculateOffset(Vector3 origin, Vector3 target, float contactDistance)
        {
            Vector3 direction = target - origin;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance <= 0.0001f)
                return Vector3.zero;

            return direction / distance * Mathf.Max(0f, distance - Mathf.Max(0f, contactDistance));
        }

        public static Quaternion FacingRotation(Vector3 origin, Vector3 target, Quaternion fallback)
        {
            Vector3 direction = target - origin;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction)
                : fallback;
        }
    }
}

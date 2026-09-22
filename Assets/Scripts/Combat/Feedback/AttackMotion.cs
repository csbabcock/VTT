using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>Geometry and timing for a visual-only melee lunge.</summary>
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

        public static float LungeWeight(float normalizedTime)
        {
            float time = Mathf.Clamp01(normalizedTime);
            if (time < 0.3f)
                return Mathf.SmoothStep(0f, 1f, time / 0.3f);
            if (time < 0.55f)
                return 1f;
            return 1f - Mathf.SmoothStep(0f, 1f, (time - 0.55f) / 0.45f);
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

using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>World-space standoff for melee approach (capsule edge distance, not pivot-to-pivot).</summary>
    public static class MeleeStandoff
    {
        private const float DefaultBodyRadius = 0.25f;
        private const float ContactGapWorldUnits = 0.08f;
        private const float MinPivotSeparation = 0.2f;

        public static float ComputeApproachStandoff(
            Transform attacker,
            Transform target,
            float meleeRangeWorldUnits)
        {
            float combinedRadius = GetBodyRadius(attacker) + GetBodyRadius(target);
            float contactStandoff = combinedRadius + ContactGapWorldUnits;
            float maxStandoff = Mathf.Max(MinPivotSeparation, meleeRangeWorldUnits - 0.01f);
            return Mathf.Min(maxStandoff, Mathf.Max(MinPivotSeparation, contactStandoff));
        }

        public static float GetBodyRadius(Transform transform)
        {
            if (transform == null)
                return DefaultBodyRadius;

            var controller = transform.GetComponent<CharacterController>();
            if (controller != null)
                return controller.radius;

            return DefaultBodyRadius;
        }
    }
}

using UnityEngine;
using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Pure geometry for encounter grid movement: target position, arrival thresholds,
    /// diagonal direction, and progress detection. Extracted from
    /// <see cref="EncounterMovementHandler"/> so the math is unit-testable without a
    /// CharacterController, Transform, or per-frame Time.
    /// </summary>
    public static class EncounterPathPlanner
    {
        /// <summary>World-space position of a target cell at the given elevation level.</summary>
        public static Vector3 CalculateTargetPosition(GridCell targetCell, int elevation, float cellSize)
        {
            float groundLevelY = targetCell.WorldPosition.y;
            float elevationHeight = elevation * cellSize;

            return new Vector3(
                targetCell.WorldPosition.x,
                elevation == 0 ? groundLevelY : groundLevelY + elevationHeight,
                targetCell.WorldPosition.z);
        }

        /// <summary>Distance between two points projected onto the XZ (horizontal) plane.</summary>
        public static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            float dx = to.x - from.x;
            float dz = to.z - from.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Normalized 3D direction from current to target (zero if coincident).</summary>
        public static Vector3 CalculateDiagonalDirection(Vector3 currentPos, Vector3 targetPos)
        {
            Vector3 dir = new Vector3(
                targetPos.x - currentPos.x,
                targetPos.y - currentPos.y,
                targetPos.z - currentPos.z);

            float magnitude = dir.magnitude;
            return magnitude > 0.0001f ? dir / magnitude : Vector3.zero;
        }

        /// <summary>
        /// True once the avatar has actually progressed from its start position toward the
        /// target (guards against false "arrived" detection on the first frame).
        /// </summary>
        public static bool HasMovedTowardTarget(Vector3 currentPos, Vector3 startPos, Vector3 targetPos)
        {
            float distanceMoved = Vector3.Distance(currentPos, startPos);
            float initialDistToTarget = Vector3.Distance(startPos, targetPos);
            float currentDistToTarget = Vector3.Distance(currentPos, targetPos);

            return distanceMoved > EncounterMovementConstants.MIN_MOVEMENT_DISTANCE &&
                   currentDistToTarget <= initialDistToTarget + EncounterMovementConstants.MOVEMENT_TOLERANCE;
        }

        /// <summary>
        /// Geometric arrival test shared by the "already there" early-out and the in-flight
        /// arrival check. Ground targets require a small non-negative drop; elevated targets
        /// allow a symmetric tolerance.
        /// </summary>
        public static bool IsWithinArrivalThreshold(
            Vector3 currentPos,
            Vector3 targetPos,
            int elevation,
            float cellSize,
            float? horizontalThresholdOverride = null)
        {
            float horizontalDistance = HorizontalDistance(currentPos, targetPos);
            float verticalDistance = targetPos.y - currentPos.y;

            float horizontalThreshold = horizontalThresholdOverride ?? Mathf.Max(
                cellSize * EncounterMovementConstants.HORIZONTAL_THRESHOLD_MULTIPLIER,
                EncounterMovementConstants.MIN_HORIZONTAL_THRESHOLD);

            if (elevation == 0)
            {
                float verticalThreshold = EncounterMovementConstants.GROUND_LEVEL_VERTICAL_THRESHOLD;
                return horizontalDistance < horizontalThreshold &&
                       verticalDistance >= 0 && verticalDistance <= verticalThreshold;
            }

            float elevatedThreshold = EncounterMovementConstants.ELEVATED_VERTICAL_THRESHOLD;
            return horizontalDistance < horizontalThreshold &&
                   Mathf.Abs(verticalDistance) < elevatedThreshold;
        }
    }
}

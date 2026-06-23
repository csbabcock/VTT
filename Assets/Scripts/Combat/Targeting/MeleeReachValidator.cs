using GameCore.EncounterMode.Grid;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>5 ft melee reach checks for grid and free-roam play.</summary>
    public static class MeleeReachValidator
    {
        public const int MeleeReachFeet = GridDistanceRules.FeetPerCell;

        public static bool IsWithinMeleeReachFeet(int distanceFeet) =>
            distanceFeet <= MeleeReachFeet;

        public static bool IsWithinMeleeReachCells(GridCell fromCell, GridCell toCell)
        {
            if (fromCell == null || toCell == null)
                return false;

            return IsWithinMeleeReachFeet(GridDistanceRules.DistanceFeet(fromCell, toCell));
        }

        /// <summary>
        /// Horizontal world distance check. <paramref name="feetPerWorldUnit"/> is one grid cell width (5 feet).
        /// </summary>
        public static bool IsWithinMeleeReachWorld(Vector3 from, Vector3 to, float feetPerWorldUnit)
        {
            float dx = from.x - to.x;
            float dz = from.z - to.z;
            float horizontalDistance = Mathf.Sqrt(dx * dx + dz * dz);
            return horizontalDistance <= feetPerWorldUnit + 0.001f;
        }
    }
}

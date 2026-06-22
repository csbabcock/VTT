namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Encounter grid movement bypasses normal gravity while moving; idle actors still
    /// need vertical velocity applied (e.g. after spawning mid-encounter).
    /// </summary>
    public static class EncounterPlayerGroundingPolicy
    {
        public static bool ShouldApplyIdleGravity(bool isEncounterMovementMode, bool isMovingOnGrid)
        {
            return isEncounterMovementMode && !isMovingOnGrid;
        }
    }
}

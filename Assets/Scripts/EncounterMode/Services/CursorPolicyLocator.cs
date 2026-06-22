namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Scene-scoped seam for cursor lock policy. Registered by the in-game UI when the
    /// local client is the DM so <see cref="PlayerInputs"/> does not re-lock the cursor
    /// on application focus.
    /// </summary>
    public interface ICursorPolicy
    {
        bool ShouldKeepCursorUnlocked { get; }
    }

    public static class CursorPolicyLocator
    {
        public static ICursorPolicy Policy { get; set; }

        public static bool ShouldKeepCursorUnlocked =>
            Policy != null && Policy.ShouldKeepCursorUnlocked;

        public static void Clear() => Policy = null;
    }
}

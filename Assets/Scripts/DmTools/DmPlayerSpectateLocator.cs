namespace GameCore.DmTools
{
    /// <summary>Local-only DM spectate state exposed to UI and camera controllers.</summary>
    public static class DmPlayerSpectateLocator
    {
        public static bool IsSpectating { get; private set; }

        public static int SpectatedOwnerId { get; private set; } = -1;

        public static void SetSpectating(int ownerId)
        {
            IsSpectating = true;
            SpectatedOwnerId = ownerId;
        }

        public static void Clear()
        {
            IsSpectating = false;
            SpectatedOwnerId = -1;
        }
    }
}

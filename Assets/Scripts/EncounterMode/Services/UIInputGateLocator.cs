namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Scene-scoped seam for the active <see cref="IUIInputGate"/>. Registered by the in-game
    /// UI on startup and cleared on teardown. A null gate means "no UI is blocking input",
    /// preserving the previous behavior where an uninitialized service never blocked.
    /// </summary>
    public static class UIInputGateLocator
    {
        public static IUIInputGate Gate { get; set; }

        public static bool ShouldBlockInput() => Gate != null && Gate.ShouldBlockInput();

        public static bool IsCharacterSheetOpen() => Gate != null && Gate.IsCharacterSheetOpen();

        public static void Clear() => Gate = null;
    }
}

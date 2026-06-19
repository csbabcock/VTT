namespace GameCore.EncounterMode
{
    /// <summary>
    /// Static access to the active <see cref="IEncounterSessionAuthority"/> and the scene's
    /// <see cref="EncounterModeManager"/> (for server-side grid validation).
    /// </summary>
    public static class EncounterSessionLocator
    {
        public static IEncounterSessionAuthority Authority { get; set; }
        public static EncounterModeManager Manager { get; set; }
    }
}

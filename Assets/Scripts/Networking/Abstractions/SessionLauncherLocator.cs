namespace GameCore.Networking
{
    /// <summary>
    /// Access point for the active <see cref="ISessionLauncher"/>.
    ///
    /// Mirrors the existing <c>PlayerDataServiceLocator</c> pattern: a simple static
    /// handle rather than a DI framework. The netcode-backed launcher registers itself
    /// here when it awakes, and UI code reads <see cref="Current"/> without referencing
    /// any netcode types. When no launcher is registered, callers can fall back to
    /// local/offline behavior.
    /// </summary>
    public static class SessionLauncherLocator
    {
        /// <summary>The currently active launcher, or null if networking is not set up.</summary>
        public static ISessionLauncher Current { get; set; }
    }
}

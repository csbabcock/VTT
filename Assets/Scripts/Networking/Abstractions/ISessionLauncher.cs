namespace GameCore.Networking
{
    /// <summary>
    /// Transport-agnostic entry point for starting or joining a multiplayer session.
    ///
    /// UI and game-flow code (e.g. the main menu) depend only on this abstraction so
    /// they carry no dependency on a specific netcode stack (Dependency Inversion).
    /// The concrete implementation that talks to Netcode for GameObjects lives in the
    /// separate <c>GameCore.Networking</c> assembly; a different transport could be
    /// substituted by providing another implementation.
    /// </summary>
    public interface ISessionLauncher
    {
        /// <summary>Host address the client connects to / the host listens on.</summary>
        string Address { get; set; }

        /// <summary>Port used for the session.</summary>
        ushort Port { get; set; }

        /// <summary>True once a host or client session is running.</summary>
        bool IsActive { get; }

        /// <summary>
        /// Starts hosting and loads the given gameplay scene (replicated to clients).
        /// Returns false if the session could not start.
        /// </summary>
        bool StartHost(string gameSceneName = null);

        /// <summary>
        /// Connects to a host as a client. The host replicates its loaded scene to us.
        /// Returns false if the client could not start.
        /// </summary>
        bool StartClient(string address = null, ushort port = 0);

        /// <summary>Tears down the current session if one is active.</summary>
        void Shutdown();
    }
}

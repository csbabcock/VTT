namespace GameCore.Networking
{
    /// <summary>Role the local machine plays in a session.</summary>
    public enum SessionRole
    {
        /// <summary>A regular player controlling a single character.</summary>
        Player,

        /// <summary>The host, who acts as the Dungeon Master / game authority.</summary>
        DungeonMaster
    }

    /// <summary>
    /// Exposes the local machine's <see cref="SessionRole"/> to netcode-agnostic code
    /// (UI, gameplay). The networking layer sets <see cref="LocalRole"/> when a session
    /// starts (host = Dungeon Master, client = Player). Defaults to
    /// <see cref="SessionRole.DungeonMaster"/> for single-machine/offline play so the
    /// existing tools remain available before networking is wired.
    /// </summary>
    public static class SessionRoleLocator
    {
        public static SessionRole LocalRole { get; set; } = SessionRole.DungeonMaster;

        /// <summary>True when the local machine is the Dungeon Master / host.</summary>
        public static bool IsDungeonMaster => LocalRole == SessionRole.DungeonMaster;
    }
}

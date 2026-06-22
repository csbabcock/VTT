namespace GameCore.Networking
{
    /// <summary>
    /// Decides which connected clients receive a player avatar. The host/DM is a
    /// session manager and does not spawn into the world as a player character.
    /// </summary>
    public static class NetworkPlayerSpawnPolicy
    {
        /// <summary>
        /// Returns true when the given client should receive a spawned player object.
        /// The server client id (host/DM) is excluded.
        /// </summary>
        public static bool ShouldSpawnPlayerObject(ulong clientId, ulong serverClientId)
        {
            return clientId != serverClientId;
        }
    }
}

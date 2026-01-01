namespace GameCore.PlayerData
{
    /// <summary>
    /// Simple service locator pattern for accessing the player data service.
    /// Provides a single point of access without requiring dependency injection framework.
    /// Can be swapped with DI framework later if needed.
    /// 
    /// Usage:
    ///   var data = PlayerDataServiceLocator.Service.GetPlayerData();
    /// </summary>
    public static class PlayerDataServiceLocator
    {
        private static IPlayerDataService _service;

        /// <summary>
        /// Gets the current player data service instance.
        /// Creates a default LocalPlayerDataService if none is set.
        /// </summary>
        public static IPlayerDataService Service
        {
            get
            {
                if (_service == null)
                {
                    _service = new LocalPlayerDataService();
                }
                return _service;
            }
            set => _service = value; // Allow injection for testing or custom implementations
        }

        /// <summary>
        /// Resets the service to null. Useful for testing.
        /// </summary>
        public static void Reset()
        {
            _service = null;
        }
    }
}


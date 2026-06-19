namespace GameCore.PlayerData
{
    /// <summary>
    /// Simple service locator pattern for accessing the player data service.
    /// Provides a single point of access without requiring dependency injection framework.
    /// Can be swapped with DI framework later if needed.
    /// 
    /// Usage:
    ///   var sheet = PlayerDataServiceLocator.Service.GetCharacterSheet();
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
        /// True if a service has already been assigned, without lazily creating a default.
        /// Lets scene initializers avoid clobbering a selection made elsewhere (e.g. the
        /// character chosen in the main menu before loading the gameplay scene).
        /// </summary>
        public static bool HasService => _service != null;

        /// <summary>
        /// Resets the service to null. Useful for testing.
        /// </summary>
        public static void Reset()
        {
            _service = null;
        }
    }
}


namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Factory for obtaining the default ability score roller.
    /// Keeps Presenter decoupled from concrete implementation (DIP).
    /// </summary>
    public static class AbilityScoreRollerFactory
    {
        private static IAbilityScoreRoller _default;

        public static IAbilityScoreRoller GetDefault()
        {
            if (_default == null)
                _default = new AbilityScoreRoller();
            return _default;
        }
    }
}

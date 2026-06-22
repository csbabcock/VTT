namespace GameCore.EncounterMode.Services
{
    /// <summary>How the encounter grid presentation should initialize on scene start.</summary>
    public enum EncounterGridStartupAction
    {
        HideUntilEncounter,
        ShowForActiveEncounter,
    }

    /// <summary>
    /// Resolves grid presentation when <see cref="EncounterModeManager"/> starts after
    /// replicated encounter state may already be active (late-join / scene-sync race).
    /// </summary>
    public static class EncounterGridStartupPolicy
    {
        public static EncounterGridStartupAction ResolveStartupAction(bool isEncounterActive)
        {
            return isEncounterActive
                ? EncounterGridStartupAction.ShowForActiveEncounter
                : EncounterGridStartupAction.HideUntilEncounter;
        }

        public static bool ShouldRefreshPresentation(bool isEncounterActive) => isEncounterActive;
    }
}

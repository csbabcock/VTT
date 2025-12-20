namespace GameCore.EncounterMode
{
    /// <summary>
    /// Interface for managing encounter mode
    /// </summary>
    public interface IEncounterModeManager
    {
        bool IsEncounterModeActive { get; }
        void ToggleEncounterMode();
        void Initialize();
    }
}


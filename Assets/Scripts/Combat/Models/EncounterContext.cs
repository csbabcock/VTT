namespace GameCore.Combat.Models
{
    /// <summary>Context for whether encounter rules apply to a combat action.</summary>
    public readonly struct EncounterContext
    {
        public bool IsEncounterActive { get; }
        public bool IsLocalTurnActive { get; }

        public EncounterContext(bool isEncounterActive, bool isLocalTurnActive)
        {
            IsEncounterActive = isEncounterActive;
            IsLocalTurnActive = isLocalTurnActive;
        }

        public static EncounterContext OutOfEncounter => new(false, isLocalTurnActive: true);
    }
}

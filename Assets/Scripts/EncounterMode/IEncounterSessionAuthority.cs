using System;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Ruleset- and transport-agnostic encounter session authority. The networked
    /// implementation replicates state from the server; offline play leaves this null
    /// and <see cref="EncounterModeManager"/> toggles locally.
    /// </summary>
    public interface IEncounterSessionAuthority
    {
        bool IsEncounterActive { get; }
        int CurrentTurnOwnerId { get; }
        bool HasActiveTurnOrder { get; }

        event Action<bool> EncounterActiveChanged;
        event Action<int> TurnOwnerChanged;

        /// <summary>DM/host only: toggles encounter mode for all clients.</summary>
        void RequestToggleEncounter();

        /// <summary>DM/host only: rolls initiative and starts turn order.</summary>
        void RequestStartTurnOrder();

        /// <summary>DM/host only: advances to the next turn in initiative order.</summary>
        void RequestEndTurn();
    }
}

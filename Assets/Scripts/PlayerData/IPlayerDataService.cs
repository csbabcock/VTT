using System;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Service interface for accessing a player's character sheet.
    /// Provides a ruleset-agnostic abstraction (<see cref="ICharacterSheet"/>) that can be
    /// swapped between local, JSON-backed, and network-received implementations.
    /// Follows the Dependency Inversion Principle - UI and gameplay depend on this
    /// abstraction, not a concrete ruleset model.
    /// </summary>
    public interface IPlayerDataService
    {
        /// <summary>
        /// Gets the current player's character sheet as a ruleset-agnostic view.
        /// </summary>
        ICharacterSheet GetCharacterSheet();

        /// <summary>
        /// Raised whenever the backing character sheet changes (e.g. reloaded from disk
        /// or mutated server-side). Consumers re-read <see cref="GetCharacterSheet"/>.
        /// </summary>
        event Action<ICharacterSheet> CharacterSheetChanged;
    }
}

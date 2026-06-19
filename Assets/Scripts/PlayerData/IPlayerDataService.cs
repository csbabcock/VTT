using GameCore.UI.InGame.Models;
using System;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Service interface for accessing player data.
    /// Provides a simple abstraction that can be swapped between local and network implementations.
    /// Follows Dependency Inversion Principle - UI depends on abstraction, not concrete implementation.
    /// </summary>
    public interface IPlayerDataService
    {
        /// <summary>
        /// Gets the current player's character data.
        /// </summary>
        CharacterData GetPlayerData();

        /// <summary>
        /// Gets the current player's character sheet as a ruleset-agnostic view.
        /// Prefer this over <see cref="GetPlayerData"/> for ruleset-aware logic
        /// (it carries level, proficiency, and weapon proficiencies that the legacy
        /// <see cref="CharacterData"/> does not).
        /// </summary>
        ICharacterSheet GetCharacterSheet();

        /// <summary>
        /// Event fired whenever player data changes.
        /// UI can subscribe to this to reactively update when data changes.
        /// </summary>
        event Action<CharacterData> PlayerDataChanged;

        /// <summary>
        /// Updates player data using the provided action.
        /// Triggers PlayerDataChanged event after update.
        /// </summary>
        /// <param name="updateAction">Action that modifies the character data.</param>
        void UpdatePlayerData(Action<CharacterData> updateAction);
    }
}


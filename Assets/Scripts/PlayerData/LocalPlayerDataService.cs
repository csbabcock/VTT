using System;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Local implementation of <see cref="IPlayerDataService"/>.
    /// Loads a character sheet from a <see cref="PlayerDataAsset"/> preset or uses default
    /// values. For single-player/local use; can be swapped with a network implementation.
    /// </summary>
    public class LocalPlayerDataService : IPlayerDataService
    {
        private readonly DnD5eCharacterData _data;

        public event Action<ICharacterSheet> CharacterSheetChanged;

        /// <summary>
        /// Creates a new LocalPlayerDataService.
        /// </summary>
        /// <param name="dataAsset">Optional ScriptableObject preset. If null, uses default values.</param>
        public LocalPlayerDataService(PlayerDataAsset dataAsset = null)
        {
            _data = dataAsset != null ? dataAsset.ToDnD5eCharacterData() : new DnD5eCharacterData();
        }

        /// <inheritdoc />
        public ICharacterSheet GetCharacterSheet() => _data;

        /// <summary>Raises <see cref="CharacterSheetChanged"/> after the sheet has been mutated.</summary>
        public void NotifyChanged() => CharacterSheetChanged?.Invoke(_data);
    }
}

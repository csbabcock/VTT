using GameCore.UI.InGame.Models;
using System;

namespace GameCore.PlayerData
{
    /// <summary>
    /// <see cref="IPlayerDataService"/> backed directly by an in-memory
    /// <see cref="DnD5eCharacterData"/> instance rather than a JSON file.
    ///
    /// Used when a character arrives over the network: the server deserializes the
    /// transmitted character and wraps it in this service so the player's
    /// <c>PlayerActor</c> exposes a real sheet without any file backing.
    /// </summary>
    public class InMemoryPlayerDataService : IPlayerDataService
    {
        private readonly DnD5eCharacterData _dnD5eData;
        private CharacterData _legacyData;

        public event Action<CharacterData> PlayerDataChanged;

        public InMemoryPlayerDataService(DnD5eCharacterData data)
        {
            _dnD5eData = data ?? new DnD5eCharacterData();
            _legacyData = _dnD5eData.ToLegacyCharacterData();
        }

        public DnD5eCharacterData GetDnD5eCharacterData() => _dnD5eData;

        /// <inheritdoc />
        public ICharacterSheet GetCharacterSheet() => _dnD5eData;

        public CharacterData GetPlayerData()
        {
            return _legacyData ?? (_legacyData = _dnD5eData.ToLegacyCharacterData());
        }

        public void UpdatePlayerData(Action<CharacterData> updateAction)
        {
            if (updateAction == null)
                return;

            if (_legacyData == null)
                _legacyData = _dnD5eData.ToLegacyCharacterData();

            updateAction(_legacyData);

            // Mirror JsonPlayerDataService: sync the editable fields back to the rich model.
            _dnD5eData.strength = _legacyData.Strength;
            _dnD5eData.dexterity = _legacyData.Dexterity;
            _dnD5eData.constitution = _legacyData.Constitution;
            _dnD5eData.intelligence = _legacyData.Intelligence;
            _dnD5eData.wisdom = _legacyData.Wisdom;
            _dnD5eData.charisma = _legacyData.Charisma;
            _dnD5eData.characterName = _legacyData.CharacterName;

            PlayerDataChanged?.Invoke(_legacyData);
        }
    }
}

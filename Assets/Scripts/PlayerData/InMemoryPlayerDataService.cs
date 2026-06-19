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

        public event Action<ICharacterSheet> CharacterSheetChanged;

        public InMemoryPlayerDataService(DnD5eCharacterData data)
        {
            _dnD5eData = data ?? new DnD5eCharacterData();
        }

        public DnD5eCharacterData GetDnD5eCharacterData() => _dnD5eData;

        /// <inheritdoc />
        public ICharacterSheet GetCharacterSheet() => _dnD5eData;

        /// <summary>Raises <see cref="CharacterSheetChanged"/> after the sheet has been mutated.</summary>
        public void NotifyChanged() => CharacterSheetChanged?.Invoke(_dnD5eData);
    }
}

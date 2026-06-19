using System;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// JSON-based implementation of <see cref="IPlayerDataService"/>.
    /// Loads character data from JSON files in StreamingAssets/Characters/.
    /// </summary>
    public class JsonPlayerDataService : IPlayerDataService
    {
        private DnD5eCharacterData _dnD5eData;
        private readonly string _jsonFilePath;

        public event Action<ICharacterSheet> CharacterSheetChanged;

        /// <summary>
        /// Creates a new JsonPlayerDataService.
        /// </summary>
        /// <param name="jsonFilePath">Path to JSON file relative to StreamingAssets (e.g., "Characters/MyCharacter.json")</param>
        public JsonPlayerDataService(string jsonFilePath)
        {
            _jsonFilePath = jsonFilePath;
            LoadCharacterData();
        }

        /// <summary>
        /// Gets the D&D 5e character data (concrete ruleset model).
        /// </summary>
        public DnD5eCharacterData GetDnD5eCharacterData()
        {
            return _dnD5eData;
        }

        /// <inheritdoc />
        public ICharacterSheet GetCharacterSheet()
        {
            return _dnD5eData;
        }

        /// <summary>
        /// Loads character data from JSON file.
        /// </summary>
        private void LoadCharacterData()
        {
            if (string.IsNullOrEmpty(_jsonFilePath))
            {
                Debug.LogWarning("JsonPlayerDataService: No JSON file path provided. Using default character data.");
                _dnD5eData = new DnD5eCharacterData();
                return;
            }

            _dnD5eData = PlayerDataJsonLoader.LoadFromFile(_jsonFilePath);

            if (_dnD5eData == null)
            {
                Debug.LogWarning($"JsonPlayerDataService: Failed to load character data from {_jsonFilePath}. Using default character data.");
                _dnD5eData = new DnD5eCharacterData();
            }
            else
            {
                Debug.Log($"JsonPlayerDataService: Successfully loaded character data for {_dnD5eData.characterName} from {_jsonFilePath}");
            }
        }

        /// <summary>
        /// Reloads character data from the JSON file.
        /// Useful if the file was edited externally.
        /// </summary>
        public void Reload()
        {
            LoadCharacterData();
            CharacterSheetChanged?.Invoke(_dnD5eData);
        }

        /// <summary>
        /// Saves the current character data back to the JSON file.
        /// </summary>
        public bool Save()
        {
            if (string.IsNullOrEmpty(_jsonFilePath) || _dnD5eData == null)
            {
                return false;
            }

            return PlayerDataJsonLoader.SaveToFile(_dnD5eData, _jsonFilePath);
        }

        /// <summary>Raises <see cref="CharacterSheetChanged"/> after the sheet has been mutated.</summary>
        public void NotifyChanged() => CharacterSheetChanged?.Invoke(_dnD5eData);
    }
}

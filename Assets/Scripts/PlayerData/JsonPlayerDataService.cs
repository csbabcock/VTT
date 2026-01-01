using GameCore.UI.InGame.Models;
using System;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// JSON-based implementation of IPlayerDataService.
    /// Loads character data from JSON files in StreamingAssets/Characters/.
    /// Converts to legacy CharacterData format for backward compatibility.
    /// </summary>
    public class JsonPlayerDataService : IPlayerDataService
    {
        private DnD5eCharacterData _dnD5eData;
        private CharacterData _legacyData;
        private readonly string _jsonFilePath;

        public event Action<CharacterData> PlayerDataChanged;

        /// <summary>
        /// Creates a new JsonPlayerDataService.
        /// </summary>
        /// <param name="jsonFilePath">Path to JSON file relative to StreamingAssets (e.g., "Characters/MyCharacter.json")</param>
        public JsonPlayerDataService(string jsonFilePath)
        {
            _jsonFilePath = jsonFilePath;
            LoadCharacterData();
        }

        public CharacterData GetPlayerData()
        {
            // Return legacy format for backward compatibility
            if (_legacyData == null && _dnD5eData != null)
            {
                _legacyData = _dnD5eData.ToLegacyCharacterData();
            }
            return _legacyData ?? new CharacterData();
        }

        /// <summary>
        /// Gets the D&D 5e character data (new format).
        /// </summary>
        public DnD5eCharacterData GetDnD5eCharacterData()
        {
            return _dnD5eData;
        }

        public void UpdatePlayerData(Action<CharacterData> updateAction)
        {
            if (updateAction == null)
            {
                Debug.LogWarning("JsonPlayerDataService: Update action is null.");
                return;
            }

            // Get or create legacy data
            if (_legacyData == null)
            {
                _legacyData = _dnD5eData?.ToLegacyCharacterData() ?? new CharacterData();
            }

            // Apply the update
            updateAction(_legacyData);

            // Sync back to D&D 5e format (basic sync - only ability scores for now)
            if (_dnD5eData != null)
            {
                _dnD5eData.strength = _legacyData.Strength;
                _dnD5eData.dexterity = _legacyData.Dexterity;
                _dnD5eData.constitution = _legacyData.Constitution;
                _dnD5eData.intelligence = _legacyData.Intelligence;
                _dnD5eData.wisdom = _legacyData.Wisdom;
                _dnD5eData.charisma = _legacyData.Charisma;
                _dnD5eData.characterName = _legacyData.CharacterName;
            }

            // Notify listeners
            PlayerDataChanged?.Invoke(_legacyData);
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
                _legacyData = _dnD5eData.ToLegacyCharacterData();
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

            // Convert to legacy format
            _legacyData = _dnD5eData.ToLegacyCharacterData();
        }

        /// <summary>
        /// Reloads character data from the JSON file.
        /// Useful if the file was edited externally.
        /// </summary>
        public void Reload()
        {
            LoadCharacterData();
            PlayerDataChanged?.Invoke(_legacyData);
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
    }
}


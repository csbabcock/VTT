using GameCore.UI.InGame.Models;
using System;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Local implementation of IPlayerDataService.
    /// Loads player data from ScriptableObject or uses default values.
    /// For single-player/local use. Can be swapped with network implementation later.
    /// Follows Single Responsibility Principle - only handles local data loading and updates.
    /// </summary>
    public class LocalPlayerDataService : IPlayerDataService
    {
        private CharacterData _playerData;
        private readonly PlayerDataAsset _dataAsset;

        public event Action<CharacterData> PlayerDataChanged;

        /// <summary>
        /// Creates a new LocalPlayerDataService.
        /// </summary>
        /// <param name="dataAsset">Optional ScriptableObject to load data from. If null, uses default values.</param>
        public LocalPlayerDataService(PlayerDataAsset dataAsset = null)
        {
            _dataAsset = dataAsset;
            _playerData = LoadPlayerData();
        }

        public CharacterData GetPlayerData()
        {
            return _playerData;
        }

        /// <inheritdoc />
        public ICharacterSheet GetCharacterSheet()
        {
            // The local service only holds legacy data; project it into a ruleset
            // sheet so consumers get a consistent ruleset-agnostic view.
            return CharacterDataConverter.ConvertToDnD5eCharacterData(_playerData);
        }

        public void UpdatePlayerData(Action<CharacterData> updateAction)
        {
            if (updateAction == null)
            {
                Debug.LogWarning("LocalPlayerDataService: Update action is null.");
                return;
            }

            // Apply the update
            updateAction(_playerData);

            // Notify listeners
            PlayerDataChanged?.Invoke(_playerData);
        }

        /// <summary>
        /// Loads player data from ScriptableObject or creates default data.
        /// </summary>
        private CharacterData LoadPlayerData()
        {
            if (_dataAsset != null)
            {
                return _dataAsset.ToCharacterData();
            }

            // Fallback to default data
            return new CharacterData();
        }
    }
}


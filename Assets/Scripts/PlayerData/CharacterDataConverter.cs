using System.Collections.Generic;
using GameCore.UI.InGame.Models;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Helper class for converting between CharacterData and DnD5eCharacterData.
    /// Follows Single Responsibility Principle - only handles data conversion.
    /// Eliminates code duplication across the codebase.
    /// </summary>
    public static class CharacterDataConverter
    {
        /// <summary>
        /// Converts legacy CharacterData to DnD5eCharacterData with default values.
        /// Used as a fallback when ruleset-specific data is not available.
        /// </summary>
        /// <param name="characterData">The legacy CharacterData to convert</param>
        /// <param name="defaultLevel">Default level to use if not available (default: 1)</param>
        /// <param name="defaultProficientWeapons">Default weapon proficiencies (default: Simple, Martial)</param>
        /// <returns>A new DnD5eCharacterData instance with converted values</returns>
        public static DnD5eCharacterData ConvertToDnD5eCharacterData(
            CharacterData characterData, 
            int defaultLevel = 1,
            List<string> defaultProficientWeapons = null)
        {
            if (characterData == null)
            {
                return null;
            }

            if (defaultProficientWeapons == null)
            {
                defaultProficientWeapons = new List<string> { "Simple", "Martial" };
            }

            return new DnD5eCharacterData
            {
                characterName = characterData.CharacterName,
                strength = characterData.Strength,
                dexterity = characterData.Dexterity,
                constitution = characterData.Constitution,
                intelligence = characterData.Intelligence,
                wisdom = characterData.Wisdom,
                charisma = characterData.Charisma,
                level = defaultLevel,
                proficientWeapons = defaultProficientWeapons
            };
        }
    }
}


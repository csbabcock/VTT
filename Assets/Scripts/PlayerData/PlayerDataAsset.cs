using GameCore.UI.InGame.Models;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// ScriptableObject for storing player character data.
    /// Allows designers to create character presets in Unity without code changes.
    /// Follows Single Responsibility Principle - only holds data configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data", order = 1)]
    public class PlayerDataAsset : ScriptableObject
    {
        [Header("Character Info")]
        [Tooltip("Character's name")]
        public string characterName = "Arlen";

        [Header("Ability Scores")]
        [Tooltip("Strength score")]
        [Range(1, 30)]
        public int strength = 16;

        [Tooltip("Dexterity score")]
        [Range(1, 30)]
        public int dexterity = 14;

        [Tooltip("Constitution score")]
        [Range(1, 30)]
        public int constitution = 14;

        [Tooltip("Intelligence score")]
        [Range(1, 30)]
        public int intelligence = 10;

        [Tooltip("Wisdom score")]
        [Range(1, 30)]
        public int wisdom = 12;

        [Tooltip("Charisma score")]
        [Range(1, 30)]
        public int charisma = 8;

        [Header("Proficiency")]
        [Tooltip("Proficiency bonus")]
        [Range(1, 6)]
        public int proficiencyBonus = 2;

        [Header("Skills")]
        [Tooltip("List of skills the character is proficient in")]
        public List<string> proficientSkills = new List<string> { "Athletics" };

        /// <summary>
        /// Converts this ScriptableObject data to a CharacterData instance.
        /// </summary>
        public CharacterData ToCharacterData()
        {
            return new CharacterData
            {
                CharacterName = characterName,
                Strength = strength,
                Dexterity = dexterity,
                Constitution = constitution,
                Intelligence = intelligence,
                Wisdom = wisdom,
                Charisma = charisma,
                ProficiencyBonus = proficiencyBonus,
                ProficientSkills = new HashSet<string>(proficientSkills)
            };
        }
    }
}


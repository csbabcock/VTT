using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Comprehensive D&D 5e character data model.
    /// Supports JSON serialization for portability and tool integration.
    /// </summary>
    [Serializable]
    public class DnD5eCharacterData
    {
        [Header("Basic Information")]
        public string characterName = "Arlen";
        public int level = 1;
        public string characterClass = "Fighter";
        public string subclass = "";
        public string race = "Human";
        public string subrace = "";
        public string background = "Soldier";
        public string alignment = "Lawful Good";
        public string playerName = "";
        public int experiencePoints = 0;

        [Header("Ability Scores")]
        public int strength = 16;
        public int dexterity = 14;
        public int constitution = 14;
        public int intelligence = 10;
        public int wisdom = 12;
        public int charisma = 8;

        [Header("Combat Stats")]
        public int maxHitPoints = 10;
        public int currentHitPoints = 10;
        public int temporaryHitPoints = 0;
        public int armorClass = 16;
        public int initiative = 0; // Usually set manually, but should equal DEX modifier
        public int walkingSpeed = 30;
        public int flyingSpeed = 0;
        public int swimmingSpeed = 0;
        public int climbingSpeed = 0;
        public string hitDice = "1d10"; // e.g., "3d10" for level 3 fighter
        public int hitDiceUsed = 0;

        [Header("Proficiencies")]
        public List<string> proficientSavingThrows = new List<string>(); // "STR", "DEX", etc.
        [SerializeField] private List<string> _proficientSkillsStrings = new List<string>(); // Serialized as strings for JSON
        public List<string> proficientArmor = new List<string>(); // "Light", "Medium", "Heavy", "Shields"
        public List<string> proficientWeapons = new List<string>(); // "Simple", "Martial", or specific weapons
        public List<string> proficientTools = new List<string>();
        public List<string> languages = new List<string>();

        /// <summary>
        /// Gets the list of proficient skills as enums.
        /// Converts from string list (for JSON serialization).
        /// </summary>
        public List<DnD5eSkill> GetProficientSkills()
        {
            var skills = new List<DnD5eSkill>();
            foreach (var skillString in _proficientSkillsStrings)
            {
                var skill = DnD5eSkillExtensions.FromString(skillString);
                if (skill.HasValue)
                {
                    skills.Add(skill.Value);
                }
            }
            return skills;
        }

        /// <summary>
        /// Sets the list of proficient skills.
        /// Converts to string list for JSON serialization.
        /// </summary>
        public void SetProficientSkills(List<DnD5eSkill> skills)
        {
            _proficientSkillsStrings.Clear();
            foreach (var skill in skills)
            {
                _proficientSkillsStrings.Add(skill.GetDisplayName());
            }
        }

        /// <summary>
        /// Checks if the character is proficient in a skill.
        /// </summary>
        public bool IsProficientInSkill(DnD5eSkill skill)
        {
            return GetProficientSkills().Contains(skill);
        }

        [Header("Skills")]
        // Skill proficiencies are in proficientSkills list above
        // This is calculated from ability scores + proficiency bonus

        [Header("Other")]
        public bool hasInspiration = false;
        public int exhaustionLevel = 0;
        public List<string> conditions = new List<string>(); // "Poisoned", "Frightened", etc.
        public int deathSaveSuccesses = 0;
        public int deathSaveFailures = 0;

        [Header("Calculated Values (read-only)")]
        public int proficiencyBonus => CalculateProficiencyBonus();
        public int strengthModifier => CalculateModifier(strength);
        public int dexterityModifier => CalculateModifier(dexterity);
        public int constitutionModifier => CalculateModifier(constitution);
        public int intelligenceModifier => CalculateModifier(intelligence);
        public int wisdomModifier => CalculateModifier(wisdom);
        public int charismaModifier => CalculateModifier(charisma);
        
        /// <summary>
        /// Initiative modifier (DEX modifier in 5e).
        /// </summary>
        public int initiativeModifier => dexterityModifier;

        /// <summary>
        /// Calculates proficiency bonus based on level (D&D 5e rules).
        /// </summary>
        private int CalculateProficiencyBonus()
        {
            return (level - 1) / 4 + 2; // +2 at 1-4, +3 at 5-8, +4 at 9-12, etc.
        }

        /// <summary>
        /// Calculates ability modifier from ability score.
        /// </summary>
        private int CalculateModifier(int abilityScore)
        {
            return (abilityScore - 10) / 2;
        }

        /// <summary>
        /// Gets the modifier for a specific ability score.
        /// </summary>
        public int GetAbilityModifier(string abilityName)
        {
            return abilityName.ToUpper() switch
            {
                "STR" => strengthModifier,
                "DEX" => dexterityModifier,
                "CON" => constitutionModifier,
                "INT" => intelligenceModifier,
                "WIS" => wisdomModifier,
                "CHA" => charismaModifier,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the skill modifier for a skill, including proficiency if applicable.
        /// </summary>
        public int GetSkillModifier(DnD5eSkill skill)
        {
            string abilityName = skill.GetAbilityScore();
            int abilityMod = GetAbilityModifier(abilityName);
            bool isProficient = IsProficientInSkill(skill);
            int proficiency = isProficient ? proficiencyBonus : 0;
            return abilityMod + proficiency;
        }

        /// <summary>
        /// Gets the skill modifier for a skill by name (for backward compatibility).
        /// </summary>
        public int GetSkillModifier(string skillName)
        {
            var skill = DnD5eSkillExtensions.FromString(skillName);
            if (skill.HasValue)
            {
                return GetSkillModifier(skill.Value);
            }
            return 0;
        }

        /// <summary>
        /// Checks if the character is proficient in a saving throw.
        /// </summary>
        public bool IsProficientInSavingThrow(string abilityName)
        {
            return proficientSavingThrows.Contains(abilityName.ToUpper());
        }

        /// <summary>
        /// Gets the saving throw modifier for an ability score.
        /// </summary>
        public int GetSavingThrowModifier(string abilityName)
        {
            int abilityMod = GetAbilityModifier(abilityName);
            bool isProficient = IsProficientInSavingThrow(abilityName);
            int proficiency = isProficient ? proficiencyBonus : 0;
            return abilityMod + proficiency;
        }

        /// <summary>
        /// Converts to the old CharacterData format for backward compatibility.
        /// </summary>
        public GameCore.UI.InGame.Models.CharacterData ToLegacyCharacterData()
        {
            return new GameCore.UI.InGame.Models.CharacterData
            {
                CharacterName = characterName,
                Strength = strength,
                Dexterity = dexterity,
                Constitution = constitution,
                Intelligence = intelligence,
                Wisdom = wisdom,
                Charisma = charisma,
                ProficiencyBonus = proficiencyBonus,
                ProficientSkills = new HashSet<string>(GetProficientSkills().Select(s => s.GetDisplayName()))
            };
        }
    }
}


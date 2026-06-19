using System;
using System.Collections.Generic;
using GameCore.PlayerData.Rulesets;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Comprehensive D&D 5e character data model.
    /// Supports JSON serialization for portability and tool integration.
    /// </summary>
    [Serializable]
    public class DnD5eCharacterData : ICharacterSheet
    {
        [Header("Basic Information")]
        public string characterName = "";
        public int level = 1;
        public string characterClass = "";
        public string subclass = "";
        public string race = "";
        public string subrace = "";
        public string background = "";
        public string alignment = "";
        public string playerName = "";
        public int experiencePoints = 0;

        [Header("Ability Scores")]
        public int strength = 10;
        public int dexterity = 10;
        public int constitution = 10;
        public int intelligence = 10;
        public int wisdom = 10;
        public int charisma = 10;

        [Header("Combat Stats")]
        public int maxHitPoints = 10;
        public int currentHitPoints = 10;
        public int temporaryHitPoints = 0;
        public int armorClass = 10;
        public int initiative = 0;
        public int walkingSpeed = 30;
        public int flyingSpeed = 0;
        public int swimmingSpeed = 0;
        public int climbingSpeed = 0;
        public string hitDice = "";
        public int hitDiceUsed = 0;

        [Header("Proficiencies")]
        public List<string> proficientSavingThrows = new List<string>(); // "STR", "DEX", etc.
        [SerializeField] private List<string> _proficientSkillsStrings = new List<string>(); // Serialized as strings for JSON
        [SerializeField] private List<string> _expertiseSkillsStrings = new List<string>(); // Skills with expertise (double proficiency)
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
            // Expertise implies proficiency, so the proficient set is the union of both
            // lists (deduplicated). This keeps "is proficient" checks and styling correct
            // while expertise is tracked separately for the double-proficiency bonus.
            var skills = new List<DnD5eSkill>();
            ParseSkillsInto(_proficientSkillsStrings, skills);
            ParseSkillsInto(_expertiseSkillsStrings, skills);
            return skills;
        }

        /// <summary>
        /// Gets the list of skills the character has expertise in (double proficiency).
        /// </summary>
        public List<DnD5eSkill> GetExpertiseSkills()
        {
            var skills = new List<DnD5eSkill>();
            ParseSkillsInto(_expertiseSkillsStrings, skills);
            return skills;
        }

        private static void ParseSkillsInto(List<string> source, List<DnD5eSkill> target)
        {
            foreach (var skillString in source)
            {
                var skill = DnD5eSkillExtensions.FromString(skillString);
                if (skill.HasValue && !target.Contains(skill.Value))
                {
                    target.Add(skill.Value);
                }
            }
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
        /// Sets the list of expertise skills.
        /// Converts to string list for JSON serialization.
        /// </summary>
        public void SetExpertiseSkills(List<DnD5eSkill> skills)
        {
            _expertiseSkillsStrings.Clear();
            foreach (var skill in skills)
            {
                _expertiseSkillsStrings.Add(skill.GetDisplayName());
            }
        }

        /// <summary>
        /// Checks if the character is proficient in a skill (includes expertise).
        /// </summary>
        public bool IsProficientInSkill(DnD5eSkill skill)
        {
            return GetProficientSkills().Contains(skill);
        }

        /// <summary>
        /// Checks if the character has expertise (double proficiency) in a skill.
        /// </summary>
        public bool IsExpertInSkill(DnD5eSkill skill)
        {
            return GetExpertiseSkills().Contains(skill);
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
            int[] scores = { strength, dexterity, constitution, intelligence, wisdom, charisma };
            if (!DnD5eAbilityCodes.TryIndexFromCode(abilityName, out int idx) ||
                (uint)idx >= (uint)scores.Length)
                return 0;
            return CalculateModifier(scores[idx]);
        }

        /// <summary>
        /// Gets the skill modifier for a skill, including proficiency if applicable.
        /// </summary>
        public int GetSkillModifier(DnD5eSkill skill)
        {
            string abilityName = skill.GetAbilityScore();
            int abilityMod = GetAbilityModifier(abilityName);

            int proficiency = 0;
            if (IsExpertInSkill(skill))
                proficiency = proficiencyBonus * 2;
            else if (IsProficientInSkill(skill))
                proficiency = proficiencyBonus;

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

        // --- ICharacterSheet (ruleset-agnostic read surface) ---
        // Explicit implementation so the serialized lower-case fields above stay the
        // primary API while consumers can depend on the ruleset-agnostic abstraction.
        string ICharacterSheet.RulesetId => "DnD5e";
        string ICharacterSheet.CharacterName => characterName;
        int ICharacterSheet.Level => level;
        int ICharacterSheet.ProficiencyBonus => proficiencyBonus;
        IReadOnlyList<string> ICharacterSheet.ProficientWeapons => proficientWeapons;

        int ICharacterSheet.GetAbilityScore(string abilityCode)
        {
            int[] scores = { strength, dexterity, constitution, intelligence, wisdom, charisma };
            if (!DnD5eAbilityCodes.TryIndexFromCode(abilityCode, out int idx) ||
                (uint)idx >= (uint)scores.Length)
                return 0;
            return scores[idx];
        }

        int ICharacterSheet.GetAbilityModifier(string abilityCode) => GetAbilityModifier(abilityCode);

        string ICharacterSheet.GetSkillAbility(string skill)
        {
            var parsed = DnD5eSkillExtensions.FromString(skill);
            return parsed.HasValue ? parsed.Value.GetAbilityScore() : string.Empty;
        }

        int ICharacterSheet.GetSkillModifier(string skill) => GetSkillModifier(skill);

        bool ICharacterSheet.IsProficientInSkill(string skill)
        {
            var parsed = DnD5eSkillExtensions.FromString(skill);
            return parsed.HasValue && IsProficientInSkill(parsed.Value);
        }

        bool ICharacterSheet.HasExpertiseInSkill(string skill)
        {
            var parsed = DnD5eSkillExtensions.FromString(skill);
            return parsed.HasValue && IsExpertInSkill(parsed.Value);
        }
    }
}


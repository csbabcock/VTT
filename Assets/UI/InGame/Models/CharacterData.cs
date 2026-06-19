using System.Collections.Generic;
using GameCore.PlayerData.Rulesets;

namespace GameCore.UI.InGame.Models
{
    /// <summary>
    /// Represents character data including ability scores and modifiers.
    /// This is a simplified version that can be extended later.
    /// </summary>
    public class CharacterData
    {
        public string CharacterName { get; set; } = "Arlen";

        // Ability Scores
        public int Strength { get; set; } = 16;
        public int Dexterity { get; set; } = 14;
        public int Constitution { get; set; } = 14;
        public int Intelligence { get; set; } = 10;
        public int Wisdom { get; set; } = 12;
        public int Charisma { get; set; } = 8;

        // Proficiency Bonus
        public int ProficiencyBonus { get; set; } = 2;

        // Skill Proficiencies (includes expertise skills, since expertise implies proficiency)
        public HashSet<string> ProficientSkills { get; set; } = new HashSet<string> { "Athletics" };

        // Skills with expertise (double proficiency bonus)
        public HashSet<string> ExpertiseSkills { get; set; } = new HashSet<string>();

        /// <summary>
        /// Calculates the ability modifier from an ability score.
        /// </summary>
        public static int GetAbilityModifier(int abilityScore)
        {
            return (abilityScore - 10) / 2;
        }

        /// <summary>
        /// Gets the modifier for a specific ability.
        /// </summary>
        public int GetAbilityModifier(string abilityName)
        {
            int[] scores = { Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma };
            if (!DnD5eAbilityCodes.TryIndexFromCode(abilityName, out int idx) ||
                (uint)idx >= (uint)scores.Length)
                return 0;
            return GetAbilityModifier(scores[idx]);
        }

        /// <summary>
        /// Gets the skill modifier for a skill, including proficiency if applicable.
        /// </summary>
        public int GetSkillModifier(string skillName, string abilityName)
        {
            int abilityMod = GetAbilityModifier(abilityName);

            int proficiency = 0;
            if (ExpertiseSkills.Contains(skillName))
                proficiency = ProficiencyBonus * 2;
            else if (ProficientSkills.Contains(skillName))
                proficiency = ProficiencyBonus;

            return abilityMod + proficiency;
        }

        /// <summary>
        /// Gets the ability name associated with a skill.
        /// </summary>
        public static string GetSkillAbility(string skillName)
        {
            // D&D 5e skill-to-ability mapping
            return skillName switch
            {
                "Acrobatics" => "DEX",
                "Animal Handling" => "WIS",
                "Arcana" => "INT",
                "Athletics" => "STR",
                "Deception" => "CHA",
                "History" => "INT",
                "Insight" => "WIS",
                "Intimidation" => "CHA",
                "Investigation" => "INT",
                "Medicine" => "WIS",
                "Nature" => "INT",
                "Perception" => "WIS",
                "Performance" => "CHA",
                "Persuasion" => "CHA",
                "Religion" => "INT",
                "Sleight of Hand" => "DEX",
                "Stealth" => "DEX",
                "Survival" => "WIS",
                _ => "STR"
            };
        }
    }
}


using System.Collections.Generic;

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

        // Skill Proficiencies
        public HashSet<string> ProficientSkills { get; set; } = new HashSet<string> { "Athletics" };

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
            return abilityName.ToUpper() switch
            {
                "STR" => GetAbilityModifier(Strength),
                "DEX" => GetAbilityModifier(Dexterity),
                "CON" => GetAbilityModifier(Constitution),
                "INT" => GetAbilityModifier(Intelligence),
                "WIS" => GetAbilityModifier(Wisdom),
                "CHA" => GetAbilityModifier(Charisma),
                _ => 0
            };
        }

        /// <summary>
        /// Gets the skill modifier for a skill, including proficiency if applicable.
        /// </summary>
        public int GetSkillModifier(string skillName, string abilityName)
        {
            int abilityMod = GetAbilityModifier(abilityName);
            bool isProficient = ProficientSkills.Contains(skillName);
            int proficiency = isProficient ? ProficiencyBonus : 0;
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


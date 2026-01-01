using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Maps UI element names and structure for the character sheet.
    /// Follows Single Responsibility Principle - only handles UI structure mapping.
    /// Separates UI structure knowledge from update logic.
    /// </summary>
    public static class CharacterSheetUIMapper
    {
        /// <summary>
        /// Gets the mapping of ability score button names to ability score names.
        /// </summary>
        public static Dictionary<string, string> GetAbilityScoreMapping()
        {
            return new Dictionary<string, string>
            {
                { "ability-str", "STR" },
                { "ability-dex", "DEX" },
                { "ability-con", "CON" },
                { "ability-int", "INT" },
                { "ability-wis", "WIS" },
                { "ability-cha", "CHA" }
            };
        }

        /// <summary>
        /// Gets the mapping of skill button names to skill display names.
        /// </summary>
        public static Dictionary<string, string> GetSkillMapping()
        {
            return new Dictionary<string, string>
            {
                { "skill-acrobatics", "Acrobatics" },
                { "skill-animal-handling", "Animal Handling" },
                { "skill-arcana", "Arcana" },
                { "skill-athletics", "Athletics" },
                { "skill-deception", "Deception" },
                { "skill-history", "History" },
                { "skill-insight", "Insight" },
                { "skill-intimidation", "Intimidation" },
                { "skill-investigation", "Investigation" },
                { "skill-medicine", "Medicine" },
                { "skill-nature", "Nature" },
                { "skill-perception", "Perception" },
                { "skill-performance", "Performance" },
                { "skill-persuasion", "Persuasion" },
                { "skill-religion", "Religion" },
                { "skill-sleight-of-hand", "Sleight of Hand" },
                { "skill-stealth", "Stealth" },
                { "skill-survival", "Survival" }
            };
        }

        /// <summary>
        /// Gets the mapping of attack button names to weapon names.
        /// </summary>
        public static Dictionary<string, string> GetAttackMapping()
        {
            return new Dictionary<string, string>
            {
                { "attack-longsword", "Longsword" },
                { "attack-shortbow", "Shortbow" }
            };
        }

        /// <summary>
        /// Gets the name of the character name label element.
        /// </summary>
        public static string GetCharacterNameElementName() => "character-name";

        /// <summary>
        /// Gets the name of the character details label element.
        /// </summary>
        public static string GetCharacterDetailsElementName() => "character-details";

        /// <summary>
        /// Gets the class name for skill modifier labels.
        /// </summary>
        public static string GetSkillModifierClassName() => "skill-modifier";

        /// <summary>
        /// Gets the class name for skill icon elements.
        /// </summary>
        public static string GetSkillIconClassName() => "skill-icon";

        /// <summary>
        /// Gets the class name for skill icon spacer elements.
        /// </summary>
        public static string GetSkillIconSpacerClassName() => "skill-icon-spacer";

        /// <summary>
        /// Gets the class name for proficient skills.
        /// </summary>
        public static string GetProficientClassName() => "proficient";
    }
}

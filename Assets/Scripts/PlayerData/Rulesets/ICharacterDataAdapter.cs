using GameCore.UI.InGame.Models;
using System.Collections.Generic;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Interface for adapting ruleset-specific character data to a generic UI-friendly format.
    /// Follows Adapter Pattern - allows different ruleset data structures to be used by generic UI code.
    /// </summary>
    public interface ICharacterDataAdapter
    {
        /// <summary>
        /// Gets the ruleset identifier this adapter supports.
        /// </summary>
        string RulesetId { get; }

        /// <summary>
        /// Gets ability scores as a dictionary.
        /// </summary>
        Dictionary<string, int> GetAbilityScores(object rulesetData);

        /// <summary>
        /// Gets ability modifiers as a dictionary.
        /// </summary>
        Dictionary<string, int> GetAbilityModifiers(object rulesetData, IRulesetCalculator calculator);

        /// <summary>
        /// Gets skill modifiers as a dictionary.
        /// </summary>
        Dictionary<string, int> GetSkillModifiers(object rulesetData, IRulesetCalculator calculator);

        /// <summary>
        /// Gets list of proficient skills.
        /// </summary>
        List<string> GetProficientSkills(object rulesetData);

        /// <summary>
        /// Gets weapon data for a specific weapon.
        /// </summary>
        WeaponData GetWeaponData(string weaponName, object rulesetData, IRulesetCalculator calculator);
    }
}


using System.Collections.Generic;
using GameCore.UI.InGame.Models;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Interface for ruleset-specific calculation logic.
    /// Follows Strategy Pattern and Dependency Inversion Principle.
    /// Allows different rulesets (D&D 5e, Pathfinder, etc.) to have their own calculation implementations.
    /// </summary>
    public interface IRulesetCalculator
    {
        /// <summary>
        /// Gets the ruleset identifier (e.g., "DnD5e", "Pathfinder2e").
        /// </summary>
        string RulesetId { get; }

        /// <summary>
        /// Calculates ability modifier from ability score.
        /// </summary>
        /// <param name="abilityScore">The ability score</param>
        /// <returns>The ability modifier</returns>
        int CalculateAbilityModifier(int abilityScore);

        /// <summary>
        /// Calculates proficiency bonus based on level.
        /// </summary>
        /// <param name="level">Character level</param>
        /// <returns>Proficiency bonus</returns>
        int CalculateProficiencyBonus(int level);

        /// <summary>
        /// Calculates skill modifier.
        /// </summary>
        /// <param name="abilityModifier">The relevant ability modifier</param>
        /// <param name="isProficient">Whether the character is proficient in the skill</param>
        /// <param name="level">Character level (for proficiency bonus calculation)</param>
        /// <returns>Total skill modifier</returns>
        int CalculateSkillModifier(int abilityModifier, bool isProficient, int level);

        /// <summary>
        /// Calculates skill modifier, accounting for expertise (double proficiency).
        /// </summary>
        /// <param name="abilityModifier">The relevant ability modifier</param>
        /// <param name="isProficient">Whether the character is proficient in the skill</param>
        /// <param name="hasExpertise">Whether the character has expertise (doubles proficiency)</param>
        /// <param name="level">Character level (for proficiency bonus calculation)</param>
        /// <returns>Total skill modifier</returns>
        int CalculateSkillModifier(int abilityModifier, bool isProficient, bool hasExpertise, int level);

        /// <summary>
        /// Calculates saving throw modifier.
        /// </summary>
        /// <param name="abilityModifier">The relevant ability modifier</param>
        /// <param name="isProficient">Whether the character is proficient in the saving throw</param>
        /// <param name="level">Character level (for proficiency bonus calculation)</param>
        /// <returns>Total saving throw modifier</returns>
        int CalculateSavingThrowModifier(int abilityModifier, bool isProficient, int level);

        /// <summary>
        /// Calculates weapon attack bonus.
        /// </summary>
        /// <param name="weaponName">Name of the weapon</param>
        /// <param name="abilityModifier">Relevant ability modifier (STR or DEX)</param>
        /// <param name="isProficient">Whether proficient with the weapon</param>
        /// <param name="level">Character level (for proficiency bonus)</param>
        /// <returns>Attack bonus</returns>
        int CalculateWeaponAttackBonus(string weaponName, int abilityModifier, bool isProficient, int level);

        /// <summary>
        /// Calculates weapon damage modifier.
        /// </summary>
        /// <param name="weaponName">Name of the weapon</param>
        /// <param name="abilityModifier">Relevant ability modifier (STR or DEX)</param>
        /// <returns>Damage modifier (proficiency does NOT apply to damage)</returns>
        int CalculateWeaponDamageModifier(string weaponName, int abilityModifier);

        /// <summary>
        /// Gets the ability score used for a weapon's attack and damage.
        /// </summary>
        /// <param name="weaponName">Name of the weapon</param>
        /// <param name="strengthModifier">STR modifier</param>
        /// <param name="dexterityModifier">DEX modifier</param>
        /// <returns>The ability modifier to use (for finesse weapons, returns the higher)</returns>
        int GetWeaponAbilityModifier(string weaponName, int strengthModifier, int dexterityModifier);

        /// <summary>
        /// Gets weapon properties (damage dice, damage type, etc.).
        /// </summary>
        /// <param name="weaponName">Name of the weapon</param>
        /// <returns>Weapon properties, or null if weapon not found</returns>
        WeaponProperties? GetWeaponProperties(string weaponName);

        /// <summary>
        /// Checks if a character is proficient with a weapon.
        /// </summary>
        /// <param name="weaponName">Name of the weapon</param>
        /// <param name="proficientWeapons">List of weapons/categories the character is proficient with</param>
        /// <returns>True if proficient</returns>
        bool IsProficientWithWeapon(string weaponName, List<string> proficientWeapons);

        /// <summary>
        /// Gets all available skills for this ruleset.
        /// </summary>
        /// <returns>Dictionary mapping skill IDs to display names</returns>
        Dictionary<string, string> GetAvailableSkills();

        /// <summary>
        /// Gets the ability score name associated with a skill.
        /// </summary>
        /// <param name="skillId">Skill identifier</param>
        /// <returns>Ability score name (e.g., "STR", "DEX")</returns>
        string GetSkillAbilityScore(string skillId);
    }

    /// <summary>
    /// Weapon properties structure.
    /// </summary>
    public struct WeaponProperties
    {
        public string Name;
        public int DamageDice;
        public int DamageDieType;
        public string DamageType;
        public bool IsFinesse;
        public bool IsRanged;
        public string Category; // "Simple", "Martial", etc.
        
        // Note: This is a separate struct from DnD5eWeaponCalculator.WeaponProperties
        // to maintain separation of concerns and allow different rulesets to have different structures
    }
}


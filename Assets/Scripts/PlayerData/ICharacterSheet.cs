using System.Collections.Generic;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Ruleset-agnostic read interface over a character's sheet data.
    /// UI and gameplay systems depend on this abstraction rather than a concrete
    /// ruleset model (e.g. <see cref="DnD5eCharacterData"/>), so additional rulesets
    /// can be supported without changing consumers (Dependency Inversion).
    /// Ability/stat access is keyed by ruleset-defined codes; for DnD5e these are
    /// "STR", "DEX", "CON", "INT", "WIS", "CHA".
    /// </summary>
    public interface ICharacterSheet
    {
        /// <summary>Identifier of the ruleset that produced this sheet (e.g. "DnD5e").</summary>
        string RulesetId { get; }

        /// <summary>Display name of the character.</summary>
        string CharacterName { get; }

        /// <summary>Character level (or the ruleset's equivalent progression value).</summary>
        int Level { get; }

        /// <summary>Proficiency bonus (or ruleset equivalent) for the current level.</summary>
        int ProficiencyBonus { get; }

        /// <summary>Weapon proficiency identifiers/categories the character has.</summary>
        IReadOnlyList<string> ProficientWeapons { get; }

        /// <summary>Raw ability/attribute score for the given ruleset code, or 0 if unknown.</summary>
        int GetAbilityScore(string abilityCode);

        /// <summary>Derived ability/attribute modifier for the given ruleset code, or 0 if unknown.</summary>
        int GetAbilityModifier(string abilityCode);

        // --- Skills ---
        // Skills are identified by ruleset-defined names/ids (for DnD5e these are display
        // names such as "Athletics"). Consumers stay ruleset-agnostic by keying on strings.

        /// <summary>Ability code (e.g. "DEX") a skill is based on, or "" if unknown.</summary>
        string GetSkillAbility(string skill);

        /// <summary>Total modifier for a skill, including proficiency/expertise, or 0 if unknown.</summary>
        int GetSkillModifier(string skill);

        /// <summary>Whether the character is proficient (or has expertise) in a skill.</summary>
        bool IsProficientInSkill(string skill);

        /// <summary>Whether the character has expertise (doubled proficiency) in a skill.</summary>
        bool HasExpertiseInSkill(string skill);
    }
}

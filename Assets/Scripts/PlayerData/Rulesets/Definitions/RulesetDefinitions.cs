using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.PlayerData.Rulesets.Definitions
{
    /// <summary>
    /// Simple ability score bonus definition (e.g., STR +2).
    /// </summary>
    [Serializable]
    public class AbilityScoreBonusDefinition
    {
        public string ability; // "STR", "DEX", etc.
        public int bonus;
    }

    /// <summary>
    /// Generic feature definition used by races, classes, backgrounds, etc.
    /// </summary>
    [Serializable]
    public class FeatureDefinition
    {
        public string id;          // Stable identifier, e.g., "feature.dwarven_resilience"
        public string name;        // Display name
        [TextArea]
        public string description; // Long-form rules / lore text
    }

    /// <summary>
    /// Race definition for a ruleset.
    /// Designed to be JSON-serialized with Unity's JsonUtility.
    /// </summary>
    [Serializable]
    public class RaceDefinition
    {
        public string id;                   // e.g., "race.hill_dwarf"
        public string name;                 // e.g., "Hill Dwarf"
        [TextArea]
        public string description;

        public List<AbilityScoreBonusDefinition> abilityScoreBonuses;
        public int speed;                   // Base walking speed in feet
        public string size;                 // e.g., "Small", "Medium"

        public bool hasDarkvision;
        public int darkvisionRange;         // In feet, if hasDarkvision is true

        public List<string> traits;         // Free-form trait names
        public List<FeatureDefinition> features;
    }

    /// <summary>
    /// Class feature gained at a specific level.
    /// </summary>
    [Serializable]
    public class ClassFeatureByLevelDefinition
    {
        public int level;
        public FeatureDefinition feature;
    }

    /// <summary>
    /// Class definition for a ruleset.
    /// </summary>
    [Serializable]
    public class ClassDefinition
    {
        public string id;                   // e.g., "class.fighter"
        public string name;                 // e.g., "Fighter"
        [TextArea]
        public string description;

        public int hitDie;                  // e.g., 10 for d10
        public string primaryAbility;       // e.g., "STR"
        public string spellcastingAbility;  // e.g., "INT", or empty if none

        public List<string> savingThrowProficiencies; // e.g., ["STR", "CON"]
        public List<string> armorProficiencies;       // e.g., ["Light", "Medium", "Heavy", "Shield"]
        public List<string> weaponProficiencies;      // e.g., ["Simple", "Martial"]

        public List<ClassFeatureByLevelDefinition> featuresByLevel;
    }

    /// <summary>
    /// Background definition for a ruleset.
    /// </summary>
    [Serializable]
    public class BackgroundDefinition
    {
        public string id;                   // e.g., "background.acolyte"
        public string name;                 // e.g., "Acolyte"
        [TextArea]
        public string description;

        public List<string> skillProficiencies;   // Skill IDs
        public List<string> toolProficiencies;    // Tool IDs or names
        public List<FeatureDefinition> features;
    }

    /// <summary>
    /// Skill definition for a ruleset.
    /// </summary>
    [Serializable]
    public class SkillDefinition
    {
        public string id;             // e.g., "skill.athletics"
        public string name;           // e.g., "Athletics"
        public string ability;        // e.g., "STR"
    }

    /// <summary>
    /// Top-level manifest for a ruleset.
    /// </summary>
    [Serializable]
    public class RulesetDefinition
    {
        public string id;                 // e.g., "DnD5e"
        public string displayName;        // e.g., "Dungeons & Dragons 5th Edition"
        public string version;            // e.g., "1.0.0"

        public List<RaceDefinition> races;
        public List<ClassDefinition> classes;
        public List<BackgroundDefinition> backgrounds;
        public List<SkillDefinition> skills;
    }
}


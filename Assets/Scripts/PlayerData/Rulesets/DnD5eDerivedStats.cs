using System;
using System.Collections.Generic;
using GameCore.PlayerData.Rulesets.Definitions;
using UnityEngine;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// D&amp;D 5e combat totals derived from class content, level, and ability modifiers.
    /// Shared by character creation previews and the in-game character sheet.
    /// </summary>
    public static class DnD5eDerivedStats
    {
        public static bool TryResolveClassDefinition(
            IReadOnlyCollection<ClassDefinition> classes,
            string characterClassDisplayName,
            out ClassDefinition classDef)
        {
            classDef = null;
            if (classes == null || string.IsNullOrWhiteSpace(characterClassDisplayName))
                return false;
            string trimmed = characterClassDisplayName.Trim();
            foreach (ClassDefinition c in classes)
            {
                if (c == null || string.IsNullOrEmpty(c.name))
                    continue;
                if (string.Equals(c.name, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    classDef = c;
                    return true;
                }
            }

            return false;
        }

        /// <summary>D&amp;D 5e: max hit die + CON at 1st level, then rounded-up average + CON per level thereafter.</summary>
        public static int CalculateMaxHitPointsForLevel(ClassDefinition classDef, int conModifier, int level)
        {
            if (classDef == null || level < 1)
                return Mathf.Max(1, 8 + conModifier);
            int die = classDef.hitDie >= 4 ? classDef.hitDie : 8;
            int avg = AverageHitDieIncreasePerLevel(die);
            return die + conModifier + (level - 1) * (avg + conModifier);
        }

        public static int AverageHitDieIncreasePerLevel(int hitDie)
        {
            return hitDie switch
            {
                12 => 7,
                10 => 6,
                8 => 5,
                6 => 4,
                _ => Mathf.Max(1, hitDie / 2 + 1)
            };
        }

        /// <summary>
        /// Unarmored AC baseline: Barbarian adds CON; Monk adds WIS; others use DEX only (no equipped armor yet).
        /// </summary>
        public static int CalculateUnarmoredArmorClass(
            ClassDefinition classDef,
            int dexModifier,
            int conModifier,
            int wisModifier)
        {
            if (classDef?.id != null)
            {
                if (classDef.id.Equals("class.barbarian", StringComparison.OrdinalIgnoreCase))
                    return 10 + dexModifier + conModifier;
                if (classDef.id.Equals("class.monk", StringComparison.OrdinalIgnoreCase))
                    return 10 + dexModifier + wisModifier;
            }

            return 10 + dexModifier;
        }
    }
}

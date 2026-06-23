using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;
using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Shared hit-point math for UI display and server-authoritative updates.
    /// Keeps clamping and max-HP derivation in one place.
    /// </summary>
    public static class CharacterHitPoints
    {
        /// <summary>
        /// Returns the display max HP for a character, matching the in-game sheet header.
        /// </summary>
        public static int GetDisplayMaxHp(DnD5eCharacterData data)
        {
            if (data == null)
                return 1;

            IRulesetContentQuery query = RulesetContentQueryProvider.GetOrCreate("DnD5e");
            ClassDefinition classDef = null;
            if (!string.IsNullOrWhiteSpace(data.characterClass))
                DnD5eDerivedStats.TryResolveClassDefinition(query.GetClasses(), data.characterClass, out classDef);

            int level = Mathf.Max(1, data.level);
            return DnD5eDerivedStats.CalculateMaxHitPointsForLevel(classDef, data.constitutionModifier, level);
        }

        /// <summary>Clamps current HP to [0, max]. Max is floored at 1.</summary>
        public static int ClampCurrent(int current, int maxHp)
        {
            maxHp = Mathf.Max(1, maxHp);
            return Mathf.Clamp(current, 0, maxHp);
        }

        /// <summary>Sets current HP to the ruleset-derived maximum for the character's level and class.</summary>
        public static void EnsureFullHealth(DnD5eCharacterData data)
        {
            if (data == null)
                return;

            int maxHp = GetDisplayMaxHp(data);
            data.maxHitPoints = maxHp;
            data.currentHitPoints = maxHp;
        }
    }
}

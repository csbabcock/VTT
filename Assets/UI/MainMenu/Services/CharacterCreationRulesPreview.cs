using System;
using System.Collections.Generic;
using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;
using GameCore.UI.MainMenu;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Builds character-creation preview values (combat/proficiencies) from ruleset definitions.
    /// Keeps <see cref="CharacterCreationPresenter"/> focused on orchestration (SRP).
    /// </summary>
    public static class CharacterCreationRulesPreview
    {
        private const int MinimumHitDieSides = 4;
        private const int AbilityCount = 6;
        private const int DexterityIndex = 1;
        private const int ConstitutionIndex = 2;
        private const int WisdomIndex = 4;

        /// <summary>
        /// Unarmored AC preview: respects Barbarian (needs CON) / Monk (needs WIS) before using modifiers.
        /// </summary>
        public static int? ComputeUnarmoredArmorClassPreview(
            ClassDefinition classDef,
            int[] abilityScores,
            IRulesetCalculator calculator)
        {
            if (abilityScores == null || abilityScores.Length < AbilityCount || calculator == null)
                return null;
            if (abilityScores[DexterityIndex] < 0)
                return null;

            int dexMod = calculator.CalculateAbilityModifier(abilityScores[DexterityIndex]);

            if (IsBarbarian(classDef) && abilityScores[ConstitutionIndex] < 0)
                return null;
            if (IsMonk(classDef) && abilityScores[WisdomIndex] < 0)
                return null;

            int conMod = abilityScores[ConstitutionIndex] >= 0
                ? calculator.CalculateAbilityModifier(abilityScores[ConstitutionIndex])
                : 0;
            int wisMod = abilityScores[WisdomIndex] >= 0
                ? calculator.CalculateAbilityModifier(abilityScores[WisdomIndex])
                : 0;

            return DnD5eDerivedStats.CalculateUnarmoredArmorClass(classDef, dexMod, conMod, wisMod);
        }

        /// <summary>Returns null when hit dice cannot be shown (no class or invalid die).</summary>
        public static string FormatHitDicePool(ClassDefinition classDef, int level)
        {
            if (classDef == null || level < 1 || classDef.hitDie < MinimumHitDieSides)
                return null;
            return $"{level}d{classDef.hitDie}";
        }

        /// <summary>
        /// Merges class and background proficiency lists for the proficiencies panel.
        /// </summary>
        public static List<CharacterProficiencySection> BuildProficiencySections(
            ClassDefinition classDef,
            BackgroundDefinition background)
        {
            var list = new List<CharacterProficiencySection>();
            AddIfNonEmpty(list, "Saving throws", classDef?.savingThrowProficiencies);
            AddIfNonEmpty(list, "Armor", classDef?.armorProficiencies);
            AddIfNonEmpty(list, "Weapons", classDef?.weaponProficiencies);
            AddIfNonEmpty(list, "Skills (background)", background?.skillProficiencies);
            AddIfNonEmpty(list, "Tools (background)", background?.toolProficiencies);
            return list;
        }

        private static void AddIfNonEmpty(
            List<CharacterProficiencySection> destination,
            string title,
            IReadOnlyList<string> items)
        {
            if (string.IsNullOrEmpty(title) || items == null || items.Count == 0)
                return;
            destination.Add(new CharacterProficiencySection(title, items));
        }

        private static bool IsBarbarian(ClassDefinition classDef) =>
            classDef?.id != null &&
            classDef.id.Equals(DnD5eClassIds.Barbarian, StringComparison.OrdinalIgnoreCase);

        private static bool IsMonk(ClassDefinition classDef) =>
            classDef?.id != null &&
            classDef.id.Equals(DnD5eClassIds.Monk, StringComparison.OrdinalIgnoreCase);
    }
}

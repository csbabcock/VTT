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
            BackgroundDefinition background,
            RaceDefinition race = null)
        {
            var list = new List<CharacterProficiencySection>();
            AddIfNonEmpty(list, "Saving throws", classDef?.savingThrowProficiencies);
            AddIfNonEmpty(list, "Armor", classDef?.armorProficiencies);
            AddIfNonEmpty(list, "Weapons", classDef?.weaponProficiencies);
            AddIfNonEmpty(list, "Skills (background)", background?.skillProficiencies);
            AddIfNonEmpty(list, "Tools (background)", background?.toolProficiencies);
            AddRaceProficiencies(list, race);
            return list;
        }

        public static string FormatRaceSpeed(RaceDefinition race)
        {
            if (race == null)
                return "—";

            var parts = new List<string>();
            if (race.speed > 0)
                parts.Add($"{race.speed} ft");

            if (race.mechanicalEffects != null)
            {
                foreach (MechanicalEffectDefinition effect in race.mechanicalEffects)
                {
                    if (effect == null || !IsEffectType(effect, "speed"))
                        continue;
                    string mode = string.IsNullOrEmpty(effect.target) ? "speed" : effect.target;
                    string value = !string.IsNullOrEmpty(effect.value)
                        ? effect.value
                        : effect.amount > 0 ? $"{effect.amount} ft" : string.Empty;
                    if (!string.IsNullOrEmpty(value))
                        parts.Add($"{mode}: {value}");
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "—";
        }

        public static string FormatRaceSenses(RaceDefinition race)
        {
            if (race == null)
                return "—";

            var parts = new List<string>();
            if (race.hasDarkvision)
                parts.Add($"Darkvision {race.darkvisionRange} ft");

            if (race.mechanicalEffects != null)
            {
                foreach (MechanicalEffectDefinition effect in race.mechanicalEffects)
                {
                    if (effect == null || !IsEffectType(effect, "sense"))
                        continue;
                    string name = string.IsNullOrEmpty(effect.name) ? effect.target : effect.name;
                    string value = !string.IsNullOrEmpty(effect.value)
                        ? effect.value
                        : effect.amount > 0 ? $"{effect.amount} ft" : string.Empty;
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                        parts.Add($"{name} {value}");
                    else if (!string.IsNullOrEmpty(name))
                        parts.Add(name);
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "—";
        }

        public static int? ComputeRaceArmorClassPreview(
            RaceDefinition race,
            int[] abilityScores,
            IRulesetCalculator calculator)
        {
            if (race?.mechanicalEffects == null || abilityScores == null ||
                abilityScores.Length < AbilityCount || calculator == null)
                return null;

            foreach (MechanicalEffectDefinition effect in race.mechanicalEffects)
            {
                if (effect == null || !IsEffectType(effect, "naturalArmor"))
                    continue;
                int baseAc = effect.amount > 0 ? effect.amount : 13;
                int dexMod = abilityScores[DexterityIndex] >= 0
                    ? calculator.CalculateAbilityModifier(abilityScores[DexterityIndex])
                    : 0;
                return baseAc + dexMod;
            }

            return null;
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

        private static void AddRaceProficiencies(
            List<CharacterProficiencySection> destination,
            RaceDefinition race)
        {
            if (race?.mechanicalEffects == null)
                return;

            var skills = new List<string>();
            var tools = new List<string>();
            var weapons = new List<string>();
            var armor = new List<string>();
            var defenses = new List<string>();
            var languages = new List<string>();
            foreach (MechanicalEffectDefinition effect in race.mechanicalEffects)
            {
                if (effect == null)
                    continue;
                if (IsEffectType(effect, "proficiency"))
                {
                    string display = !string.IsNullOrEmpty(effect.value)
                        ? effect.value
                        : !string.IsNullOrEmpty(effect.name) ? effect.name : effect.target;
                    AddRaceEffectToBucket(effect.target, display, skills, tools, weapons, armor);
                }
                else if (IsEffectType(effect, "resistance") || IsEffectType(effect, "defense"))
                {
                    string display = !string.IsNullOrEmpty(effect.value)
                        ? effect.value
                        : !string.IsNullOrEmpty(effect.target) ? effect.target : effect.name;
                    if (!string.IsNullOrEmpty(display))
                        defenses.Add(display);
                }
                else if (IsEffectType(effect, "language"))
                {
                    string display = !string.IsNullOrEmpty(effect.value) ? effect.value : effect.target;
                    if (!string.IsNullOrEmpty(display))
                        languages.Add(display);
                }
            }

            AddIfNonEmpty(destination, "Skills (race)", skills);
            AddIfNonEmpty(destination, "Tools (race)", tools);
            AddIfNonEmpty(destination, "Weapons (race)", weapons);
            AddIfNonEmpty(destination, "Armor (race)", armor);
            AddIfNonEmpty(destination, "Defenses (race)", defenses);
            AddIfNonEmpty(destination, "Languages (race)", languages);
        }

        private static void AddRaceEffectToBucket(
            string target,
            string display,
            List<string> skills,
            List<string> tools,
            List<string> weapons,
            List<string> armor)
        {
            if (string.IsNullOrEmpty(display))
                return;
            switch ((target ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "skill":
                case "skills":
                    skills.Add(display);
                    break;
                case "tool":
                case "tools":
                    tools.Add(display);
                    break;
                case "weapon":
                case "weapons":
                    weapons.Add(display);
                    break;
                case "armor":
                    armor.Add(display);
                    break;
                default:
                    skills.Add(display);
                    break;
            }
        }

        private static bool IsEffectType(MechanicalEffectDefinition effect, string type) =>
            effect?.type != null &&
            effect.type.Equals(type, StringComparison.OrdinalIgnoreCase);

        private static bool IsBarbarian(ClassDefinition classDef) =>
            classDef?.id != null &&
            classDef.id.Equals(DnD5eClassIds.Barbarian, StringComparison.OrdinalIgnoreCase);

        private static bool IsMonk(ClassDefinition classDef) =>
            classDef?.id != null &&
            classDef.id.Equals(DnD5eClassIds.Monk, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Spell save DC and spell attack when the class is a spellcaster and the key ability score is assigned.
        /// </summary>
        public static bool TryGetSpellcastingPreview(
            ClassDefinition classDef,
            int[] abilityScores,
            int level,
            IRulesetCalculator calculator,
            out int spellSaveDc,
            out int spellAttack)
        {
            spellSaveDc = 0;
            spellAttack = 0;
            if (calculator == null || !IsSpellcastingClass(classDef) ||
                !HasSpellcastingAbilityAssigned(classDef, abilityScores))
                return false;

            int castingModifier = GetSpellcastingAbilityModifier(classDef, abilityScores, calculator);
            int prof = calculator.CalculateProficiencyBonus(level);
            spellSaveDc = 8 + prof + castingModifier;
            spellAttack = prof + castingModifier;
            return true;
        }

        private static bool IsSpellcastingClass(ClassDefinition classDef) =>
            classDef != null && !string.IsNullOrEmpty(classDef.spellcastingAbility);

        private static bool HasSpellcastingAbilityAssigned(ClassDefinition classDef, int[] abilityScores)
        {
            if (classDef == null || abilityScores == null || abilityScores.Length < AbilityCount)
                return false;
            if (!DnD5eAbilityCodes.TryIndexFromCode(classDef.spellcastingAbility, out int idx))
                return false;
            return idx >= 0 && idx < abilityScores.Length && abilityScores[idx] >= 0;
        }

        private static int GetSpellcastingAbilityModifier(
            ClassDefinition classDef,
            int[] abilityScores,
            IRulesetCalculator calculator)
        {
            if (!DnD5eAbilityCodes.TryIndexFromCode(classDef.spellcastingAbility, out int idx) ||
                abilityScores == null || idx >= abilityScores.Length)
                return 0;
            return calculator.CalculateAbilityModifier(abilityScores[idx]);
        }
    }
}

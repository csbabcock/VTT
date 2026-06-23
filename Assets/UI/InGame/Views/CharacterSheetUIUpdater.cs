using GameCore.UI.InGame.Models;
using GameCore.PlayerData;
using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Character sheet UI updater using ruleset-agnostic architecture.
    /// Follows SOLID principles:
    /// - Single Responsibility: Only handles UI updates
    /// - Open/Closed: Can extend with new rulesets without modification
    /// - Dependency Inversion: Depends on abstractions (ICharacterSheet, IRulesetCalculator, ICharacterDataAdapter)
    /// </summary>
    public static class CharacterSheetUIUpdater
    {
        /// <summary>
        /// Updates all character sheet UI elements from a character sheet.
        /// Uses ruleset calculator and adapter for ruleset-agnostic updates.
        /// </summary>
        public static void UpdateCharacterSheet(VisualElement root, ICharacterSheet sheet, string rulesetId = null)
        {
            if (root == null)
            {
                Debug.LogWarning("CharacterSheetUIUpdater: Root element is null!");
                return;
            }

            if (sheet == null)
            {
                Debug.LogWarning("CharacterSheetUIUpdater: Character sheet is null!");
                return;
            }

            rulesetId = string.IsNullOrEmpty(rulesetId) ? sheet.RulesetId : rulesetId;

            // Get ruleset calculator and adapter for this sheet's ruleset.
            var calculator = RulesetCalculatorFactory.GetCalculator(rulesetId) ?? RulesetCalculatorFactory.GetDefaultCalculator();
            var adapter = RulesetAdapterFactory.GetAdapter(rulesetId) ?? RulesetAdapterFactory.GetDefaultAdapter();

            // The sheet IS the ruleset domain object the adapter consumes.
            object rulesetData = sheet;

            UpdateCharacterName(root, sheet);
            if (rulesetId == "DnD5e" && sheet is DnD5eCharacterData dnd)
            {
                UpdateCombatHeader(root, dnd, calculator);
            }

            UpdateAbilityScores(root, adapter, calculator, rulesetData);
            UpdateSkills(root, adapter, calculator, rulesetData);
            UpdateAttacks(root, adapter, calculator, rulesetData);
        }

        /// <summary>
        /// Updates character name in the header.
        /// </summary>
        private static void UpdateCharacterName(VisualElement root, ICharacterSheet sheet)
        {
            var nameLabel = root.Q<Label>(CharacterSheetUIMapper.GetCharacterNameElementName());
            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrEmpty(sheet.CharacterName) ? "—" : sheet.CharacterName;
            }
        }

        /// <summary>
        /// Updates header combat stats (HP, AC, initiative, speed) and subtitle from class, level, and abilities.
        /// </summary>
        private static void UpdateCombatHeader(VisualElement root, DnD5eCharacterData data, IRulesetCalculator calculator)
        {
            if (root == null || data == null || calculator == null)
                return;

            IRulesetContentQuery query = RulesetContentQueryProvider.GetOrCreate("DnD5e");
            ClassDefinition classDef = null;
            if (!string.IsNullOrWhiteSpace(data.characterClass))
                DnD5eDerivedStats.TryResolveClassDefinition(query.GetClasses(), data.characterClass, out classDef);

            int level = Mathf.Max(1, data.level);
            int maxHp = DnD5eDerivedStats.CalculateMaxHitPointsForLevel(classDef, data.constitutionModifier, level);
            int ac = DnD5eDerivedStats.CalculateUnarmoredArmorClass(classDef,
                data.dexterityModifier, data.constitutionModifier, data.wisdomModifier);

            int displayCurrent = Mathf.Clamp(data.currentHitPoints, 0, maxHp);

            SetLabelText(root, CharacterSheetUIMapper.GetHitPointsValueElementName(), $"{displayCurrent} / {maxHp}");
            SetLabelText(root, CharacterSheetUIMapper.GetTempHitPointsValueElementName(),
                data.temporaryHitPoints > 0 ? $"+{data.temporaryHitPoints}" : "0");
            SetLabelText(root, CharacterSheetUIMapper.GetArmorClassValueElementName(), ac.ToString());
            SetLabelText(root, CharacterSheetUIMapper.GetInitiativeValueElementName(),
                FormatSignedModifier(data.dexterityModifier));
            int speed = data.walkingSpeed > 0 ? data.walkingSpeed : 30;
            SetLabelText(root, CharacterSheetUIMapper.GetSpeedValueElementName(), $"{speed} / {speed} ft");

            var details = root.Q<Label>(CharacterSheetUIMapper.GetCharacterDetailsElementName());
            if (details != null)
            {
                string race = string.IsNullOrEmpty(data.race) ? "Unknown" : data.race;
                string cls = string.IsNullOrEmpty(data.characterClass) ? "Adventurer" : data.characterClass;
                details.text = $"{race} {cls} {level}";
            }
        }

        private static void SetLabelText(VisualElement root, string elementName, string text)
        {
            Label label = root.Q<Label>(elementName);
            if (label != null)
                label.text = text;
        }

        private static string FormatSignedModifier(int modifier)
        {
            return modifier >= 0 ? $"+{modifier}" : modifier.ToString();
        }

        /// <summary>
        /// Updates all ability score buttons using the ruleset adapter.
        /// </summary>
        private static void UpdateAbilityScores(VisualElement root, ICharacterDataAdapter adapter,
            IRulesetCalculator calculator, object rulesetData)
        {
            var mapping = CharacterSheetUIMapper.GetAbilityScoreMapping();
            var abilityScores = adapter.GetAbilityScores(rulesetData);
            var abilityModifiers = adapter.GetAbilityModifiers(rulesetData, calculator);

            foreach (var kvp in mapping)
            {
                string buttonName = kvp.Key;
                string abilityName = kvp.Value;

                int score = abilityScores.TryGetValue(abilityName, out var s) ? s : 10;
                int modifier = abilityModifiers.TryGetValue(abilityName, out var m) ? m : 0;

                UpdateAbilityScore(root, buttonName, abilityName, score, modifier);
            }
        }

        /// <summary>
        /// Updates a single ability score button.
        /// </summary>
        private static void UpdateAbilityScore(VisualElement root, string buttonName, string abilityName, int score, int modifier)
        {
            var buttons = root.Query<Button>(name: buttonName).ToList();
            foreach (var button in buttons)
            {
                string modifierText = modifier >= 0 ? $"+{modifier}" : modifier.ToString();
                button.text = $"{abilityName} {score} ({modifierText})";
            }
        }

        /// <summary>
        /// Updates all skill buttons using the ruleset adapter.
        /// </summary>
        private static void UpdateSkills(VisualElement root, ICharacterDataAdapter adapter,
            IRulesetCalculator calculator, object rulesetData)
        {
            var skillMapping = CharacterSheetUIMapper.GetSkillMapping();
            var proficientSkills = new HashSet<string>(adapter.GetProficientSkills(rulesetData));
            var skillModifiers = adapter.GetSkillModifiers(rulesetData, calculator);

            foreach (var kvp in skillMapping)
            {
                string buttonName = kvp.Key;
                string skillName = kvp.Value;

                var buttons = root.Query<Button>(name: buttonName).ToList();

                foreach (var button in buttons)
                {
                    bool isProficient = proficientSkills.Contains(skillName);
                    int modifier = skillModifiers.TryGetValue(skillName, out var m) ? m : 0;

                    UpdateSkill(button, skillName, modifier, isProficient);
                }
            }
        }

        /// <summary>
        /// Updates a single skill button.
        /// </summary>
        private static bool UpdateSkill(Button button, string skillName, int modifier, bool isProficient)
        {
            if (button == null)
                return false;

            // Update modifier label
            var modifierLabel = button.Q<Label>(className: CharacterSheetUIMapper.GetSkillModifierClassName());
            if (modifierLabel == null)
            {
                modifierLabel = button.Q<Label>(CharacterSheetUIMapper.GetSkillModifierClassName());
            }

            if (modifierLabel != null)
            {
                string modifierText = modifier >= 0 ? $"+{modifier}" : modifier.ToString();
                modifierLabel.text = modifierText;
            }

            // Update proficiency indicator
            var icon = button.Q<VisualElement>(className: CharacterSheetUIMapper.GetSkillIconClassName());
            var spacer = button.Q<VisualElement>(className: CharacterSheetUIMapper.GetSkillIconSpacerClassName());

            if (isProficient)
            {
                button.AddToClassList(CharacterSheetUIMapper.GetProficientClassName());
                if (icon != null) icon.style.display = DisplayStyle.Flex;
                if (spacer != null) spacer.style.display = DisplayStyle.None;
            }
            else
            {
                button.RemoveFromClassList(CharacterSheetUIMapper.GetProficientClassName());
                if (icon != null) icon.style.display = DisplayStyle.None;
                if (spacer != null) spacer.style.display = DisplayStyle.Flex;
            }

            return true;
        }

        /// <summary>
        /// Updates all attack buttons using the ruleset adapter.
        /// </summary>
        private static void UpdateAttacks(VisualElement root, ICharacterDataAdapter adapter,
            IRulesetCalculator calculator, object rulesetData)
        {
            var attackMapping = CharacterSheetUIMapper.GetAttackMapping();

            foreach (var kvp in attackMapping)
            {
                string buttonName = kvp.Key;
                string weaponName = kvp.Value;

                var buttons = root.Query<Button>(name: buttonName).ToList();

                foreach (var button in buttons)
                {
                    WeaponData weaponData = adapter.GetWeaponData(weaponName, rulesetData, calculator);
                    UpdateAttackButton(button, weaponData, calculator);
                }
            }
        }

        /// <summary>
        /// Updates a single attack button.
        /// </summary>
        private static void UpdateAttackButton(Button button, WeaponData weaponData, IRulesetCalculator calculator)
        {
            if (button == null || weaponData == null)
                return;

            string attackBonusText = weaponData.AttackBonus >= 0
                ? $"+{weaponData.AttackBonus}"
                : weaponData.AttackBonus.ToString();

            string damageText = FormatDamage(weaponData, calculator);
            string newText = $"{weaponData.WeaponName} {attackBonusText} ({damageText})";

            button.text = newText;
        }

        /// <summary>
        /// Formats damage string.
        /// </summary>
        private static string FormatDamage(WeaponData weaponData, IRulesetCalculator calculator)
        {
            var properties = calculator.GetWeaponProperties(weaponData.WeaponName);

            if (weaponData.DamageDice == 0
                && properties.HasValue
                && properties.Value.FlatBaseDamage > 0)
            {
                string baseText = properties.Value.FlatBaseDamage.ToString();
                string modifierText = weaponData.DamageModifier != 0
                    ? weaponData.DamageModifier >= 0
                        ? $"+{weaponData.DamageModifier}"
                        : weaponData.DamageModifier.ToString()
                    : string.Empty;

                string damageType = properties.Value.DamageType?.ToLower() ?? "damage";
                return $"{baseText}{modifierText} {damageType}";
            }

            if (weaponData.DamageDice == 0)
            {
                return "0 damage";
            }

            string diceText = $"{weaponData.DamageDice}d{weaponData.DamageDieType}";
            string diceModifierText = weaponData.DamageModifier != 0
                ? weaponData.DamageModifier >= 0
                    ? $"+{weaponData.DamageModifier}"
                    : weaponData.DamageModifier.ToString()
                : string.Empty;

            string diceDamageType = properties.HasValue
                ? properties.Value.DamageType?.ToLower() ?? "damage"
                : "damage";

            return $"{diceText}{diceModifierText} {diceDamageType}";
        }
    }
}

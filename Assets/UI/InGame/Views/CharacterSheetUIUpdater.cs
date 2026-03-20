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
    /// - Dependency Inversion: Depends on abstractions (IRulesetCalculator, ICharacterDataAdapter)
    /// </summary>
    public static class CharacterSheetUIUpdater
    {
        /// <summary>
        /// Updates all character sheet UI elements from character data.
        /// Uses ruleset calculator and adapter for ruleset-agnostic updates.
        /// </summary>
        public static void UpdateCharacterSheet(VisualElement root, CharacterData data, string rulesetId = "DnD5e")
        {
            if (root == null)
            {
                Debug.LogWarning("CharacterSheetUIUpdater: Root element is null!");
                return;
            }
            
            if (data == null)
            {
                Debug.LogWarning("CharacterSheetUIUpdater: Character data is null!");
                return;
            }

            Debug.Log($"CharacterSheetUIUpdater: Updating character sheet for {data.CharacterName} (ruleset: {rulesetId})");

            // Get ruleset calculator and adapter
            var calculator = RulesetCalculatorFactory.GetCalculator(rulesetId) ?? RulesetCalculatorFactory.GetDefaultCalculator();
            var adapter = RulesetAdapterFactory.GetAdapter(rulesetId) ?? RulesetAdapterFactory.GetDefaultAdapter();

            // Get ruleset-specific data if available
            object rulesetData = GetRulesetData(data, rulesetId);

            UpdateCharacterName(root, data);
            if (rulesetId == "DnD5e" && rulesetData is DnD5eCharacterData dnd)
            {
                UpdateCombatHeader(root, dnd, calculator);
            }

            UpdateAbilityScores(root, data, calculator, adapter, rulesetData);
            UpdateSkills(root, data, calculator, adapter, rulesetData);
            UpdateAttacks(root, data, calculator, adapter, rulesetData);
        }

        /// <summary>
        /// Gets ruleset-specific data from the service if available.
        /// </summary>
        private static object GetRulesetData(CharacterData data, string rulesetId)
        {
            // Try to get D&D 5e data from service
            if (rulesetId == "DnD5e")
            {
                var service = PlayerDataServiceLocator.Service;
                if (service is JsonPlayerDataService jsonService)
                {
                    return jsonService.GetDnD5eCharacterData();
                }
            }
            return null;
        }

        /// <summary>
        /// Updates character name in the header.
        /// </summary>
        private static void UpdateCharacterName(VisualElement root, CharacterData data)
        {
            var nameLabel = root.Q<Label>(CharacterSheetUIMapper.GetCharacterNameElementName());
            if (nameLabel != null)
            {
                nameLabel.text = data.CharacterName;
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
        /// Updates all ability score buttons using ruleset calculator.
        /// </summary>
        private static void UpdateAbilityScores(VisualElement root, CharacterData data, IRulesetCalculator calculator, 
            ICharacterDataAdapter adapter, object rulesetData)
        {
            var mapping = CharacterSheetUIMapper.GetAbilityScoreMapping();
            var abilityScores = rulesetData != null ? adapter.GetAbilityScores(rulesetData) : GetAbilityScoresFromLegacy(data);
            var abilityModifiers = rulesetData != null ? adapter.GetAbilityModifiers(rulesetData, calculator) : GetAbilityModifiersFromLegacy(data, calculator);

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
        /// Updates all skill buttons using ruleset calculator.
        /// </summary>
        private static void UpdateSkills(VisualElement root, CharacterData data, IRulesetCalculator calculator,
            ICharacterDataAdapter adapter, object rulesetData)
        {
            var skillMapping = CharacterSheetUIMapper.GetSkillMapping();
            // Convert to HashSet for consistent type handling
            HashSet<string> proficientSkills;
            if (rulesetData != null)
            {
                var proficientList = adapter.GetProficientSkills(rulesetData);
                proficientSkills = new HashSet<string>(proficientList);
            }
            else
            {
                proficientSkills = data.ProficientSkills;
            }
            var skillModifiers = rulesetData != null ? adapter.GetSkillModifiers(rulesetData, calculator) : GetSkillModifiersFromLegacy(data, calculator);

            int foundCount = 0;
            int updatedCount = 0;

            foreach (var kvp in skillMapping)
            {
                string buttonName = kvp.Key;
                string skillName = kvp.Value;
                
                var buttons = root.Query<Button>(name: buttonName).ToList();
                foundCount += buttons.Count;

                foreach (var button in buttons)
                {
                    bool isProficient = proficientSkills.Contains(skillName);
                    int modifier = skillModifiers.TryGetValue(skillName, out var m) ? m : 0;

                    if (UpdateSkill(button, skillName, modifier, isProficient))
                    {
                        updatedCount++;
                    }
                }
            }

            Debug.Log($"CharacterSheetUIUpdater: Found {foundCount} skill button(s), updated {updatedCount}");
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
        /// Updates all attack buttons using ruleset calculator.
        /// </summary>
        private static void UpdateAttacks(VisualElement root, CharacterData data, IRulesetCalculator calculator,
            ICharacterDataAdapter adapter, object rulesetData)
        {
            var attackMapping = CharacterSheetUIMapper.GetAttackMapping();

            foreach (var kvp in attackMapping)
            {
                string buttonName = kvp.Key;
                string weaponName = kvp.Value;

                var buttons = root.Query<Button>(name: buttonName).ToList();
                
                foreach (var button in buttons)
                {
                    WeaponData weaponData;
                    if (rulesetData != null)
                    {
                        weaponData = adapter.GetWeaponData(weaponName, rulesetData, calculator);
                    }
                    else
                    {
                        // Fallback: Convert legacy CharacterData to DnD5eCharacterData for ruleset system
                        var dnD5eFallback = new DnD5eCharacterData
                        {
                            characterName = data.CharacterName,
                            strength = data.Strength,
                            dexterity = data.Dexterity,
                            constitution = data.Constitution,
                            intelligence = data.Intelligence,
                            wisdom = data.Wisdom,
                            charisma = data.Charisma,
                            level = 1, // Default level
                            proficientWeapons = new List<string> { "Simple", "Martial" } // Assume basic proficiency
                        };
                        weaponData = adapter.GetWeaponData(weaponName, dnD5eFallback, calculator);
                    }

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
            if (weaponData.DamageDice == 0)
            {
                return "0 damage";
            }

            string diceText = $"{weaponData.DamageDice}d{weaponData.DamageDieType}";
            string modifierText = "";
            
            if (weaponData.DamageModifier != 0)
            {
                modifierText = weaponData.DamageModifier >= 0 
                    ? $"+{weaponData.DamageModifier}" 
                    : weaponData.DamageModifier.ToString();
            }

            // Get damage type from weapon properties
            string damageType = "damage";
            var properties = calculator.GetWeaponProperties(weaponData.WeaponName);
            if (properties.HasValue)
            {
                damageType = properties.Value.DamageType?.ToLower() ?? "damage";
            }

            return $"{diceText}{modifierText} {damageType}";
        }

        #region Legacy Support Methods

        private static Dictionary<string, int> GetAbilityScoresFromLegacy(CharacterData data)
        {
            return new Dictionary<string, int>
            {
                { "STR", data.Strength },
                { "DEX", data.Dexterity },
                { "CON", data.Constitution },
                { "INT", data.Intelligence },
                { "WIS", data.Wisdom },
                { "CHA", data.Charisma }
            };
        }

        private static Dictionary<string, int> GetAbilityModifiersFromLegacy(CharacterData data, IRulesetCalculator calculator)
        {
            var scores = GetAbilityScoresFromLegacy(data);
            var modifiers = new Dictionary<string, int>();
            foreach (var kvp in scores)
            {
                modifiers[kvp.Key] = calculator.CalculateAbilityModifier(kvp.Value);
            }
            return modifiers;
        }

        private static Dictionary<string, int> GetSkillModifiersFromLegacy(CharacterData data, IRulesetCalculator calculator)
        {
            var modifiers = new Dictionary<string, int>();
            var abilityModifiers = GetAbilityModifiersFromLegacy(data, calculator);

            foreach (var skillName in CharacterSheetUIMapper.GetSkillMapping().Values)
            {
                string abilityName = CharacterData.GetSkillAbility(skillName);
                bool isProficient = data.ProficientSkills.Contains(skillName);
                int abilityModifier = abilityModifiers.TryGetValue(abilityName, out var m) ? m : 0;
                int skillModifier = calculator.CalculateSkillModifier(abilityModifier, isProficient, 1); // Default level 1
                modifiers[skillName] = skillModifier;
            }

            return modifiers;
        }

        #endregion
    }
}


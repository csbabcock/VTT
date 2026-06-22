using System;
using System.Collections.Generic;
using GameCore.PlayerData;
using GameCore.PlayerData.Rulesets;
using GameCore.UI.InGame.Models;
using GameCore.UI.InGame.Services;
using UnityEngine.UIElements;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Handles dice rolls and game-log entries triggered from the character sheet
    /// (ability/skill checks, attacks, actions, features, rests) plus log maintenance.
    /// Extracted from <see cref="InGameUIPresenter"/> so roll/log formatting lives in one
    /// focused place; the presenter keeps the view-event subscription lifecycle.
    /// </summary>
    public sealed class InGameActionLogController
    {
        private readonly DiceRollService _diceRollService;
        private readonly GameLogService _gameLogService;
        private readonly InGameUIView _view;
        private readonly Func<ICharacterSheet> _getActiveSheet;

        public InGameActionLogController(
            DiceRollService diceRollService,
            GameLogService gameLogService,
            InGameUIView view,
            Func<ICharacterSheet> getActiveSheet)
        {
            _diceRollService = diceRollService;
            _gameLogService = gameLogService;
            _view = view;
            _getActiveSheet = getActiveSheet;
        }

        public void RollAbilityCheck(string abilityName)
        {
            var sheet = _getActiveSheet();
            int modifier = sheet.GetAbilityModifier(abilityName);
            var rollResult = _diceRollService.RollD20Check(
                sheet.CharacterName,
                $"{abilityName} Check",
                modifier,
                new List<ModifierBreakdown>
                {
                    new ModifierBreakdown { Source = abilityName, Value = modifier }
                });

            _view.AddLogEntry(_gameLogService.FormatRollResult(rollResult));
        }

        public void RollSkillCheck(string skillName)
        {
            var sheet = _getActiveSheet();
            string abilityName = sheet.GetSkillAbility(skillName);
            int abilityModifier = sheet.GetAbilityModifier(abilityName);
            int modifier = sheet.GetSkillModifier(skillName);
            bool hasExpertise = sheet.HasExpertiseInSkill(skillName);
            bool isProficient = sheet.IsProficientInSkill(skillName);

            var breakdowns = new List<ModifierBreakdown>
            {
                new ModifierBreakdown { Source = abilityName, Value = abilityModifier }
            };

            if (hasExpertise)
            {
                breakdowns.Add(new ModifierBreakdown
                {
                    Source = "Expertise",
                    Value = sheet.ProficiencyBonus * 2
                });
            }
            else if (isProficient)
            {
                breakdowns.Add(new ModifierBreakdown
                {
                    Source = "Proficiency",
                    Value = sheet.ProficiencyBonus
                });
            }

            var rollResult = _diceRollService.RollD20Check(sheet.CharacterName, skillName, modifier, breakdowns);
            _view.AddLogEntry(_gameLogService.FormatRollResult(rollResult));
        }

        /// <summary>Logs a (non-dice) action; encounter side effects are handled by the presenter.</summary>
        public void LogAction(string actionName)
        {
            var sheet = _getActiveSheet();
            _view.AddLogEntry(_gameLogService.FormatAction(sheet.CharacterName, actionName));
        }

        public void RollAttack(string weaponName)
        {
            // Resolve a ruleset-agnostic sheet; both data services provide one, so no
            // service-type downcasts or hand-rolled fallbacks are needed here.
            ICharacterSheet sheet = _getActiveSheet();
            string characterName = sheet?.CharacterName ?? "Unknown";

            var calculator = RulesetCalculatorFactory.GetDefaultCalculator();
            var adapter = RulesetAdapterFactory.GetDefaultAdapter();
            WeaponData weaponData = adapter.GetWeaponData(weaponName, sheet, calculator);

            var (attackRoll, damageRoll) = _diceRollService.RollAttack(
                characterName,
                weaponData.WeaponName,
                weaponData.AttackBonus,
                weaponData.DamageDice,
                weaponData.DamageDieType,
                weaponData.DamageModifier);

            _view.AddLogEntry(_gameLogService.FormatAttackRoll(attackRoll, damageRoll));
        }

        public void LogFeature(string featureName)
        {
            var sheet = _getActiveSheet();
            _view.AddLogEntry(_gameLogService.FormatAction(sheet.CharacterName, $"Used: {featureName}"));
        }

        public void LogRest(string restType)
        {
            var sheet = _getActiveSheet();
            _view.AddLogEntry(_gameLogService.FormatAction(sheet.CharacterName, restType));
        }

        public void ClearLog() => _view.ClearLog();

        public void DeleteLogEntry(VisualElement entryCard) => _view.RemoveLogEntry(entryCard);
    }
}

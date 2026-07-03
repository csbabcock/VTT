using System;
using GameCore.Actors;
using GameCore.Combat;
using GameCore.Combat.ActionEconomy;
using GameCore.Combat.Adapters;
using GameCore.Combat.Definitions;
using GameCore.Combat.Feedback;
using GameCore.Combat.Models;
using GameCore.Combat.Services;
using GameCore.Combat.Targeting;
using GameCore.EncounterMode;
using GameCore.EncounterMode.Grid;
using GameCore.PlayerData.Rulesets;
using GameCore.UI.InGame.Services;
using UnityEngine;

namespace GameCore.UI.InGame
{
    public sealed class InGameCombatController
    {
        private readonly AttackTargetingSession _targetingSession = new();
        private readonly CombatActionExecutor _executor;
        private readonly ActionEconomyTracker _actionEconomy = new();
        private readonly AttackTargetHighlightService _targetHighlighter = new();
        private readonly IActorTargetResolver _targetResolver;
        private readonly GameLogService _gameLogService;
        private readonly InGameUIView _view;
        private readonly Func<IActor> _getLocalActor;
        private readonly Func<EncounterContext> _getEncounterContext;
        private readonly Func<Camera> _getCamera;
        private readonly Func<IGridGenerator> _getGridGenerator;
        private readonly Func<float> _getFeetPerWorldUnit;
        private readonly Func<IEncounterModeManager> _getEncounterModeManager;

        public InGameCombatController(
            GameLogService gameLogService,
            InGameUIView view,
            Func<IActor> getLocalActor,
            Func<EncounterContext> getEncounterContext,
            Func<Camera> getCamera,
            Func<IGridGenerator> getGridGenerator,
            Func<float> getFeetPerWorldUnit,
            Func<IEncounterModeManager> getEncounterModeManager = null,
            IActorTargetResolver targetResolver = null,
            IRandomSource randomSource = null)
        {
            _gameLogService = gameLogService;
            _view = view;
            _getLocalActor = getLocalActor;
            _getEncounterContext = getEncounterContext;
            _getCamera = getCamera;
            _getGridGenerator = getGridGenerator;
            _getFeetPerWorldUnit = getFeetPerWorldUnit;
            _getEncounterModeManager = getEncounterModeManager;
            _targetResolver = targetResolver ?? new RendererScreenActorTargetResolver();

            var calculator = RulesetCalculatorFactory.GetDefaultCalculator();
            _executor = new CombatActionExecutor(
                new AttackStatBuilder(calculator),
                new AttackResolutionService(),
                randomSource ?? new UnityRandomSource());
        }

        public bool IsTargeting => _targetingSession.IsActive;

        public bool CanBeginEncounterAttack()
        {
            EncounterContext context = _getEncounterContext();
            if (!context.IsEncounterActive)
                return true;

            return context.IsLocalTurnActive && _actionEconomy.CanSpend(ActionCostKind.Action);
        }

        public void BeginUnarmedStrikeTargeting()
        {
            _targetingSession.Begin(UnarmedStrikeAttackDefinition.Instance);
        }

        public void ClearTargetingHover() => _targetHighlighter.Clear();

        public void UpdateTargetingHover(Vector2 screenPosition)
        {
            if (!_targetingSession.IsActive)
                return;

            _targetHighlighter.UpdateHover(
                _getLocalActor(),
                _getCamera(),
                screenPosition,
                _targetResolver);
        }

        public void CancelTargeting()
        {
            _targetingSession.Cancel();
            _targetHighlighter.Clear();
        }

        public void ResetActionEconomyForNewTurn() => _actionEconomy.ResetForNewTurn();

        public bool TryResolveTargetClick(Vector2 screenPosition, out IActor targetActor)
        {
            targetActor = null;
            if (!_targetingSession.IsActive || _targetingSession.ActiveAttack == null)
                return false;

            IActor localActor = _getLocalActor();
            if (localActor == null)
            {
                CancelTargeting();
                return false;
            }

            Camera camera = _getCamera();
            if (camera == null)
                return false;

            if (!_targetResolver.TryResolveTarget(
                    camera,
                    screenPosition,
                    ActorRegistry.Actors,
                    localActor,
                    out targetActor))
            {
                return false;
            }

            return true;
        }

        public IAttackDefinition ActiveAttack =>
            _targetingSession.IsActive ? _targetingSession.ActiveAttack : null;

        public CombatActionResult ExecuteAgainstActor(
            IAttackDefinition attack,
            IActor attackerActor,
            IActor targetActor)
        {
            string attackerName = CharacterSheetAuthorityHelper.GetDisplayName(attackerActor);
            string targetName = CharacterSheetAuthorityHelper.GetDisplayName(targetActor);
            string attackName = attack.DisplayName;

            if (ReferenceEquals(attackerActor, targetActor))
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.SelfTarget,
                    attackerName,
                    targetName,
                    attackName);
            }

            if (!IsWithinMeleeReach(attackerActor, targetActor))
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.OutOfRange,
                    attackerName,
                    targetName,
                    attackName);
            }

            IAttackParticipant attacker = ActorCombatBridge.CreateAttackParticipant(attackerActor);
            if (attacker == null)
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.InvalidTarget,
                    attackerName,
                    targetName,
                    attackName);
            }

            IDamageable target = ActorCombatBridge.TryCreateDamageable(targetActor, attackerActor);
            if (target == null)
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.InvalidTarget,
                    attackerName,
                    targetName,
                    attackName);
            }

            EncounterContext context = _getEncounterContext();

            return _executor.TryExecute(
                attack,
                attacker,
                target,
                context,
                context.IsEncounterActive ? _actionEconomy : null);
        }

        public void CompleteAttackAgainstTarget(IActor targetActor)
        {
            IAttackDefinition attack = ActiveAttack;
            IActor localActor = _getLocalActor();
            if (attack == null || localActor == null || targetActor == null)
            {
                CancelTargeting();
                return;
            }

            CombatActionResult result = ExecuteAgainstActor(attack, localActor, targetActor);
            _view.AddLogEntry(_gameLogService.FormatCombatAttackRoll(result));

            if (result.Succeeded && result.AttackOutcome.DidHit && result.AttackOutcome.DamageAmount > 0)
            {
                string damageType = GetDamageType(attack.WeaponName);
                _view.AddLogEntry(_gameLogService.FormatCombatFlatDamage(
                    result.TargetName,
                    result.AttackOutcome.DamageAmount,
                    damageType));

                if (targetActor.Transform != null && !HasCombatDamageReceiver(targetActor))
                    DamageFlashIndicator.Flash(targetActor.Transform);
            }

            CancelTargeting();
        }

        public bool IsWithinMeleeReach(IActor attacker, IActor target)
        {
            if (attacker?.Transform == null || target?.Transform == null)
                return false;

            return MeleeRangeQuery.IsWithinMeleeReach(
                attacker,
                target,
                _getGridGenerator(),
                _getFeetPerWorldUnit());
        }

        private static string GetDamageType(string weaponName)
        {
            var props = RulesetCalculatorFactory.GetDefaultCalculator().GetWeaponProperties(weaponName);
            return props.HasValue ? props.Value.DamageType : string.Empty;
        }

        private static bool HasCombatDamageReceiver(IActor actor)
        {
            if (actor?.Transform == null)
                return false;

            var components = actor.Transform.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is ICombatDamageReceiver)
                    return true;
            }

            return false;
        }
    }
}

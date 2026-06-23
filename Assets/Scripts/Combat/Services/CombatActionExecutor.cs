using System;
using GameCore.Combat.ActionEconomy;
using GameCore.Combat.Models;

namespace GameCore.Combat.Services
{
    /// <summary>
    /// Orchestrates attack validation, rolling, resolution, and damage application.
    /// </summary>
    public sealed class CombatActionExecutor
    {
        private readonly AttackStatBuilder _statBuilder;
        private readonly AttackResolutionService _resolution;
        private readonly IRandomSource _random;

        public CombatActionExecutor(
            AttackStatBuilder statBuilder,
            AttackResolutionService resolution,
            IRandomSource random)
        {
            _statBuilder = statBuilder ?? throw new ArgumentNullException(nameof(statBuilder));
            _resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public CombatActionResult TryExecute(
            IAttackDefinition attack,
            IAttackParticipant attacker,
            IDamageable target,
            EncounterContext context,
            IActionEconomyTracker actionEconomy = null)
        {
            if (attack == null)
                throw new ArgumentNullException(nameof(attack));
            if (attacker == null)
                throw new ArgumentNullException(nameof(attacker));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            string attackerName = attacker.DisplayName;
            string targetName = target.DisplayName;
            string attackName = attack.DisplayName;

            if (target.IsDestroyed)
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.TargetDestroyed,
                    attackerName,
                    targetName,
                    attackName);
            }

            if (context.IsEncounterActive && !context.IsLocalTurnActive)
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.NotYourTurn,
                    attackerName,
                    targetName,
                    attackName);
            }

            if (context.IsEncounterActive
                && attack.Cost != ActionCostKind.None
                && actionEconomy != null
                && !actionEconomy.CanSpend(attack.Cost))
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.ActionAlreadyUsed,
                    attackerName,
                    targetName,
                    attackName);
            }

            if (!_statBuilder.TryBuild(attack.WeaponName, attacker.Sheet, out AttackStatBuilder.AttackStats stats))
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.UnknownAttack,
                    attackerName,
                    targetName,
                    attackName);
            }

            if (!stats.UsesFlatDamage)
            {
                return CombatActionResult.Failed(
                    CombatFailureReason.UnknownAttack,
                    attackerName,
                    targetName,
                    attackName);
            }

            int natural = _random.RollDie(20);
            int attackTotal = natural + stats.AttackBonus;
            AttackOutcome outcome = _resolution.Resolve(
                natural,
                attackTotal,
                target.ArmorClass,
                stats.FlatBaseDamage,
                stats.DamageAbilityModifier);

            if (outcome.DidHit && outcome.DamageAmount > 0)
                target.ApplyDamage(outcome.DamageAmount);

            if (context.IsEncounterActive
                && attack.Cost != ActionCostKind.None
                && actionEconomy != null)
            {
                actionEconomy.TrySpend(attack.Cost);
            }

            return CombatActionResult.Completed(
                outcome,
                attackerName,
                targetName,
                attackName);
        }
    }
}

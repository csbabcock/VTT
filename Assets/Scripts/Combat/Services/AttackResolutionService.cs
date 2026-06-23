using System;
using GameCore.Combat.Models;

namespace GameCore.Combat.Services
{
    /// <summary>
    /// Pure hit/miss and flat-damage resolution. Does not roll dice or mutate targets.
    /// </summary>
    public sealed class AttackResolutionService
    {
        public AttackOutcome Resolve(
            int attackRollNatural,
            int attackRollTotal,
            int targetArmorClass,
            int flatBaseDamage,
            int damageAbilityModifier)
        {
            bool isCriticalMiss = attackRollNatural == 1;
            if (isCriticalMiss)
            {
                return AttackOutcome.Miss(
                    attackRollNatural,
                    attackRollTotal,
                    targetArmorClass);
            }

            bool isCritical = attackRollNatural == 20;
            bool hits = isCritical || attackRollTotal >= targetArmorClass;
            if (!hits)
            {
                return AttackOutcome.Miss(
                    attackRollNatural,
                    attackRollTotal,
                    targetArmorClass);
            }

            int damage = CalculateFlatDamage(flatBaseDamage, damageAbilityModifier, isCritical);
            return AttackOutcome.Hit(
                damage,
                isCritical,
                attackRollNatural,
                attackRollTotal,
                targetArmorClass);
        }

        /// <summary>
        /// PHB flat damage: base (doubled on crit when base &gt; 0) + ability modifier, minimum 0.
        /// </summary>
        public static int CalculateFlatDamage(int flatBaseDamage, int damageAbilityModifier, bool isCritical)
        {
            int basePart = isCritical && flatBaseDamage > 0
                ? flatBaseDamage * 2
                : flatBaseDamage;

            return Math.Max(0, basePart + damageAbilityModifier);
        }
    }
}

using System.Collections.Generic;
using GameCore.UI.InGame.Models;
using UnityEngine;

namespace GameCore.UI.InGame.Services
{
    /// <summary>
    /// Service responsible for rolling dice and calculating roll results.
    /// Follows Single Responsibility Principle by only handling dice mechanics.
    /// </summary>
    public class DiceRollService
    {
        /// <summary>
        /// Rolls a single die of the specified type.
        /// </summary>
        /// <param name="dieType">The type of die (e.g., 20 for d20, 6 for d6).</param>
        /// <returns>A random number between 1 and dieType (inclusive).</returns>
        public int RollDie(int dieType)
        {
            return Random.Range(1, dieType + 1);
        }

        /// <summary>
        /// Rolls multiple dice of the same type.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll.</param>
        /// <param name="dieType">The type of die (e.g., 20 for d20, 6 for d6).</param>
        /// <returns>A list of individual die results.</returns>
        public List<int> RollDice(int numberOfDice, int dieType)
        {
            var results = new List<int>();
            for (int i = 0; i < numberOfDice; i++)
            {
                results.Add(RollDie(dieType));
            }
            return results;
        }

        /// <summary>
        /// Performs a d20 roll with modifiers (for ability checks, skill checks, attack rolls).
        /// </summary>
        /// <param name="characterName">The name of the character making the roll.</param>
        /// <param name="rollType">The type of roll (e.g., "Strength Check", "Athletics").</param>
        /// <param name="modifier">The base modifier to add.</param>
        /// <param name="modifierBreakdowns">Optional breakdown of modifier sources.</param>
        /// <returns>A RollResult containing all roll information.</returns>
        public RollResult RollD20Check(
            string characterName,
            string rollType,
            int modifier,
            List<ModifierBreakdown> modifierBreakdowns = null)
        {
            var dieResult = RollDie(20);
            var dieResults = new List<int> { dieResult };
            
            bool isCritical = dieResult == 20;
            bool isCriticalMiss = dieResult == 1;

            int total = dieResult + modifier;

            return new RollResult
            {
                CharacterName = characterName,
                RollType = rollType,
                DieResults = dieResults,
                Modifier = modifier,
                ModifierBreakdowns = modifierBreakdowns ?? new List<ModifierBreakdown>(),
                Total = total,
                DieType = 20,
                NumberOfDice = 1,
                IsCritical = isCritical,
                IsCriticalMiss = isCriticalMiss
            };
        }

        /// <summary>
        /// Performs a damage roll (e.g., 1d8+3).
        /// </summary>
        /// <param name="characterName">The name of the character making the roll.</param>
        /// <param name="rollType">The type of damage (e.g., "Longsword Damage").</param>
        /// <param name="numberOfDice">The number of dice to roll.</param>
        /// <param name="dieType">The type of die (e.g., 8 for d8).</param>
        /// <param name="modifier">The modifier to add to the damage.</param>
        /// <returns>A RollResult containing all roll information.</returns>
        public RollResult RollDamage(
            string characterName,
            string rollType,
            int numberOfDice,
            int dieType,
            int modifier)
        {
            var dieResults = RollDice(numberOfDice, dieType);
            int total = 0;
            foreach (var result in dieResults)
            {
                total += result;
            }
            total += modifier;

            return new RollResult
            {
                CharacterName = characterName,
                RollType = rollType,
                DieResults = dieResults,
                Modifier = modifier,
                ModifierBreakdowns = new List<ModifierBreakdown>(),
                Total = total,
                DieType = dieType,
                NumberOfDice = numberOfDice,
                IsCritical = false,
                IsCriticalMiss = false
            };
        }

        /// <summary>
        /// Performs an attack roll (d20 + attack bonus) followed by damage if it hits.
        /// </summary>
        /// <param name="characterName">The name of the character making the attack.</param>
        /// <param name="weaponName">The name of the weapon.</param>
        /// <param name="attackBonus">The attack bonus modifier.</param>
        /// <param name="damageDice">The number of damage dice.</param>
        /// <param name="damageDieType">The type of damage die.</param>
        /// <param name="damageModifier">The damage modifier.</param>
        /// <returns>A tuple containing the attack roll result and damage roll result (if hit).</returns>
        public (RollResult attackRoll, RollResult? damageRoll) RollAttack(
            string characterName,
            string weaponName,
            int attackBonus,
            int damageDice,
            int damageDieType,
            int damageModifier)
        {
            var attackRoll = RollD20Check(
                characterName,
                $"{weaponName} Attack",
                attackBonus
            );

            RollResult? damageRoll = null;
            // Only roll damage if attack hits (not a critical miss)
            if (!attackRoll.IsCriticalMiss)
            {
                // If critical hit, roll damage dice twice
                int damageDiceCount = attackRoll.IsCritical ? damageDice * 2 : damageDice;
                damageRoll = RollDamage(
                    characterName,
                    $"{weaponName} Damage",
                    damageDiceCount,
                    damageDieType,
                    damageModifier
                );
            }

            return (attackRoll, damageRoll);
        }
    }
}


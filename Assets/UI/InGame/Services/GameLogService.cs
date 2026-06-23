using GameCore.UI.InGame.Models;
using GameCore.Combat.Models;
using System.Collections.Generic;
using System.Text;

namespace GameCore.UI.InGame.Services
{
    /// <summary>
    /// Result of formatting a log entry, containing structured data for display.
    /// </summary>
    public struct FormattedLogEntry
    {
        public string CharacterName;
        public string ActionType; // e.g., "ARCANA", "INVESTIGATION", "ATTACK"
        public string SubActionType; // e.g., "CHECK", "ROLL", "HEAL"
        public string DiceFormula; // e.g., "1d20+10"
        public string DiceBreakdown; // e.g., "14 + 10"
        public int? Result; // The final result number
        public string CssClass;
        public string FullMessage; // Fallback for simple messages
    }

    /// <summary>
    /// Service responsible for formatting roll results into log messages.
    /// Follows Single Responsibility Principle by only handling log formatting.
    /// </summary>
    public class GameLogService
    {
        /// <summary>
        /// Formats a roll result into a structured log entry.
        /// </summary>
        /// <param name="rollResult">The roll result to format.</param>
        /// <returns>A formatted log entry with structured data.</returns>
        public FormattedLogEntry FormatRollResult(RollResult rollResult)
        {
            // Determine action type and sub-action type
            string actionType = rollResult.RollType.ToUpper();
            string subActionType = "ROLL";
            
            if (rollResult.RollType.Contains("Check"))
            {
                subActionType = "CHECK";
                // Extract skill/ability name
                actionType = rollResult.RollType.Replace(" Check", "").ToUpper();
            }
            else if (rollResult.RollType.Contains("Attack"))
            {
                subActionType = "ATTACK";
                actionType = rollResult.RollType.Replace(" Attack", "").ToUpper();
            }

            // Build dice formula
            var formulaParts = new List<string>();
            if (rollResult.NumberOfDice == 1)
            {
                formulaParts.Add($"1d{rollResult.DieType}");
            }
            else
            {
                formulaParts.Add($"{rollResult.NumberOfDice}d{rollResult.DieType}");
            }

            // Build dice breakdown (die result + modifiers) with proper spacing
            var breakdownParts = new List<string>();
            if (rollResult.NumberOfDice == 1)
            {
                breakdownParts.Add(rollResult.DieResults[0].ToString());
            }
            else
            {
                breakdownParts.Add(string.Join(" + ", rollResult.DieResults));
            }

            // Add modifiers to breakdown with proper spacing
            if (rollResult.ModifierBreakdowns != null && rollResult.ModifierBreakdowns.Count > 0)
            {
                foreach (var breakdown in rollResult.ModifierBreakdowns)
                {
                    string sign = breakdown.Value >= 0 ? " + " : " ";
                    breakdownParts.Add($"{sign}{breakdown.Value}");
                    formulaParts.Add(breakdown.Value >= 0 ? $"+{breakdown.Value}" : breakdown.Value.ToString());
                }
            }
            else if (rollResult.Modifier != 0)
            {
                string sign = rollResult.Modifier >= 0 ? " + " : " ";
                breakdownParts.Add($"{sign}{rollResult.Modifier}");
                formulaParts.Add(rollResult.Modifier >= 0 ? $"+{rollResult.Modifier}" : rollResult.Modifier.ToString());
            }

            string diceFormula = string.Join("", formulaParts);
            string diceBreakdown = string.Join("", breakdownParts);

            // Determine CSS class
            string cssClass = "log-roll";
            if (rollResult.IsCritical)
            {
                cssClass = "log-roll-critical";
            }
            else if (rollResult.IsCriticalMiss)
            {
                cssClass = "log-roll-critical-miss";
            }
            else if (rollResult.RollType.Contains("Check") || rollResult.RollType.Contains("STR") || 
                     rollResult.RollType.Contains("DEX") || rollResult.RollType.Contains("CON") ||
                     rollResult.RollType.Contains("INT") || rollResult.RollType.Contains("WIS") ||
                     rollResult.RollType.Contains("CHA"))
            {
                cssClass = "log-ability-check";
            }
            else if (rollResult.RollType.Contains("Attack"))
            {
                cssClass = "log-attack";
            }
            else
            {
                cssClass = "log-skill";
            }

            return new FormattedLogEntry
            {
                CharacterName = rollResult.CharacterName,
                ActionType = actionType,
                SubActionType = subActionType,
                DiceFormula = diceFormula,
                DiceBreakdown = diceBreakdown,
                Result = rollResult.Total,
                CssClass = cssClass,
                FullMessage = $"[{rollResult.CharacterName}] {rollResult.RollType}: {diceBreakdown} = {rollResult.Total}"
            };
        }

        /// <summary>
        /// Formats an attack roll with damage into a structured log entry.
        /// </summary>
        /// <param name="attackRoll">The attack roll result.</param>
        /// <param name="damageRoll">The damage roll result (null if miss).</param>
        /// <returns>A formatted log entry with structured data.</returns>
        public FormattedLogEntry FormatAttackRoll(RollResult attackRoll, RollResult? damageRoll)
        {
            var attackFormatted = FormatRollResult(attackRoll);
            
            // Update for attack-specific styling
            string cssClass = "log-attack";
            if (attackRoll.IsCritical)
            {
                cssClass = "log-attack-critical";
            }
            else if (attackRoll.IsCriticalMiss)
            {
                cssClass = "log-attack-miss";
            }

            // Build full message with damage if applicable
            var sb = new StringBuilder(attackFormatted.FullMessage);
            if (damageRoll.HasValue)
            {
                var damage = damageRoll.Value;
                sb.Append($" → Damage: {damage.Total}");
            }
            else if (attackRoll.IsCriticalMiss)
            {
                sb.Append(" → Miss");
            }

            return new FormattedLogEntry
            {
                CharacterName = attackFormatted.CharacterName,
                ActionType = attackFormatted.ActionType,
                SubActionType = "ATTACK",
                DiceFormula = attackFormatted.DiceFormula,
                DiceBreakdown = attackFormatted.DiceBreakdown,
                Result = attackFormatted.Result,
                CssClass = cssClass,
                FullMessage = sb.ToString()
            };
        }

        /// <summary>
        /// Formats a simple action log entry (for non-dice actions like Dash, Disengage, etc.).
        /// </summary>
        /// <param name="characterName">The name of the character performing the action.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <returns>A formatted log entry with structured data.</returns>
        public FormattedLogEntry FormatAction(string characterName, string actionName)
        {
            string cssClass = "log-action";
            string subActionType = "ACTION";
            
            if (actionName.Contains("Rest"))
            {
                cssClass = "log-rest";
                subActionType = "REST";
            }
            else if (actionName.Contains("Attack") || actionName.Contains("Dash") || 
                     actionName.Contains("Disengage") || actionName.Contains("Dodge"))
            {
                cssClass = "log-combat-action";
                subActionType = "ACTION";
            }

            return new FormattedLogEntry 
            { 
                CharacterName = characterName,
                ActionType = actionName.ToUpper(),
                SubActionType = subActionType,
                DiceFormula = "",
                DiceBreakdown = "",
                Result = null,
                CssClass = cssClass,
                FullMessage = $"[{characterName}] {actionName}"
            };
        }

        /// <summary>Formats a resolved combat action (attack vs target with damage).</summary>
        public FormattedLogEntry FormatCombatActionResult(CombatActionResult result)
            => FormatCombatAttackRoll(result);

        public FormattedLogEntry FormatCombatAttackRoll(CombatActionResult result)
        {
            if (!result.Succeeded)
                return FormatCombatFailure(result);

            AttackOutcome outcome = result.AttackOutcome;
            var sb = new StringBuilder();
            sb.Append($"[{result.AttackerName}] {result.AttackDisplayName} vs [{result.TargetName}] — ");
            sb.Append($"To Hit: d20 ({outcome.AttackRollNatural}) → {outcome.AttackRollTotal} vs AC {outcome.TargetArmorClass}");

            string cssClass = "log-attack";
            if (outcome.IsCritical)
            {
                cssClass = "log-attack-critical";
                sb.Append(" → CRITICAL HIT");
            }
            else if (outcome.AttackRollNatural == 1)
            {
                cssClass = "log-attack-miss";
                sb.Append(" → MISS");
            }
            else if (outcome.DidHit)
            {
                sb.Append(" → HIT");
            }
            else
            {
                cssClass = "log-attack-miss";
                sb.Append(" → MISS");
            }

            return new FormattedLogEntry
            {
                CharacterName = result.AttackerName,
                ActionType = result.AttackDisplayName.ToUpper(),
                SubActionType = "TO HIT",
                DiceFormula = "1d20",
                DiceBreakdown = outcome.AttackRollNatural.ToString(),
                Result = outcome.AttackRollTotal,
                CssClass = cssClass,
                FullMessage = sb.ToString()
            };
        }

        public FormattedLogEntry FormatCombatFlatDamage(string targetName, int damageAmount, string damageType)
        {
            string type = string.IsNullOrEmpty(damageType) ? "damage" : damageType.ToLower();
            return new FormattedLogEntry
            {
                CharacterName = targetName,
                ActionType = "DAMAGE",
                SubActionType = "DAMAGE",
                DiceFormula = string.Empty,
                DiceBreakdown = string.Empty,
                Result = damageAmount,
                CssClass = "log-damage",
                FullMessage = $"[{targetName}] takes {damageAmount} {type} damage (flat; no damage roll)"
            };
        }

        private static FormattedLogEntry FormatCombatFailure(CombatActionResult result)
        {
            string reason = result.FailureReason switch
            {
                CombatFailureReason.NotYourTurn => "Not your turn",
                CombatFailureReason.ActionAlreadyUsed => "Action already used",
                CombatFailureReason.OutOfRange => "Target out of melee range",
                CombatFailureReason.SelfTarget => "Cannot target yourself",
                CombatFailureReason.InvalidTarget => "Invalid target",
                CombatFailureReason.TargetDestroyed => "Target is destroyed",
                CombatFailureReason.NoPermissionToApplyDamage => "No permission to apply damage",
                _ => "Attack failed"
            };

            string message = $"[{result.AttackerName}] {result.AttackDisplayName}: {reason}";
            if (!string.IsNullOrEmpty(result.TargetName))
                message = $"[{result.AttackerName}] {result.AttackDisplayName} vs [{result.TargetName}]: {reason}";

            return new FormattedLogEntry
            {
                CharacterName = result.AttackerName,
                ActionType = result.AttackDisplayName.ToUpper(),
                SubActionType = "ACTION",
                CssClass = "log-combat-action",
                FullMessage = message
            };
        }
    }
}


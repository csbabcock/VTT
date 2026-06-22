using System.Collections.Generic;
using System.Text;

namespace GameCore.PlayerData
{
    /// <summary>Compact combat status text for DM player-list rows.</summary>
    public static class CharacterCombatStatusFormatter
    {
        public static string FormatSummary(CharacterCombatState state, int maxHp)
        {
            var parts = new List<string>(4);

            if (maxHp > 0 && state.CurrentHitPoints <= 0)
                parts.Add($"Dying {state.DeathSaveSuccesses}/{state.DeathSaveFailures}");

            if (state.HasInspiration)
                parts.Add("Inspired");

            if (state.ExhaustionLevel > 0)
                parts.Add($"Exhaustion {state.ExhaustionLevel}");

            var conditions = DnD5eConditions.ToList(state.ConditionFlags);
            if (conditions.Count > 0)
                parts.Add(string.Join(", ", conditions));

            if (parts.Count == 0)
                return string.Empty;

            var builder = new StringBuilder(parts[0]);
            for (int i = 1; i < parts.Count; i++)
            {
                builder.Append(" · ");
                builder.Append(parts[i]);
            }

            return builder.ToString();
        }
    }
}

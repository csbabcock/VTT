using System;
using System.Collections.Generic;

namespace GameCore.PlayerData
{
    /// <summary>Standard D&amp;D 5e conditions as a compact flag set for replication.</summary>
    [Flags]
    public enum DnD5eConditionFlags : uint
    {
        None = 0,
        Blinded = 1 << 0,
        Charmed = 1 << 1,
        Deafened = 1 << 2,
        Frightened = 1 << 3,
        Grappled = 1 << 4,
        Incapacitated = 1 << 5,
        Invisible = 1 << 6,
        Paralyzed = 1 << 7,
        Petrified = 1 << 8,
        Poisoned = 1 << 9,
        Prone = 1 << 10,
        Restrained = 1 << 11,
        Stunned = 1 << 12,
        Unconscious = 1 << 13,
    }

    /// <summary>Maps between sheet condition strings and <see cref="DnD5eConditionFlags"/>.</summary>
    public static class DnD5eConditions
    {
        private static readonly string[] ConditionIds =
        {
            "Blinded",
            "Charmed",
            "Deafened",
            "Frightened",
            "Grappled",
            "Incapacitated",
            "Invisible",
            "Paralyzed",
            "Petrified",
            "Poisoned",
            "Prone",
            "Restrained",
            "Stunned",
            "Unconscious",
        };

        public static IReadOnlyList<string> AllConditionIds => ConditionIds;

        public static uint ToFlags(IReadOnlyList<string> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return 0;

            uint flags = 0;
            for (int i = 0; i < conditions.Count; i++)
            {
                if (TryParse(conditions[i], out DnD5eConditionFlags flag))
                    flags |= (uint)flag;
            }

            return flags;
        }

        public static List<string> ToList(uint flags)
        {
            var list = new List<string>();
            for (int i = 0; i < ConditionIds.Length; i++)
            {
                var flag = (DnD5eConditionFlags)(1u << i);
                if (((DnD5eConditionFlags)flags & flag) != 0)
                    list.Add(ConditionIds[i]);
            }

            return list;
        }

        public static bool TryParse(string conditionId, out DnD5eConditionFlags flag)
        {
            flag = DnD5eConditionFlags.None;
            if (string.IsNullOrWhiteSpace(conditionId))
                return false;

            for (int i = 0; i < ConditionIds.Length; i++)
            {
                if (string.Equals(ConditionIds[i], conditionId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    flag = (DnD5eConditionFlags)(1u << i);
                    return true;
                }
            }

            return false;
        }

        public static uint Toggle(uint flags, string conditionId)
        {
            if (!TryParse(conditionId, out DnD5eConditionFlags flag))
                return flags;

            uint bit = (uint)flag;
            return (flags & bit) != 0 ? flags & ~bit : flags | bit;
        }

        public static bool Has(uint flags, string conditionId)
        {
            return TryParse(conditionId, out DnD5eConditionFlags flag) && ((DnD5eConditionFlags)flags & flag) != 0;
        }

        public static int Count(uint flags)
        {
            int count = 0;
            for (int i = 0; i < ConditionIds.Length; i++)
            {
                if (((DnD5eConditionFlags)flags & (DnD5eConditionFlags)(1u << i)) != 0)
                    count++;
            }

            return count;
        }
    }
}

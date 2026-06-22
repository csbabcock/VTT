using System;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Authoritative combat-tracking slice of a character sheet. Used for DM tools,
    /// replication, and player-list status chips.
    /// </summary>
    [Serializable]
    public struct CharacterCombatState : IEquatable<CharacterCombatState>
    {
        public int CurrentHitPoints;
        public int TemporaryHitPoints;
        public int DeathSaveSuccesses;
        public int DeathSaveFailures;
        public uint ConditionFlags;
        public byte ExhaustionLevel;
        public bool HasInspiration;

        public static CharacterCombatState FromSheet(DnD5eCharacterData data)
        {
            if (data == null)
                return default;

            int maxHp = CharacterHitPoints.GetDisplayMaxHp(data);
            return new CharacterCombatState
            {
                CurrentHitPoints = CharacterHitPoints.ClampCurrent(data.currentHitPoints, maxHp),
                TemporaryHitPoints = Math.Max(0, data.temporaryHitPoints),
                DeathSaveSuccesses = CharacterCombatStateRules.ClampDeathSaveCount(data.deathSaveSuccesses),
                DeathSaveFailures = CharacterCombatStateRules.ClampDeathSaveCount(data.deathSaveFailures),
                ConditionFlags = DnD5eConditions.ToFlags(data.conditions),
                ExhaustionLevel = CharacterCombatStateRules.ClampExhaustion(data.exhaustionLevel),
                HasInspiration = data.hasInspiration,
            };
        }

        public void ApplyToSheet(DnD5eCharacterData data)
        {
            if (data == null)
                return;

            int maxHp = CharacterHitPoints.GetDisplayMaxHp(data);
            data.currentHitPoints = CharacterHitPoints.ClampCurrent(CurrentHitPoints, maxHp);
            data.temporaryHitPoints = Math.Max(0, TemporaryHitPoints);
            data.deathSaveSuccesses = CharacterCombatStateRules.ClampDeathSaveCount(DeathSaveSuccesses);
            data.deathSaveFailures = CharacterCombatStateRules.ClampDeathSaveCount(DeathSaveFailures);
            data.conditions = DnD5eConditions.ToList(ConditionFlags);
            data.exhaustionLevel = CharacterCombatStateRules.ClampExhaustion(ExhaustionLevel);
            data.hasInspiration = HasInspiration;
        }

        public bool Equals(CharacterCombatState other)
        {
            return CurrentHitPoints == other.CurrentHitPoints
                   && TemporaryHitPoints == other.TemporaryHitPoints
                   && DeathSaveSuccesses == other.DeathSaveSuccesses
                   && DeathSaveFailures == other.DeathSaveFailures
                   && ConditionFlags == other.ConditionFlags
                   && ExhaustionLevel == other.ExhaustionLevel
                   && HasInspiration == other.HasInspiration;
        }

        public override bool Equals(object obj) => obj is CharacterCombatState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = CurrentHitPoints;
                hash = (hash * 397) ^ TemporaryHitPoints;
                hash = (hash * 397) ^ DeathSaveSuccesses;
                hash = (hash * 397) ^ DeathSaveFailures;
                hash = (hash * 397) ^ (int)ConditionFlags;
                hash = (hash * 397) ^ ExhaustionLevel;
                hash = (hash * 397) ^ HasInspiration.GetHashCode();
                return hash;
            }
        }
    }
}

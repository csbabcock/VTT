using System;
using GameCore.PlayerData;
using Unity.Netcode;

namespace GameCore.Networking
{
    /// <summary>Netcode-serializable transport form of <see cref="CharacterCombatState"/>.</summary>
    public struct CharacterCombatStateNetwork : INetworkSerializable, IEquatable<CharacterCombatStateNetwork>
    {
        public int CurrentHitPoints;
        public int TemporaryHitPoints;
        public int DeathSaveSuccesses;
        public int DeathSaveFailures;
        public uint ConditionFlags;
        public byte ExhaustionLevel;
        public bool HasInspiration;

        public static CharacterCombatStateNetwork FromCore(CharacterCombatState state)
        {
            return new CharacterCombatStateNetwork
            {
                CurrentHitPoints = state.CurrentHitPoints,
                TemporaryHitPoints = state.TemporaryHitPoints,
                DeathSaveSuccesses = state.DeathSaveSuccesses,
                DeathSaveFailures = state.DeathSaveFailures,
                ConditionFlags = state.ConditionFlags,
                ExhaustionLevel = state.ExhaustionLevel,
                HasInspiration = state.HasInspiration,
            };
        }

        public CharacterCombatState ToCore()
        {
            return new CharacterCombatState
            {
                CurrentHitPoints = CurrentHitPoints,
                TemporaryHitPoints = TemporaryHitPoints,
                DeathSaveSuccesses = DeathSaveSuccesses,
                DeathSaveFailures = DeathSaveFailures,
                ConditionFlags = ConditionFlags,
                ExhaustionLevel = ExhaustionLevel,
                HasInspiration = HasInspiration,
            };
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref CurrentHitPoints);
            serializer.SerializeValue(ref TemporaryHitPoints);
            serializer.SerializeValue(ref DeathSaveSuccesses);
            serializer.SerializeValue(ref DeathSaveFailures);
            serializer.SerializeValue(ref ConditionFlags);
            serializer.SerializeValue(ref ExhaustionLevel);
            serializer.SerializeValue(ref HasInspiration);
        }

        public bool Equals(CharacterCombatStateNetwork other)
        {
            return CurrentHitPoints == other.CurrentHitPoints
                   && TemporaryHitPoints == other.TemporaryHitPoints
                   && DeathSaveSuccesses == other.DeathSaveSuccesses
                   && DeathSaveFailures == other.DeathSaveFailures
                   && ConditionFlags == other.ConditionFlags
                   && ExhaustionLevel == other.ExhaustionLevel
                   && HasInspiration == other.HasInspiration;
        }

        public override bool Equals(object obj) => obj is CharacterCombatStateNetwork other && Equals(other);

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

using GameCore.PlayerData;

namespace GameCore.Actors
{
    /// <summary>
    /// DM-facing authority for reading and mutating a character's combat-tracking
    /// sheet fields. Network implementations validate on the server; offline
    /// implementations mutate the backing sheet directly.
    /// </summary>
    public interface ICharacterSheetAuthority : ICharacterHitPointsAuthority
    {
        CharacterCombatState CombatState { get; }

        int TemporaryHitPoints { get; }
        int DeathSaveSuccesses { get; }
        int DeathSaveFailures { get; }
        uint ConditionFlags { get; }
        int ExhaustionLevel { get; }
        bool HasInspiration { get; }

        void RequestSetTemporaryHitPoints(int value);
        void RequestSetDeathSaves(int successes, int failures);
        void RequestResetDeathSaves();
        void RequestToggleCondition(string conditionId);
        void RequestSetExhaustionLevel(int level);
        void RequestSetInspiration(bool hasInspiration);
    }
}

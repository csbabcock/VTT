using GameCore.Networking;
using GameCore.PlayerData;

namespace GameCore.Actors
{
    /// <summary>
    /// Offline/local DM mutations against an in-memory character sheet. Used when no
    /// networked authority component is present on the actor.
    /// </summary>
    public sealed class DirectCharacterSheetAuthority : ICharacterSheetAuthority
    {
        private readonly DnD5eCharacterData _data;
        private readonly IPlayerDataService _service;
        private readonly IActor _actor;

        public DirectCharacterSheetAuthority(DnD5eCharacterData data, IPlayerDataService service, IActor actor = null)
        {
            _data = data;
            _service = service;
            _actor = actor;
        }

        public CharacterCombatState CombatState => CharacterCombatState.FromSheet(_data);

        public int CurrentHitPoints => CombatState.CurrentHitPoints;

        public int MaxHitPoints => CharacterHitPoints.GetDisplayMaxHp(_data);

        public int TemporaryHitPoints => CombatState.TemporaryHitPoints;

        public int DeathSaveSuccesses => CombatState.DeathSaveSuccesses;

        public int DeathSaveFailures => CombatState.DeathSaveFailures;

        public uint ConditionFlags => CombatState.ConditionFlags;

        public int ExhaustionLevel => CombatState.ExhaustionLevel;

        public bool HasInspiration => CombatState.HasInspiration;

        public void RequestSetCurrentHitPoints(int value) => ApplyMutated(state =>
        {
            state.CurrentHitPoints = value;
            return state;
        });

        public void RequestAdjustCurrentHitPoints(int delta) =>
            RequestSetCurrentHitPoints(CurrentHitPoints + delta);

        public void RequestSetTemporaryHitPoints(int value) => ApplyMutated(state =>
        {
            state.TemporaryHitPoints = CharacterCombatStateRules.ClampTemporaryHitPoints(value);
            return state;
        });

        public void RequestSetDeathSaves(int successes, int failures) => ApplyMutated(state =>
        {
            state.DeathSaveSuccesses = CharacterCombatStateRules.ClampDeathSaveCount(successes);
            state.DeathSaveFailures = CharacterCombatStateRules.ClampDeathSaveCount(failures);
            return state;
        });

        public void RequestResetDeathSaves() => RequestSetDeathSaves(0, 0);

        public void RequestToggleCondition(string conditionId) => ApplyMutated(state =>
        {
            state.ConditionFlags = DnD5eConditions.Toggle(state.ConditionFlags, conditionId);
            return state;
        });

        public void RequestSetExhaustionLevel(int level) => ApplyMutated(state =>
        {
            state.ExhaustionLevel = CharacterCombatStateRules.ClampExhaustion(level);
            return state;
        });

        public void RequestSetInspiration(bool hasInspiration) => ApplyMutated(state =>
        {
            state.HasInspiration = hasInspiration;
            return state;
        });

        private void ApplyMutated(System.Func<CharacterCombatState, CharacterCombatState> mutate)
        {
            if (_data == null)
                return;

            bool isDm = Networking.SessionRoleLocator.IsDungeonMaster;
            bool isOwner = _actor != null && CharacterCombatMutationPolicy.IsLocalOwner(_actor);
            if (!CharacterCombatMutationPolicy.CanMutate(isDm, isOwner))
                return;

            var state = CharacterCombatState.FromSheet(_data);
            state = mutate(state);

            int maxHp = CharacterHitPoints.GetDisplayMaxHp(_data);
            state.CurrentHitPoints = CharacterHitPoints.ClampCurrent(state.CurrentHitPoints, maxHp);
            state.ApplyToSheet(_data);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            switch (_service)
            {
                case InMemoryPlayerDataService inMemory:
                    inMemory.NotifyChanged();
                    break;
                case JsonPlayerDataService json:
                    json.NotifyChanged();
                    break;
                case LocalPlayerDataService local:
                    local.NotifyChanged();
                    break;
            }
        }
    }
}

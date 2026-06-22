using GameCore.Actors;
using GameCore.PlayerData;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Transmits the owning client's selected character to the server on spawn and
    /// replicates combat-tracking sheet fields to every client.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkCharacterIdentity : NetworkBehaviour, ICharacterSheetAuthority
    {
        [SerializeField] private PlayerActor _playerActor;

        private readonly NetworkVariable<FixedString128Bytes> _displayName =
            new NetworkVariable<FixedString128Bytes>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<CharacterCombatStateNetwork> _combatState =
            new NetworkVariable<CharacterCombatStateNetwork>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public CharacterCombatState CombatState =>
            IsSpawned ? _combatState.Value.ToCore() : CharacterCombatState.FromSheet(GetSheetData());

        public int CurrentHitPoints => CombatState.CurrentHitPoints;

        public int MaxHitPoints => CharacterHitPoints.GetDisplayMaxHp(GetSheetData());

        public int TemporaryHitPoints => CombatState.TemporaryHitPoints;

        public int DeathSaveSuccesses => CombatState.DeathSaveSuccesses;

        public int DeathSaveFailures => CombatState.DeathSaveFailures;

        public uint ConditionFlags => CombatState.ConditionFlags;

        public int ExhaustionLevel => CombatState.ExhaustionLevel;

        public bool HasInspiration => CombatState.HasInspiration;

        public override void OnNetworkSpawn()
        {
            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            _displayName.OnValueChanged += HandleDisplayNameChanged;
            _combatState.OnValueChanged += HandleCombatStateChanged;
            ApplyDisplayName(_displayName.Value);
            ApplyCombatStateToLocalServices(_combatState.Value.ToCore());

            if (IsOwner)
                SubmitLocalCharacter();
        }

        public override void OnNetworkDespawn()
        {
            _displayName.OnValueChanged -= HandleDisplayNameChanged;
            _combatState.OnValueChanged -= HandleCombatStateChanged;
        }

        public void RequestSetCurrentHitPoints(int value) =>
            RequestCombatMutation(state =>
            {
                state.CurrentHitPoints = value;
                return state;
            });

        public void RequestAdjustCurrentHitPoints(int delta) =>
            RequestSetCurrentHitPoints(CurrentHitPoints + delta);

        public void RequestSetTemporaryHitPoints(int value) =>
            RequestCombatMutation(state =>
            {
                state.TemporaryHitPoints = CharacterCombatStateRules.ClampTemporaryHitPoints(value);
                return state;
            });

        public void RequestSetDeathSaves(int successes, int failures) =>
            RequestCombatMutation(state =>
            {
                state.DeathSaveSuccesses = CharacterCombatStateRules.ClampDeathSaveCount(successes);
                state.DeathSaveFailures = CharacterCombatStateRules.ClampDeathSaveCount(failures);
                return state;
            });

        public void RequestResetDeathSaves() => RequestSetDeathSaves(0, 0);

        public void RequestToggleCondition(string conditionId) =>
            RequestCombatMutation(state =>
            {
                state.ConditionFlags = DnD5eConditions.Toggle(state.ConditionFlags, conditionId);
                return state;
            });

        public void RequestSetExhaustionLevel(int level) =>
            RequestCombatMutation(state =>
            {
                state.ExhaustionLevel = CharacterCombatStateRules.ClampExhaustion(level);
                return state;
            });

        public void RequestSetInspiration(bool hasInspiration) =>
            RequestCombatMutation(state =>
            {
                state.HasInspiration = hasInspiration;
                return state;
            });

        private void RequestCombatMutation(System.Func<CharacterCombatState, CharacterCombatState> mutate)
        {
            if (!SessionRoleLocator.IsDungeonMaster)
                return;

            var state = CombatState;
            state = mutate(state);

            int maxHp = CharacterHitPoints.GetDisplayMaxHp(GetSheetData());
            state.CurrentHitPoints = CharacterHitPoints.ClampCurrent(state.CurrentHitPoints, maxHp);

            if (!IsNetworkActive())
            {
                ApplyCombatStateLocal(state);
                return;
            }

            ApplyCombatStateServerRpc(CharacterCombatStateNetwork.FromCore(state));
        }

        private void SubmitLocalCharacter()
        {
            var sheet = PlayerDataServiceLocator.Service?.GetCharacterSheet() as DnD5eCharacterData;
            string json = sheet != null ? PlayerDataJsonLoader.ToJson(sheet, false) : string.Empty;
            string fallbackName = SessionRoleLocator.IsDungeonMaster ? "DM" : $"Player {OwnerClientId}";
            SubmitCharacterServerRpc(json, fallbackName);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitCharacterServerRpc(string characterJson, string fallbackName)
        {
            DnD5eCharacterData data = null;
            if (!string.IsNullOrEmpty(characterJson))
                data = PlayerDataJsonLoader.LoadFromJson(characterJson);

            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            if (data != null && _playerActor != null)
                _playerActor.SetDataService(new InMemoryPlayerDataService(data));

            string resolvedName = data != null && !string.IsNullOrEmpty(data.characterName)
                ? data.characterName
                : fallbackName;

            _displayName.Value = new FixedString128Bytes(Truncate(resolvedName, 60));

            if (data != null)
                ApplyCombatStateAuthoritative(CharacterCombatState.FromSheet(data));

            if (_playerActor != null)
                ActorRegistry.NotifyActorUpdated(_playerActor);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ApplyCombatStateServerRpc(CharacterCombatStateNetwork newState, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
                return;

            ApplyCombatStateAuthoritative(newState.ToCore());
        }

        private void HandleDisplayNameChanged(FixedString128Bytes previous, FixedString128Bytes current)
        {
            ApplyDisplayName(current);
        }

        private void HandleCombatStateChanged(CharacterCombatStateNetwork previous, CharacterCombatStateNetwork current)
        {
            ApplyCombatStateToLocalServices(current.ToCore());
        }

        private void ApplyDisplayName(FixedString128Bytes value)
        {
            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            string name = value.ToString();
            if (!string.IsNullOrEmpty(name))
                _playerActor?.SetDisplayName(name);
        }

        private void ApplyCombatStateAuthoritative(CharacterCombatState state)
        {
            var data = GetSheetData();
            if (data == null)
                return;

            int maxHp = CharacterHitPoints.GetDisplayMaxHp(data);
            state.CurrentHitPoints = CharacterHitPoints.ClampCurrent(state.CurrentHitPoints, maxHp);
            state.ApplyToSheet(data);
            _combatState.Value = CharacterCombatStateNetwork.FromCore(CharacterCombatState.FromSheet(data));
            NotifyActorServiceChanged();
        }

        private void ApplyCombatStateLocal(CharacterCombatState state)
        {
            var data = GetSheetData();
            if (data == null)
                return;

            int maxHp = CharacterHitPoints.GetDisplayMaxHp(data);
            state.CurrentHitPoints = CharacterHitPoints.ClampCurrent(state.CurrentHitPoints, maxHp);
            state.ApplyToSheet(data);

            if (IsServer)
                _combatState.Value = CharacterCombatStateNetwork.FromCore(CharacterCombatState.FromSheet(data));

            ApplyCombatStateToLocalServices(CharacterCombatState.FromSheet(data));
        }

        private void ApplyCombatStateToLocalServices(CharacterCombatState state)
        {
            var data = GetSheetData();
            if (data != null)
                state.ApplyToSheet(data);

            NotifyActorServiceChanged();

            if (_playerActor != null && _playerActor.IsLocalPlayer)
            {
                var localSheet = PlayerDataServiceLocator.Service?.GetCharacterSheet() as DnD5eCharacterData;
                if (localSheet != null && !ReferenceEquals(localSheet, data))
                {
                    state.ApplyToSheet(localSheet);
                    NotifyServiceChanged(PlayerDataServiceLocator.Service);
                }
            }
        }

        private void NotifyActorServiceChanged()
        {
            NotifyServiceChanged(_playerActor?.DataService);
        }

        private void NotifyServiceChanged(IPlayerDataService service)
        {
            if (service == null)
                return;

            switch (service)
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

        private DnD5eCharacterData GetSheetData()
        {
            return _playerActor?.DataService?.GetCharacterSheet() as DnD5eCharacterData
                   ?? (_playerActor != null && _playerActor.IsLocalPlayer
                       ? PlayerDataServiceLocator.Service?.GetCharacterSheet() as DnD5eCharacterData
                       : null);
        }

        private static bool IsNetworkActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }

        private static string Truncate(string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
                return value ?? string.Empty;
            return value.Substring(0, maxChars);
        }
    }
}

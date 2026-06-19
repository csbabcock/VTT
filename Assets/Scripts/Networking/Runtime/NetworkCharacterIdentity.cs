using GameCore.Actors;
using GameCore.PlayerData;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Transmits the owning client's selected character to the server on spawn and
    /// replicates the player's display name and current hit points to everyone.
    ///
    /// Add to the player prefab alongside <see cref="PlayerActor"/>,
    /// <see cref="NetworkPlayerController"/>, and a NetworkObject. Flow:
    /// <list type="bullet">
    /// <item>The owning client serializes its local <see cref="DnD5eCharacterData"/> and
    /// sends it via a ServerRpc.</item>
    /// <item>The server deserializes it, injects an <see cref="InMemoryPlayerDataService"/>
    /// into this object's <see cref="PlayerActor"/> (so DM tools can read the sheet), and
    /// writes the display name into a server-authoritative NetworkVariable.</item>
    /// <item>Every client applies the replicated name to its copy of the actor so remote
    /// players are labeled correctly even though only the server holds the full sheet.</item>
    /// <item>Current HP is replicated via a server-authoritative NetworkVariable; only the
    /// host (DM) may change it through <see cref="ICharacterHitPointsAuthority"/>.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkCharacterIdentity : NetworkBehaviour, ICharacterHitPointsAuthority
    {
        [SerializeField] private PlayerActor _playerActor;

        private readonly NetworkVariable<FixedString128Bytes> _displayName =
            new NetworkVariable<FixedString128Bytes>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _currentHitPoints =
            new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public int CurrentHitPoints =>
            IsSpawned ? _currentHitPoints.Value : ReadCurrentFromSheet();

        public int MaxHitPoints => CharacterHitPoints.GetDisplayMaxHp(GetSheetData());

        public override void OnNetworkSpawn()
        {
            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            _displayName.OnValueChanged += HandleDisplayNameChanged;
            _currentHitPoints.OnValueChanged += HandleHitPointsChanged;
            ApplyDisplayName(_displayName.Value);
            ApplyHitPointsToLocalServices(_currentHitPoints.Value);

            if (IsOwner)
                SubmitLocalCharacter();
        }

        public override void OnNetworkDespawn()
        {
            _displayName.OnValueChanged -= HandleDisplayNameChanged;
            _currentHitPoints.OnValueChanged -= HandleHitPointsChanged;
        }

        public void RequestSetCurrentHitPoints(int value)
        {
            if (!SessionRoleLocator.IsDungeonMaster)
                return;

            if (!IsNetworkActive())
            {
                ApplyHitPointsLocal(value);
                return;
            }

            SetCurrentHitPointsServerRpc(value);
        }

        public void RequestAdjustCurrentHitPoints(int delta)
        {
            RequestSetCurrentHitPoints(CurrentHitPoints + delta);
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
            {
                // Inject the real sheet into the server-side actor so DM tools can read it.
                _playerActor.SetDataService(new InMemoryPlayerDataService(data));
            }

            string resolvedName = data != null && !string.IsNullOrEmpty(data.characterName)
                ? data.characterName
                : fallbackName;

            _displayName.Value = new FixedString128Bytes(Truncate(resolvedName, 60));

            if (data != null)
            {
                int maxHp = CharacterHitPoints.GetDisplayMaxHp(data);
                int current = CharacterHitPoints.ClampCurrent(data.currentHitPoints, maxHp);
                data.currentHitPoints = current;
                _currentHitPoints.Value = current;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetCurrentHitPointsServerRpc(int newValue, RpcParams rpcParams = default)
        {
            // Host-as-DM model: only the server client (client id 0) may adjust HP.
            if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
                return;

            ApplyHitPointsAuthoritative(newValue);
        }

        private void HandleDisplayNameChanged(FixedString128Bytes previous, FixedString128Bytes current)
        {
            ApplyDisplayName(current);
        }

        private void HandleHitPointsChanged(int previous, int current)
        {
            ApplyHitPointsToLocalServices(current);
        }

        private void ApplyDisplayName(FixedString128Bytes value)
        {
            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            string name = value.ToString();
            if (!string.IsNullOrEmpty(name))
                _playerActor?.SetDisplayName(name);
        }

        private void ApplyHitPointsAuthoritative(int newValue)
        {
            var data = GetSheetData();
            if (data == null)
                return;

            int clamped = CharacterHitPoints.ClampCurrent(newValue, CharacterHitPoints.GetDisplayMaxHp(data));
            data.currentHitPoints = clamped;
            _currentHitPoints.Value = clamped;
            NotifyActorServiceChanged();
        }

        private void ApplyHitPointsLocal(int newValue)
        {
            var data = GetSheetData();
            if (data == null)
                return;

            int clamped = CharacterHitPoints.ClampCurrent(newValue, CharacterHitPoints.GetDisplayMaxHp(data));
            data.currentHitPoints = clamped;

            if (IsServer)
                _currentHitPoints.Value = clamped;

            ApplyHitPointsToLocalServices(clamped);
        }

        private void ApplyHitPointsToLocalServices(int current)
        {
            var data = GetSheetData();
            if (data != null)
                data.currentHitPoints = current;

            NotifyActorServiceChanged();

            // Keep the owning client's menu-selected sheet in sync when they are the target.
            if (_playerActor != null && _playerActor.IsLocalPlayer)
            {
                var localSheet = PlayerDataServiceLocator.Service?.GetCharacterSheet() as DnD5eCharacterData;
                if (localSheet != null && !ReferenceEquals(localSheet, data))
                {
                    localSheet.currentHitPoints = current;
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

        private int ReadCurrentFromSheet()
        {
            var data = GetSheetData();
            return data?.currentHitPoints ?? 0;
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

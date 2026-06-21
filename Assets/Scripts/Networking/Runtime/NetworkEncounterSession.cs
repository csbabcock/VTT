using System;
using System.Collections.Generic;
using GameCore.Actors;
using GameCore.EncounterMode;
using GameCore.PlayerData;
using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Server-authoritative encounter session state: active flag and current turn owner.
    /// Place on the EncounterModeManager GameObject with a <see cref="NetworkObject"/>.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkEncounterSession : NetworkBehaviour, IEncounterSessionAuthority
    {
        private const int NoTurnOwner = -1;

        [SerializeField] private EncounterModeManager _encounterModeManager;

        private readonly NetworkVariable<bool> _isEncounterActive =
            new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _currentTurnOwnerId =
            new NetworkVariable<int>(
                NoTurnOwner,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly List<int> _turnOrder = new List<int>();
        private int _turnIndex;

        public bool IsEncounterActive => _isEncounterActive.Value;
        public int CurrentTurnOwnerId => _currentTurnOwnerId.Value;
        public bool HasActiveTurnOrder => IsEncounterActive && _currentTurnOwnerId.Value != NoTurnOwner;

        public event Action<bool> EncounterActiveChanged;
        public event Action<int> TurnOwnerChanged;

        public override void OnNetworkSpawn()
        {
            if (_encounterModeManager == null)
                _encounterModeManager = GetComponent<EncounterModeManager>();

            EncounterSessionLocator.Authority = this;
            EncounterSessionLocator.Manager = _encounterModeManager;

            _isEncounterActive.OnValueChanged += HandleEncounterActiveChanged;
            _currentTurnOwnerId.OnValueChanged += HandleTurnOwnerChanged;

            ApplyEncounterActive(_isEncounterActive.Value);
            EncounterActiveChanged?.Invoke(_isEncounterActive.Value);
            TurnOwnerChanged?.Invoke(_currentTurnOwnerId.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isEncounterActive.OnValueChanged -= HandleEncounterActiveChanged;
            _currentTurnOwnerId.OnValueChanged -= HandleTurnOwnerChanged;

            if (ReferenceEquals(EncounterSessionLocator.Authority, this))
                EncounterSessionLocator.Authority = null;
            if (ReferenceEquals(EncounterSessionLocator.Manager, _encounterModeManager))
                EncounterSessionLocator.Manager = null;
        }

        public void RequestToggleEncounter()
        {
            if (!SessionRoleLocator.IsDungeonMaster)
                return;

            if (!IsNetworkActive())
            {
                ToggleEncounterLocal();
                return;
            }

            ToggleEncounterServerRpc();
        }

        public void RequestStartTurnOrder()
        {
            if (!SessionRoleLocator.IsDungeonMaster)
                return;

            if (!IsNetworkActive())
            {
                StartTurnOrderLocal();
                return;
            }

            StartTurnOrderServerRpc();
        }

        public void RequestEndTurn()
        {
            if (!SessionRoleLocator.IsDungeonMaster)
                return;

            if (!IsNetworkActive())
            {
                AdvanceTurnLocal();
                return;
            }

            EndTurnServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ToggleEncounterServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServerHostSender(rpcParams))
                return;

            bool next = !_isEncounterActive.Value;
            _isEncounterActive.Value = next;
            if (!next)
            {
                _turnOrder.Clear();
                _currentTurnOwnerId.Value = NoTurnOwner;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void StartTurnOrderServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServerHostSender(rpcParams))
                return;

            _isEncounterActive.Value = true;
            RollInitiativeAndStartTurn();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void EndTurnServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServerHostSender(rpcParams))
                return;

            AdvanceTurnServer();
        }

        private void HandleEncounterActiveChanged(bool previous, bool current)
        {
            ApplyEncounterActive(current);
            EncounterActiveChanged?.Invoke(current);
        }

        private void HandleTurnOwnerChanged(int previous, int current)
        {
            TurnOwnerChanged?.Invoke(current);
        }

        private void ApplyEncounterActive(bool isActive)
        {
            if (_encounterModeManager == null)
                return;

            if (isActive && !_encounterModeManager.IsEncounterModeActive)
                _encounterModeManager.ApplyEncounterActive(true);
            else if (!isActive && _encounterModeManager.IsEncounterModeActive)
                _encounterModeManager.ApplyEncounterActive(false);
        }

        private void ToggleEncounterLocal()
        {
            bool next = _encounterModeManager != null && !_encounterModeManager.IsEncounterModeActive;
            ApplyEncounterActive(next);
            EncounterActiveChanged?.Invoke(next);
            if (!next)
            {
                _turnOrder.Clear();
                TurnOwnerChanged?.Invoke(NoTurnOwner);
            }
        }

        private void StartTurnOrderLocal()
        {
            ApplyEncounterActive(true);
            EncounterActiveChanged?.Invoke(true);
            RollInitiativeAndStartTurnLocal();
        }

        private void AdvanceTurnLocal()
        {
            if (_turnOrder.Count == 0)
                return;

            _turnIndex = (_turnIndex + 1) % _turnOrder.Count;
            int nextOwner = _turnOrder[_turnIndex];
            ResetMovementForTurnOwner(nextOwner);
            TurnOwnerChanged?.Invoke(nextOwner);
        }

        private void RollInitiativeAndStartTurn()
        {
            RollInitiativeAndStartTurnLocal();
            int owner = _turnOrder.Count > 0 ? _turnOrder[_turnIndex] : NoTurnOwner;
            _currentTurnOwnerId.Value = owner;
            ResetMovementForTurnOwner(owner);
        }

        private void RollInitiativeAndStartTurnLocal()
        {
            _turnOrder.Clear();
            var scores = new List<(int ownerId, int initiative)>();

            foreach (var actor in ActorRegistry.Actors)
            {
                if (actor == null)
                    continue;

                int mod = 0;
                if (actor.Sheet is DnD5eCharacterData data)
                    mod = data.initiativeModifier;
                else if (actor.DataService?.GetCharacterSheet() is DnD5eCharacterData sheet)
                    mod = sheet.initiativeModifier;

                int roll = UnityEngine.Random.Range(1, 21) + mod;
                scores.Add((actor.OwnerId, roll));
            }

            scores.Sort((a, b) => b.initiative.CompareTo(a.initiative));
            foreach (var entry in scores)
                _turnOrder.Add(entry.ownerId);

            _turnIndex = 0;
        }

        private void AdvanceTurnServer()
        {
            if (_turnOrder.Count == 0)
                return;

            _turnIndex = (_turnIndex + 1) % _turnOrder.Count;
            int nextOwner = _turnOrder[_turnIndex];
            _currentTurnOwnerId.Value = nextOwner;
            ResetMovementForTurnOwner(nextOwner);
        }

        private void ResetMovementForTurnOwner(int ownerId)
        {
            if (ownerId == NoTurnOwner)
                return;

            var actor = ActorRegistry.GetByOwner(ownerId);
            if (actor?.Transform == null)
                return;

            var participant = actor.Transform.GetComponent<NetworkEncounterParticipant>();
            participant?.ResetTurnMovementServer();
        }

        private static bool IsServerHostSender(RpcParams rpcParams)
        {
            return rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId;
        }

        private static bool IsNetworkActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }
    }
}

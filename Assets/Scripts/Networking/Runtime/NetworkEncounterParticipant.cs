using GameCore.Actors;
using GameCore.EncounterMode;
using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Per-player networked encounter movement state and server-validated grid moves.
    /// Add to the player prefab alongside <see cref="NetworkCharacterIdentity"/>.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkEncounterParticipant : NetworkBehaviour, IEncounterMovementClient
    {
        [SerializeField] private int _baseMovementSpeedFeet = 30;

        private readonly NetworkVariable<int> _remainingMovementFeet =
            new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _dashActive =
            new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _lastCellX =
            new NetworkVariable<int>(
                -1,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _lastCellZ =
            new NetworkVariable<int>(
                -1,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public int RemainingMovementFeet => _remainingMovementFeet.Value;
        public bool IsDashActive => _dashActive.Value;
        public int EffectiveMaxSpeed => _dashActive.Value ? _baseMovementSpeedFeet * 2 : _baseMovementSpeedFeet;

        public void RequestBeginMovePhase()
        {
            if (!IsOwner)
                return;

            if (!IsNetworkActive())
                return;

            BeginMovePhaseServerRpc();
        }

        public void RequestMoveTo(GridCell targetCell, int elevation)
        {
            if (!IsOwner || targetCell == null)
                return;

            if (!IsNetworkActive())
                return;

            RequestMoveServerRpc(targetCell.X, targetCell.Z, elevation);
        }

        public void RequestDash()
        {
            if (!IsOwner)
                return;

            if (!IsNetworkActive())
                return;

            RequestDashServerRpc();
        }

        /// <summary>Called on the server when a new turn starts for this participant.</summary>
        public void ResetTurnMovementServer()
        {
            if (!IsServer)
                return;

            _dashActive.Value = false;
            _remainingMovementFeet.Value = _baseMovementSpeedFeet;
            SnapshotStartingCell();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void BeginMovePhaseServerRpc(RpcParams rpcParams = default)
        {
            if (!IsActiveTurnOwner())
                return;

            if (_remainingMovementFeet.Value <= 0)
                _remainingMovementFeet.Value = EffectiveMaxSpeed;

            SnapshotStartingCell();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestMoveServerRpc(int cellX, int cellZ, int elevation, RpcParams rpcParams = default)
        {
            if (!IsActiveTurnOwner())
                return;

            var manager = EncounterSessionLocator.Manager;
            var grid = manager?.GridGenerator;
            if (grid == null)
                return;

            GridCell targetCell = grid.GetCell(cellX, cellZ);
            if (targetCell == null || !targetCell.IsWalkable)
                return;

            GridCell fromCell = GetReferenceCell(grid);
            if (fromCell == null)
                return;

            var result = EncounterMoveValidator.Validate(fromCell, targetCell, _remainingMovementFeet.Value);
            if (!result.IsValid)
                return;

            _remainingMovementFeet.Value = result.RemainingFeetAfterMove;
            _lastCellX.Value = cellX;
            _lastCellZ.Value = cellZ;

            ApproveMoveClientRpc(cellX, cellZ, elevation, _remainingMovementFeet.Value);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestDashServerRpc(RpcParams rpcParams = default)
        {
            if (!IsActiveTurnOwner())
                return;

            if (!_dashActive.Value)
            {
                if (_remainingMovementFeet.Value <= 0)
                    _remainingMovementFeet.Value = _baseMovementSpeedFeet;
                _dashActive.Value = true;
            }

            SyncMovementStateClientRpc(_remainingMovementFeet.Value, _dashActive.Value);
        }

        [Rpc(SendTo.Owner)]
        private void ApproveMoveClientRpc(int cellX, int cellZ, int elevation, int remainingFeet)
        {
            var manager = EncounterSessionLocator.Manager;
            var grid = manager?.GridGenerator;
            GridCell cell = grid?.GetCell(cellX, cellZ);
            if (cell == null)
                return;

            manager?.ApplyApprovedNetworkMove(cell, elevation, remainingFeet, _dashActive.Value);
            GetComponent<PlayerController>()?.ApplyApprovedEncounterMove(cell, elevation);
        }

        [Rpc(SendTo.Owner)]
        private void SyncMovementStateClientRpc(int remainingFeet, bool dashActive)
        {
            EncounterSessionLocator.Manager?.ApplyApprovedNetworkMove(null, 0, remainingFeet, dashActive);
        }

        private GridCell GetReferenceCell(IGridGenerator grid)
        {
            if (_lastCellX.Value >= 0 && _lastCellZ.Value >= 0)
            {
                GridCell cached = grid.GetCell(_lastCellX.Value, _lastCellZ.Value);
                if (cached != null)
                    return cached;
            }

            Transform actorTransform = GetComponent<PlayerActor>()?.Transform ?? transform;
            return grid.GetCellAtWorldPosition(actorTransform.position);
        }

        private void SnapshotStartingCell()
        {
            var grid = EncounterSessionLocator.Manager?.GridGenerator;
            if (grid == null)
                return;

            GridCell start = grid.GetCellAtWorldPosition(transform.position);
            if (start == null)
                return;

            _lastCellX.Value = start.X;
            _lastCellZ.Value = start.Z;
        }

        private bool IsActiveTurnOwner()
        {
            var authority = EncounterSessionLocator.Authority;
            if (authority == null || !authority.HasActiveTurnOrder)
                return true;

            var actor = GetComponent<PlayerActor>();
            return actor != null && actor.OwnerId == authority.CurrentTurnOwnerId;
        }

        private static bool IsNetworkActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }
    }
}

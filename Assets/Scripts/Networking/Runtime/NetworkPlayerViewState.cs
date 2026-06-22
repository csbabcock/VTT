using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Replicates the owning client's main-camera pose so the DM can spectate that player's view.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [DefaultExecutionOrder(500)]
    public class NetworkPlayerViewState : NetworkBehaviour
    {
        private const float PositionEpsilon = 0.001f;
        private const float RotationEpsilon = 0.1f;
        private const float FieldOfViewEpsilon = 0.01f;

        private readonly NetworkVariable<PlayerViewStateNetwork> _viewState =
            new NetworkVariable<PlayerViewStateNetwork>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        public PlayerViewStateNetwork ViewState => _viewState.Value;

        private void LateUpdate()
        {
            if (!IsOwner)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            PlayerViewStateNetwork next = PlayerViewStateNetwork.FromCamera(camera);
            PlayerViewStateNetwork current = _viewState.Value;
            if (ShouldPublish(current, next))
                _viewState.Value = next;
        }

        private static bool ShouldPublish(PlayerViewStateNetwork current, PlayerViewStateNetwork next)
        {
            if (Vector3.Distance(current.Position, next.Position) > PositionEpsilon)
                return true;

            if (Quaternion.Angle(current.Rotation, next.Rotation) > RotationEpsilon)
                return true;

            if (Mathf.Abs(current.FieldOfView - next.FieldOfView) > FieldOfViewEpsilon)
                return true;

            return current.IsOrthographic != next.IsOrthographic;
        }
    }
}

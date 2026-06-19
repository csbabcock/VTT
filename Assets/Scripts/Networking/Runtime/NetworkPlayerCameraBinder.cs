using System.Collections;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Binds the scene's Cinemachine camera(s) to the <b>local</b> player's follow target
    /// on spawn. Add to the networked player prefab alongside <see cref="PlayerController"/>.
    ///
    /// The player is spawned at runtime, so the scene's virtual camera has no valid Follow
    /// target until a local player exists. On the owning client this points every
    /// <see cref="CinemachineVirtualCameraBase"/> (regular vcams and FreeLook rigs) at this
    /// player's camera target; remote clients do nothing, so each machine's camera follows
    /// only its own character.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayerCameraBinder : NetworkBehaviour
    {
        [SerializeField] private PlayerController _playerController;

        [Tooltip("Explicit follow target. If empty, PlayerController.CinemachineCameraTarget is used.")]
        [SerializeField] private Transform _followTarget;

        [Tooltip("Also set the camera's LookAt to the target (usually off for 3rd-person follow rigs).")]
        [SerializeField] private bool _bindLookAt = false;

        [Tooltip("How long to keep looking for a scene camera if none exists yet (host spawns before the gameplay scene finishes loading).")]
        [SerializeField] private float _bindTimeoutSeconds = 10f;

        public override void OnNetworkSpawn()
        {
            // Only the controlling client should drive the local camera.
            if (!IsOwner)
                return;

            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();

            // The host spawns its player during StartHost, before the gameplay scene (and
            // its Cinemachine cameras) finish loading. If no camera exists yet, keep trying
            // until one appears instead of giving up.
            if (!TryBindSceneCameras())
                StartCoroutine(BindWhenCameraAvailable());
        }

        private Transform ResolveTarget()
        {
            if (_followTarget != null)
                return _followTarget;

            if (_playerController != null && _playerController.CinemachineCameraTarget != null)
                return _playerController.CinemachineCameraTarget.transform;

            return transform;
        }

        private bool TryBindSceneCameras()
        {
            var cameras = FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsInactive.Exclude);
            if (cameras == null || cameras.Length == 0)
                return false;

            Transform target = ResolveTarget();
            foreach (var cam in cameras)
            {
                if (cam == null)
                    continue;

                cam.Follow = target;
                if (_bindLookAt)
                    cam.LookAt = target;
            }
            return true;
        }

        private IEnumerator BindWhenCameraAvailable()
        {
            float elapsed = 0f;
            while (elapsed < _bindTimeoutSeconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;

                if (TryBindSceneCameras())
                    yield break;
            }

            Debug.LogWarning("NetworkPlayerCameraBinder: No Cinemachine virtual camera appeared within the timeout; camera will not follow this player.");
        }
    }
}

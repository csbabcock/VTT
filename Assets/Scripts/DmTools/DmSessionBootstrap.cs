using GameCore.Networking;
using UnityEngine;

namespace GameCore.DmTools
{
    /// <summary>
    /// Enables DM-only systems (fly camera) and disables stray offline player avatars
    /// when the local machine is the Dungeon Master.
    /// </summary>
    [DisallowMultipleComponent]
    public class DmSessionBootstrap : MonoBehaviour
    {
        [SerializeField] private DmFlyCameraController _flyCamera;
        [SerializeField] private PlayerController[] _offlinePlayersToDisable;
        [SerializeField] private Transform _initialCameraLookTarget;
        [SerializeField] private Vector3 _initialCameraOffset = new Vector3(0f, 12f, -8f);

        private void Awake()
        {
            if (_flyCamera == null)
                _flyCamera = FindAnyObjectByType<DmFlyCameraController>(FindObjectsInactive.Include);

            bool isDm = SessionRoleLocator.IsDungeonMaster;
            if (_flyCamera != null)
                _flyCamera.enabled = isDm;

            if (!isDm)
                return;

            DisableOfflinePlayers();
            InitializeFlyCameraPose();
        }

        private void DisableOfflinePlayers()
        {
            if (_offlinePlayersToDisable == null || _offlinePlayersToDisable.Length == 0)
            {
                foreach (var controller in FindObjectsByType<PlayerController>(FindObjectsInactive.Include))
                {
                    if (controller != null && controller.enabled)
                        controller.gameObject.SetActive(false);
                }

                return;
            }

            for (int i = 0; i < _offlinePlayersToDisable.Length; i++)
            {
                if (_offlinePlayersToDisable[i] != null)
                    _offlinePlayersToDisable[i].gameObject.SetActive(false);
            }
        }

        private void InitializeFlyCameraPose()
        {
            if (_flyCamera == null)
                return;

            Vector3 lookTarget = _initialCameraLookTarget != null
                ? _initialCameraLookTarget.position
                : Vector3.zero;

            _flyCamera.transform.position = lookTarget + _initialCameraOffset;
            _flyCamera.transform.LookAt(lookTarget);
        }
    }
}

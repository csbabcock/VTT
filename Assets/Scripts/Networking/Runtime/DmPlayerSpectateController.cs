using GameCore.Actors;
using GameCore.DmTools;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Mirrors a selected player's replicated camera onto the DM's main camera.
    /// Local-only; not networked.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(200)]
    public class DmPlayerSpectateController : MonoBehaviour
    {
        private Camera _camera;
        private DmFlyCameraController _flyCamera;
        private bool _isSpectating;
        private Vector3 _savedFlyPosition;
        private Quaternion _savedFlyRotation;
        private bool _savedOrthographic;
        private float _savedFieldOfView;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _flyCamera = GetComponent<DmFlyCameraController>();
        }

        private void LateUpdate()
        {
            if (!SessionRoleLocator.IsDungeonMaster)
            {
                if (_isSpectating)
                    ExitSpectate();
                return;
            }

            if (!DmPlayerSpectateLocator.IsSpectating)
            {
                if (_isSpectating)
                    ExitSpectate();
                return;
            }

            if (!_isSpectating)
                EnterSpectate();

            NetworkPlayerViewState viewState = ResolveViewState(DmPlayerSpectateLocator.SpectatedOwnerId);
            if (viewState == null)
                return;

            viewState.ViewState.ApplyTo(_camera);
        }

        public void ExitSpectate()
        {
            if (!_isSpectating)
            {
                DmPlayerSpectateLocator.Clear();
                return;
            }

            _isSpectating = false;
            DmPlayerSpectateLocator.Clear();

            if (_flyCamera != null)
                _flyCamera.enabled = true;

            if (_camera != null)
            {
                _camera.transform.SetPositionAndRotation(_savedFlyPosition, _savedFlyRotation);
                _camera.orthographic = _savedOrthographic;
                if (!_savedOrthographic)
                    _camera.fieldOfView = _savedFieldOfView;
            }
        }

        private void EnterSpectate()
        {
            _isSpectating = true;

            if (_camera != null)
            {
                _savedFlyPosition = _camera.transform.position;
                _savedFlyRotation = _camera.transform.rotation;
                _savedOrthographic = _camera.orthographic;
                _savedFieldOfView = _camera.fieldOfView;
            }

            if (_flyCamera != null)
                _flyCamera.enabled = false;
        }

        private static NetworkPlayerViewState ResolveViewState(int ownerId)
        {
            if (ownerId < 0)
                return null;

            IActor actor = ActorRegistry.GetByOwner(ownerId);
            if (actor?.Transform == null)
                return null;

            return actor.Transform.GetComponent<NetworkPlayerViewState>();
        }
    }
}

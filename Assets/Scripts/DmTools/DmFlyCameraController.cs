using GameCore.EncounterMode.Services;
using GameCore.Networking;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameCore.DmTools
{
    /// <summary>
    /// Unity Scene View-style fly camera for the local DM client. Local-only; not networked.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class DmFlyCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _flySpeed = 10f;
        [SerializeField] private float _walkSpeed = 6f;
        [SerializeField] private float _fastMultiplier = 3f;
        [SerializeField] private float _zoomSpeed = 8f;

        [Header("Look")]
        [SerializeField] private float _lookSensitivity = 0.15f;
        [SerializeField] private float _orbitSensitivity = 0.3f;
        [SerializeField] private float _panSensitivity = 0.003f;
        [SerializeField] private float _minPitch = -89f;
        [SerializeField] private float _maxPitch = 89f;

        private Camera _camera;
        private float _yaw;
        private float _pitch;
        private Vector3 _orbitPivot;
        private bool _hasOrbitPivot;
        private Behaviour[] _disabledCinemachine;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            var euler = transform.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;
            if (_pitch > 180f)
                _pitch -= 360f;
            DisableCinemachineIfPresent();
        }

        private void OnEnable()
        {
            DisableCinemachineIfPresent();
            _camera ??= GetComponent<Camera>();
            if (_camera != null)
                _camera.orthographic = false;
        }

        private void Update()
        {
            if (!SessionRoleLocator.IsDungeonMaster
                || DmPlayerSpectateLocator.IsSpectating
                || UIInputGateLocator.ShouldBlockInput())
                return;

#if ENABLE_INPUT_SYSTEM
            ProcessInput();
#else
            Debug.LogWarning("DmFlyCameraController requires the Input System package.");
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void ProcessInput()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null)
                return;

            bool fast = keyboard.shiftKey.isPressed;
            bool alt = keyboard.altKey.isPressed;
            bool rmb = mouse.rightButton.isPressed;
            bool mmb = mouse.middleButton.isPressed;

            Vector2 mouseDelta = mouse.delta.ReadValue();
            float scroll = mouse.scroll.ReadValue().y;

            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;

            if (mmb && !alt)
            {
                position = DmFlyCameraMath.ApplyPan(position, rotation, mouseDelta, _panSensitivity);
            }
            else if (alt && mouse.leftButton.isPressed)
            {
                if (!_hasOrbitPivot)
                {
                    _orbitPivot = position + rotation * Vector3.forward * 5f;
                    _hasOrbitPivot = true;
                }

                (position, _yaw, _pitch) = DmFlyCameraMath.Orbit(
                    position,
                    _yaw,
                    _pitch,
                    _orbitPivot,
                    mouseDelta,
                    _orbitSensitivity,
                    _minPitch,
                    _maxPitch);
                rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
            else
            {
                _hasOrbitPivot = false;
            }

            if (Mathf.Abs(scroll) > 0.01f)
            {
                float zoomAmount = scroll * 0.01f * _zoomSpeed * (fast ? _fastMultiplier : 1f);
                position = DmFlyCameraMath.ApplyZoom(position, rotation, zoomAmount);
            }
            else if (alt && mouse.rightButton.isPressed)
            {
                float zoomAmount = -mouseDelta.y * 0.01f * _zoomSpeed * (fast ? _fastMultiplier : 1f);
                position = DmFlyCameraMath.ApplyZoom(position, rotation, zoomAmount);
            }

            if (rmb)
            {
                _yaw += mouseDelta.x * _lookSensitivity;
                _pitch -= mouseDelta.y * _lookSensitivity;
                _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
                rotation = Quaternion.Euler(_pitch, _yaw, 0f);

                Vector2 wasd = ReadWasd(keyboard);
                float vertical = 0f;
                if (keyboard.qKey.isPressed)
                    vertical -= 1f;
                if (keyboard.eKey.isPressed)
                    vertical += 1f;

                float speed = DmFlyCameraMath.ScaleSpeed(_flySpeed, fast, _fastMultiplier);
                position += DmFlyCameraMath.ComputeFlyMoveDelta(
                    wasd, vertical, rotation, speed, Time.deltaTime);
            }
            else
            {
                Vector2 arrows = ReadArrows(keyboard);
                if (arrows.sqrMagnitude > 0.0001f)
                {
                    float speed = DmFlyCameraMath.ScaleSpeed(_walkSpeed, fast, _fastMultiplier);
                    position += DmFlyCameraMath.ComputeFlyMoveDelta(
                        arrows, 0f, rotation, speed, Time.deltaTime);
                }
            }

            transform.position = position;
            transform.rotation = rotation;
        }

        private static Vector2 ReadWasd(Keyboard keyboard)
        {
            float x = 0f;
            float y = 0f;
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed) y += 1f;
            return new Vector2(x, y);
        }

        private static Vector2 ReadArrows(Keyboard keyboard)
        {
            float x = 0f;
            float y = 0f;
            if (keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.upArrowKey.isPressed) y += 1f;
            return new Vector2(x, y);
        }
#endif

        private void DisableCinemachineIfPresent()
        {
            if (_disabledCinemachine != null)
                return;

            var behaviours = GetComponents<Behaviour>();
            var list = new System.Collections.Generic.List<Behaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour != null && behaviour != this && behaviour.GetType().Name.Contains("Cinemachine"))
                {
                    if (behaviour.enabled)
                    {
                        behaviour.enabled = false;
                        list.Add(behaviour);
                    }
                }
            }

            if (list.Count > 0)
                _disabledCinemachine = list.ToArray();
        }
    }
}

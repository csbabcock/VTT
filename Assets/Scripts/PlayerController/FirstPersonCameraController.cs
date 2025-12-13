using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// First-person camera controller (Single Responsibility)
    /// </summary>
    public class FirstPersonCameraController : ICameraController
    {
        private readonly Transform _playerTransform;
        private readonly Camera _mainCamera;
        private readonly float _cameraHeight;
        private float _forwardOffset; // Not readonly so it can be updated at runtime
        private readonly float _topClamp;
        private readonly float _bottomClamp;
        private readonly float _cameraAngleOverride;
        private readonly bool _isMouseDevice;
        private readonly float _threshold;

        private float _yaw;
        private float _pitch;

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public FirstPersonCameraController(
            Transform playerTransform,
            Camera mainCamera,
            float cameraHeight,
            float forwardOffset,
            float topClamp,
            float bottomClamp,
            float cameraAngleOverride,
            bool isMouseDevice,
            float threshold = 0.01f)
        {
            _playerTransform = playerTransform;
            _mainCamera = mainCamera;
            _cameraHeight = cameraHeight;
            _forwardOffset = forwardOffset;
            _topClamp = topClamp;
            _bottomClamp = bottomClamp;
            _cameraAngleOverride = cameraAngleOverride;
            _isMouseDevice = isMouseDevice;
            _threshold = threshold;

            _yaw = playerTransform.eulerAngles.y;
            if (_mainCamera != null)
            {
                _pitch = _mainCamera.transform.eulerAngles.x;
                if (_pitch > 180f) _pitch -= 360f;
            }
        }

        public void ProcessRotation(Vector2 lookInput, bool lockCamera)
        {
            if (lookInput.sqrMagnitude >= _threshold && !lockCamera)
            {
                float deltaTimeMultiplier = _isMouseDevice ? 1.0f : Time.deltaTime;
                _yaw += lookInput.x * deltaTimeMultiplier;
                _pitch += lookInput.y * deltaTimeMultiplier;
            }

            _yaw = ClampAngle(_yaw, float.MinValue, float.MaxValue);
            _pitch = ClampAngle(_pitch, _bottomClamp, _topClamp);

            _playerTransform.rotation = Quaternion.Euler(0.0f, _yaw, 0.0f);
        }

        public void UpdateCamera()
        {
            if (_mainCamera == null) return;

            Vector3 cameraPosition = _playerTransform.position + Vector3.up * _cameraHeight +
                _playerTransform.forward * _forwardOffset;
            _mainCamera.transform.position = cameraPosition;
            _mainCamera.transform.rotation = Quaternion.Euler(
                _pitch + _cameraAngleOverride,
                _yaw,
                0.0f
            );
        }

        public void UpdateForwardOffset(float forwardOffset)
        {
            _forwardOffset = forwardOffset;
        }

        public void SetYaw(float yaw)
        {
            _yaw = yaw;
        }

        public void SetPitch(float pitch)
        {
            _pitch = pitch;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
    }
}


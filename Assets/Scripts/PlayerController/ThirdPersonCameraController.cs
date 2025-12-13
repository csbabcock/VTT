using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Third-person camera controller (Single Responsibility)
    /// </summary>
    public class ThirdPersonCameraController : ICameraController
    {
        private readonly GameObject _cinemachineCameraTarget;
        private readonly float _topClamp;
        private readonly float _bottomClamp;
        private readonly float _cameraAngleOverride;
        private readonly bool _isMouseDevice;
        private readonly float _threshold;

        private float _yaw;
        private float _pitch;

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public ThirdPersonCameraController(
            GameObject cinemachineCameraTarget,
            float topClamp,
            float bottomClamp,
            float cameraAngleOverride,
            bool isMouseDevice,
            float threshold = 0.01f)
        {
            _cinemachineCameraTarget = cinemachineCameraTarget;
            _topClamp = topClamp;
            _bottomClamp = bottomClamp;
            _cameraAngleOverride = cameraAngleOverride;
            _isMouseDevice = isMouseDevice;
            _threshold = threshold;

            if (_cinemachineCameraTarget != null)
            {
                _yaw = _cinemachineCameraTarget.transform.rotation.eulerAngles.y;
                _pitch = _cinemachineCameraTarget.transform.rotation.eulerAngles.x;
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
        }

        public void UpdateCamera()
        {
            if (_cinemachineCameraTarget != null)
            {
                _cinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                    _pitch + _cameraAngleOverride,
                    _yaw,
                    0.0f
                );
            }
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


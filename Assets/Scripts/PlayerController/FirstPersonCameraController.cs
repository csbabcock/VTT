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
        private float _forwardOffset; // Not readonly so it can be updated at runtime
        private readonly float _cameraCollisionRadius;
        private readonly LayerMask _cameraCollisionLayers;
        private readonly Transform _headBone;
        private float _topClamp;
        private float _bottomClamp;
        private float _cameraAngleOverride;
        private readonly bool _isMouseDevice;
        private readonly float _threshold;

        private float _yaw;
        private float _pitch;

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public FirstPersonCameraController(
            Transform playerTransform,
            Camera mainCamera,
            float forwardOffset,
            float topClamp,
            float bottomClamp,
            float cameraAngleOverride,
            bool isMouseDevice,
            float threshold = 0.01f,
            float cameraCollisionRadius = 0.2f,
            LayerMask cameraCollisionLayers = default,
            Transform headBone = null)
        {
            _playerTransform = playerTransform;
            _mainCamera = mainCamera;
            _forwardOffset = forwardOffset;
            _topClamp = topClamp;
            _bottomClamp = bottomClamp;
            _cameraAngleOverride = cameraAngleOverride;
            _isMouseDevice = isMouseDevice;
            _threshold = threshold;
            _cameraCollisionRadius = cameraCollisionRadius;
            _cameraCollisionLayers = cameraCollisionLayers;
            _headBone = headBone;

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

            // If a head bone is provided, treat that as the camera anchor for position.
            Transform anchor = _headBone != null ? _headBone : _playerTransform;

            // Desired head position and offset
            Vector3 origin = anchor.position;
            Vector3 desiredOffset = anchor.forward * _forwardOffset;
            Vector3 targetPosition = origin + desiredOffset;

            float distance = desiredOffset.magnitude;
            Vector3 direction = distance > 0.0001f ? desiredOffset.normalized : anchor.forward;

            // Handle camera collision independently of CharacterController radius.
            if (_cameraCollisionRadius > 0.0f && _cameraCollisionLayers != 0)
            {
                // 1) Try to prevent moving the camera into walls by SphereCasting from the anchor toward the desired position.
                if (distance > 0.0001f &&
                    Physics.SphereCast(origin, _cameraCollisionRadius, direction, out RaycastHit hit,
                        distance, _cameraCollisionLayers, QueryTriggerInteraction.Ignore))
                {
                    // Pull the camera back so the sphere stops just before the wall.
                    targetPosition = origin + direction * Mathf.Max(hit.distance - 0.01f, 0.0f);
                }
                else
                {
                    // 2) Fallback: if the desired target position is already inside geometry
                    // (e.g. head/anchor has been pushed slightly into a wall), keep the camera at the anchor
                    // instead of letting it sit inside the wall.
                    if (Physics.CheckSphere(targetPosition, _cameraCollisionRadius, _cameraCollisionLayers, QueryTriggerInteraction.Ignore))
                    {
                        targetPosition = origin;
                    }
                }
            }

            _mainCamera.transform.position = targetPosition;
            // Always use mouse-driven yaw/pitch for rotation so FPS view can look up and down.
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

        public void UpdateClampValues(float topClamp, float bottomClamp, float cameraAngleOverride)
        {
            _topClamp = topClamp;
            _bottomClamp = bottomClamp;
            _cameraAngleOverride = cameraAngleOverride;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
    }
}


using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Top-down camera controller. Camera is fixed above the grid center, parallel to the ground.
    /// Uses orthographic projection. Completely locked - no mouse rotation, unaffected by encounter mode.
    /// </summary>
    public class TopDownCameraController : ICameraController
    {
        private readonly Camera _mainCamera;
        private readonly Vector3 _gridCenter;
        private readonly float _height;
        private readonly float _orthographicSize;

        private float _yaw;

        public float Yaw => _yaw;
        public float Pitch => 90f; // Fixed looking straight down

        public TopDownCameraController(
            Camera mainCamera,
            Vector3 gridCenter,
            float height,
            float orthographicSize)
        {
            _mainCamera = mainCamera;
            _gridCenter = gridCenter;
            _height = height;
            _orthographicSize = orthographicSize;
            _yaw = 0f;
        }

        public void ProcessRotation(Vector2 lookInput, bool lockCamera)
        {
            // Camera is completely locked - no rotation, unaffected by look input or encounter mode
        }

        public void UpdateCamera()
        {
            if (_mainCamera == null) return;

            // Orthographic projection, parallel to ground (looking straight down)
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = _orthographicSize;

            // Fixed position: centered above the grid, high enough to see entire play area
            Vector3 cameraPos = _gridCenter + Vector3.up * _height;
            _mainCamera.transform.position = cameraPos;

            // Look straight down - camera plane parallel to ground
            _mainCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        }

        public void SetYaw(float yaw)
        {
            _yaw = yaw;
        }

        /// <summary>
        /// Restores the camera to perspective projection. Call when switching away from top-down.
        /// </summary>
        public static void RestorePerspectiveProjection(Camera camera)
        {
            if (camera != null)
            {
                camera.orthographic = false;
            }
        }
    }
}

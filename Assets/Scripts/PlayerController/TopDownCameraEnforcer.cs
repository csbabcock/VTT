using UnityEngine;
using GameCore.EncounterMode;

namespace GameCore
{
    /// <summary>
    /// Applies top-down camera. Disables all Cinemachine components on the camera
    /// when active, then sets orthographic top-down view every frame.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(Camera))]
    public class TopDownCameraEnforcer : MonoBehaviour
    {
        [Header("Top-Down Settings")]
        [SerializeField] private float _height = 50f;
        [SerializeField] private float _orthographicSize = 25f;

        private Vector3 _gridCenter;
        private bool _isActive;
        private Camera _camera;
        private Behaviour[] _cinemachineComponents;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            CacheCinemachineComponents();
        }

        private void OnEnable()
        {
            CacheCinemachineComponents();
        }

        private void CacheCinemachineComponents()
        {
            var components = GetComponents<Behaviour>();
            var list = new System.Collections.Generic.List<Behaviour>();
            foreach (var c in components)
            {
                if (c != null && c != this && c.GetType().Name.Contains("Cinemachine"))
                {
                    list.Add(c);
                }
            }
            _cinemachineComponents = list.ToArray();
        }

        private void LateUpdate()
        {
            if (!_isActive || _camera == null) return;

            // Disable Cinemachine so it doesn't override us
            foreach (var c in _cinemachineComponents)
            {
                if (c != null && c.enabled)
                    c.enabled = false;
            }

            _camera.orthographic = true;
            _camera.orthographicSize = _orthographicSize;
            _camera.transform.position = _gridCenter + Vector3.up * _height;
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        }

        /// <summary>
        /// Call to enable top-down mode. Uses grid from EncounterModeManager if available.
        /// </summary>
        public void Activate()
        {
            var encounterManager = FindAnyObjectByType<EncounterModeManager>();
            if (encounterManager != null)
            {
                _gridCenter = encounterManager.GridGenerator != null
                    ? encounterManager.GridGenerator.GridOrigin
                    : encounterManager.GridOriginPosition;
                float w = encounterManager.GridWidth * encounterManager.GridCellSize;
                float h = encounterManager.GridHeight * encounterManager.GridCellSize;
                _orthographicSize = Mathf.Max(w, h) * 0.6f;
            }
            else
            {
                _gridCenter = Vector3.zero;
            }
            _isActive = true;

            // Disable Cinemachine immediately so it doesn't override our camera
            foreach (var c in _cinemachineComponents)
            {
                if (c != null && c.enabled)
                    c.enabled = false;
            }
        }

        /// <summary>
        /// Call to disable top-down mode. Re-enables Cinemachine components.
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            foreach (var c in _cinemachineComponents)
            {
                if (c != null)
                    c.enabled = true;
            }
        }

        public void SetHeight(float height) => _height = height;
        public void SetOrthographicSize(float size) => _orthographicSize = size;
    }
}

using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Manages perspective mode switching (Single Responsibility)
    /// </summary>
    public class PerspectiveManager : IPerspectiveManager
    {
        private readonly Transform _playerTransform;
        private readonly GameObject _cinemachineCameraTarget;
        private readonly MonoBehaviour _cinemachineVirtualCamera;
        private readonly Camera _mainCamera;

        public PerspectiveMode CurrentPerspective { get; private set; }

        public PerspectiveManager(
            Transform playerTransform,
            GameObject cinemachineCameraTarget,
            MonoBehaviour cinemachineVirtualCamera,
            Camera mainCamera,
            PerspectiveMode initialPerspective)
        {
            _playerTransform = playerTransform;
            _cinemachineCameraTarget = cinemachineCameraTarget;
            _cinemachineVirtualCamera = cinemachineVirtualCamera;
            _mainCamera = mainCamera;
            CurrentPerspective = initialPerspective;
        }

        public void TogglePerspective()
        {
            CurrentPerspective = CurrentPerspective == PerspectiveMode.ThirdPerson
                ? PerspectiveMode.FirstPerson
                : PerspectiveMode.ThirdPerson;

            UpdatePerspectiveMode();
        }

        public void Initialize()
        {
            UpdatePerspectiveMode();
        }

        private void UpdatePerspectiveMode()
        {
            if (CurrentPerspective == PerspectiveMode.FirstPerson)
            {
                if (_cinemachineVirtualCamera != null)
                {
                    _cinemachineVirtualCamera.enabled = false;
                }
            }
            else
            {
                if (_cinemachineVirtualCamera != null)
                {
                    _cinemachineVirtualCamera.enabled = true;
                }
            }
        }
    }
}


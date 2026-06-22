using System;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Manages perspective mode switching (Single Responsibility).
    ///
    /// First/third person is implemented by gating the scene's CinemachineBrain: in first-person
    /// the brain is disabled so <c>FirstPersonCameraController</c>'s direct camera-transform writes
    /// take effect; third-person re-enables the brain so Cinemachine drives the camera again.
    ///
    /// The brain lives on the runtime main camera and may not exist at startup, so it is resolved
    /// lazily and cached. <see cref="Initialize"/> intentionally does nothing — touching the camera
    /// before the runtime camera is bound previously broke the client camera.
    /// </summary>
    public class PerspectiveManager : IPerspectiveManager
    {
        private readonly Transform _playerTransform;
        private readonly GameObject _cinemachineCameraTarget;
        private readonly Func<Behaviour> _resolveCinemachineBrain;
        private Behaviour _cinemachineBrain;

        public PerspectiveMode CurrentPerspective { get; private set; }

        public PerspectiveManager(
            Transform playerTransform,
            GameObject cinemachineCameraTarget,
            Func<Behaviour> resolveCinemachineBrain,
            PerspectiveMode initialPerspective)
        {
            _playerTransform = playerTransform;
            _cinemachineCameraTarget = cinemachineCameraTarget;
            _resolveCinemachineBrain = resolveCinemachineBrain;
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
            // Deliberately no-op. See class summary.
        }

        private void UpdatePerspectiveMode()
        {
            Behaviour brain = ResolveBrain();
            if (brain == null)
                return;

            // First-person: disable Cinemachine so the manual first-person camera writes win.
            // Third-person: re-enable Cinemachine to drive the camera from the follow vcam.
            brain.enabled = CurrentPerspective != PerspectiveMode.FirstPerson;
        }

        private Behaviour ResolveBrain()
        {
            if (_cinemachineBrain != null)
                return _cinemachineBrain;

            _cinemachineBrain = _resolveCinemachineBrain?.Invoke();
            return _cinemachineBrain;
        }
    }
}


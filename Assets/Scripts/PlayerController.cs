using UnityEngine;
using GameCore.EncounterMode;
using GameCore.EncounterMode.Services;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using PlayerInputComponent = UnityEngine.InputSystem.PlayerInput;
#endif

namespace GameCore
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInputComponent))]
#endif
    public class PlayerController : MonoBehaviour, IEncounterLocomotion
    {
        [Header("Perspective")]
        [Tooltip("Current perspective mode")]
        public PerspectiveMode CurrentPerspective = PerspectiveMode.ThirdPerson;

        [Header("Movement")]
        [Tooltip("Current movement mode")]
        public MovementMode CurrentMovementMode = MovementMode.Normal;

        [Tooltip("Forward offset for first-person camera (pushes camera slightly forward from the head/anchor)")]
        public float FirstPersonForwardOffset = 0.0f;

        [Header("First Person Camera")]
        [Tooltip("Radius of the first-person camera collision sphere (independent of CharacterController radius).")]
        public float FirstPersonCameraRadius = 0.2f;

        [Tooltip("Layers the first-person camera collides with to avoid seeing through walls and level geometry.")]
        public LayerMask FirstPersonCameraCollisionLayers;

        [Tooltip("Optional head bone; if assigned, the first-person camera will follow this bone's transform (position/rotation).")]
        public Transform FirstPersonHeadBone;

        [Tooltip("Distance in front of the head/camera at which forward movement is stopped in first-person to prevent clipping.")]
        public float FirstPersonWallStopDistance = 0.1f;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("Optional: Cinemachine Virtual Camera to disable in first-person mode (leave null to auto-detect)")]
        public MonoBehaviour CinemachineVirtualCamera;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // Component references
        private CharacterController _controller;
        private PlayerInputs _input;
        private GameObject _mainCamera;
        private Camera _mainCameraComponent;
        private Animator _animator;
        private bool _hasAnimator;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInputComponent _playerInput;
#endif

        // Handler components (SOLID - Dependency Inversion)
        private IGroundedChecker _groundedChecker;
        private IMovementHandler _movementHandler;
        private IEncounterMovementHandler _encounterMovementHandler;
        private IJumpHandler _jumpHandler;
        private ICameraController _cameraController;
        private IAnimationHandler _animationHandler;
        private IAudioHandler _audioHandler;
        private IPerspectiveManager _perspectiveManager;
        private IEncounterModeManager _encounterModeManager;

        private FirstPersonCameraController _firstPersonCameraController;
        private ThirdPersonCameraController _thirdPersonCameraController;

        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            ResolveMainCamera();
        }

        private void ResolveMainCamera()
        {
            if (_mainCamera != null && _mainCameraComponent != null)
                return;

            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            if (_mainCamera != null)
                _mainCameraComponent = _mainCamera.GetComponent<Camera>();
        }

        private void Start()
        {
            ResolveMainCamera();
            InitializeComponents();
            InitializeHandlers();
            SubscribeToEvents();
        }

        private void InitializeComponents()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputs>();

            if (_input == null)
            {
                Debug.LogError("PlayerInputs component is missing! Please add the PlayerInputs component to the same GameObject.");
            }

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInputComponent>();
#endif
        }

        private void InitializeHandlers()
        {
            // Initialize grounded checker
            _groundedChecker = new GroundedChecker(
                transform,
                GroundedOffset,
                GroundedRadius,
                GroundLayers
            );

            // Initialize movement handler
            _movementHandler = new MovementHandler(
                _controller,
                transform,
                _mainCameraComponent,
                CurrentPerspective,
                MoveSpeed,
                SprintSpeed,
                RotationSmoothTime,
                SpeedChangeRate
            );

            // Initialize jump handler
            _jumpHandler = new JumpHandler(
                JumpHeight,
                Gravity,
                JumpTimeout,
                FallTimeout
            );

            // Initialize camera controllers
            bool isMouseDevice = IsCurrentDeviceMouse;
            _firstPersonCameraController = new FirstPersonCameraController(
                transform,
                _mainCameraComponent,
                FirstPersonForwardOffset,
                TopClamp,
                BottomClamp,
                CameraAngleOverride,
                isMouseDevice,
                _threshold,
                FirstPersonCameraRadius,
                FirstPersonCameraCollisionLayers,
                FirstPersonHeadBone
            );

            _thirdPersonCameraController = new ThirdPersonCameraController(
                CinemachineCameraTarget,
                TopClamp,
                BottomClamp,
                CameraAngleOverride,
                isMouseDevice,
                _threshold
            );

            _cameraController = CurrentPerspective == PerspectiveMode.FirstPerson
                ? (ICameraController)_firstPersonCameraController
                : _thirdPersonCameraController;

            // Initialize animation handler
            _animationHandler = new AnimationHandler(_animator);
            _animationHandler.Initialize();

            // Initialize audio handler
            _audioHandler = new AudioHandler(
                _controller,
                LandingAudioClip,
                FootstepAudioClips,
                FootstepAudioVolume
            );

            // Initialize perspective manager. The CinemachineBrain lives on the runtime main
            // camera, so resolve it lazily (it may not exist yet during Start).
            _perspectiveManager = new PerspectiveManager(
                transform,
                CinemachineCameraTarget,
                ResolveCinemachineBrain,
                CurrentPerspective
            );
            _perspectiveManager.Initialize();

            // Find encounter mode manager
            var encounterModeManagerObj = FindAnyObjectByType<EncounterModeManager>();
            if (encounterModeManagerObj != null)
            {
                _encounterModeManager = encounterModeManagerObj;
                
                // Initialize encounter movement handler
                var gridGenerator = encounterModeManagerObj.GridGenerator;
                if (gridGenerator != null)
                {
                    _encounterMovementHandler = new EncounterMovementHandler(
                        _controller,
                        transform,
                        gridGenerator,
                        SprintSpeed,
                        RotationSmoothTime
                    );
                }
            }

            // Auto-detect Cinemachine virtual camera
            AutoDetectCinemachineCamera();
        }

        private void AutoDetectCinemachineCamera()
        {
            if (CinemachineVirtualCamera == null && _mainCamera != null)
            {
                var cinemachineBrain = _mainCamera.GetComponent("CinemachineBrain");
                if (cinemachineBrain != null)
                {
                    var brainType = cinemachineBrain.GetType();
                    var activeVCamProperty = brainType.GetProperty("ActiveVirtualCamera");
                    if (activeVCamProperty != null)
                    {
                        var activeVCam = activeVCamProperty.GetValue(cinemachineBrain);
                        if (activeVCam != null)
                        {
                            CinemachineVirtualCamera = activeVCam as MonoBehaviour;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves the CinemachineBrain on the main camera (via reflection so GameCore keeps no
        /// hard Cinemachine dependency). Returned to <see cref="PerspectiveManager"/> so it can gate
        /// Cinemachine control when switching between first- and third-person.
        /// </summary>
        private Behaviour ResolveCinemachineBrain()
        {
            if (_mainCamera == null)
                return null;

            return _mainCamera.GetComponent("CinemachineBrain") as Behaviour;
        }

        private void SubscribeToEvents()
        {
            if (_input != null)
            {
#if ENABLE_INPUT_SYSTEM
                // Must NOT be named OnTogglePerspective — PlayerInput uses SendMessages and would
                // invoke that method a second time, toggling twice per key press.
                _input.OnTogglePerspective += HandlePerspectiveToggle;
                _input.OnToggleEncounterMode += HandleToggleEncounterMode;
#endif
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
#if ENABLE_INPUT_SYSTEM
                _input.OnTogglePerspective -= HandlePerspectiveToggle;
                _input.OnToggleEncounterMode -= HandleToggleEncounterMode;
#endif
            }
        }

        private void Update()
        {
            if (_input == null) return;

            SyncMovementModeWithEncounterState();

            // Update grounded state
            _groundedChecker.CheckGrounded();
            bool probedGrounded = _groundedChecker.IsGrounded;

            // If our probe sphere is touching something but the CharacterController
            // itself is not actually grounded, treat the player as airborne for
            // jump / gravity. This prevents the "slow slide" when barely touching
            // an edge or wall while falling.
            if (probedGrounded && !_controller.isGrounded)
            {
                probedGrounded = false;
            }

            // In encounter mode, override grounded state when handler says we should be grounded
            if (CurrentMovementMode == MovementMode.Encounter && _encounterMovementHandler != null)
            {
                if (_encounterMovementHandler.ShouldBeGrounded)
                {
                    Grounded = true;
                }
                else
                {
                    Grounded = probedGrounded;
                }
            }
            else
            {
                Grounded = probedGrounded;
            }

            // Process jump and gravity (only allow jump input in normal mode)
            bool jumpInput = CurrentMovementMode == MovementMode.Normal ? _input.jump : false;
            _jumpHandler.ProcessJump(jumpInput, Grounded);

            // Process movement based on mode
            if (CurrentMovementMode == MovementMode.Encounter && _encounterMovementHandler != null)
            {
                _encounterMovementHandler.ProcessMovement(Grounded);

                if (EncounterPlayerGroundingPolicy.ShouldApplyIdleGravity(
                        isEncounterMovementMode: true,
                        isMovingOnGrid: _encounterMovementHandler.IsMoving))
                {
                    _movementHandler.ApplyMovementWithVerticalVelocity(_jumpHandler.VerticalVelocity);
                }

                // Update animations using encounter movement handler
                // In encounter mode, use only encounter movement handler states (not jump handler)
                if (_hasAnimator && _animationHandler != null)
                {
                    _animationHandler.UpdateAnimations(
                        _encounterMovementHandler.AnimationBlend,
                        1f, // Motion speed is always 1.0 for encounter movement
                        Grounded,
                        _encounterMovementHandler.IsJumping,
                        _encounterMovementHandler.IsFalling
                    );
                }
            }
            else
            {
                // Normal mode: use standard movement
                Vector2 moveInput = Vector2.zero;
                if (CurrentMovementMode == MovementMode.Normal)
                {
                    // Only use movement input if character sheet is not open
                    // When character sheet is open, WASD is used for UI navigation
                    if (!UIInputGateLocator.IsCharacterSheetOpen())
                    {
                        moveInput = _input.move;
                    }
                    // If character sheet is open, moveInput remains Vector2.zero
                }
                // Prevent forward movement in first-person when a wall is directly in front of the head/camera.
                if (CurrentPerspective == PerspectiveMode.FirstPerson && moveInput.y > 0.0f && IsFirstPersonWallBlockingForward())
                {
                    moveInput.y = 0.0f;
                }

                _movementHandler.ProcessMovement(moveInput, _input.sprint, _input.analogMovement);
                _movementHandler.ApplyMovementWithVerticalVelocity(_jumpHandler.VerticalVelocity);

                // Update animations
                if (_hasAnimator && _animationHandler != null)
                {
                    _animationHandler.UpdateAnimations(
                        _movementHandler.AnimationBlend,
                        _input.analogMovement ? _input.move.magnitude : 1f,
                        Grounded,
                        _jumpHandler.IsJumping,
                        _jumpHandler.IsFalling
                    );
                }
            }
        }

        private void LateUpdate()
        {
            if (_input == null) return;

            // Always sync camera clamp values from Inspector so changes take effect at runtime.
            if (_firstPersonCameraController != null)
            {
                _firstPersonCameraController.UpdateClampValues(TopClamp, BottomClamp, CameraAngleOverride);
            }
            if (_thirdPersonCameraController != null)
            {
                _thirdPersonCameraController.UpdateClampValues(TopClamp, BottomClamp, CameraAngleOverride);
            }

            bool shouldLockCamera = ShouldLockCameraInput();

            // Process camera rotation
            _cameraController.ProcessRotation(_input.look, shouldLockCamera);
            _cameraController.UpdateCamera();

            // Update movement handler with camera yaw for first-person
            if (CurrentPerspective == PerspectiveMode.FirstPerson)
            {
                _movementHandler.SetYaw(_cameraController.Yaw);
                
                // Sync forward offset from Inspector so changes apply immediately
                if (_firstPersonCameraController != null)
                {
                    _firstPersonCameraController.UpdateForwardOffset(FirstPersonForwardOffset);
                }
            }
        }

        private bool ShouldLockCameraInput()
        {
            if (LockCameraPosition)
                return true;

            if (CurrentMovementMode == MovementMode.Encounter)
                return true;

            if (IsEncounterModeActive())
                return true;

            return UIInputGateLocator.ShouldBlockInput();
        }

        private bool IsEncounterModeActive()
        {
            if (_encounterModeManager == null)
            {
                _encounterModeManager = FindAnyObjectByType<EncounterModeManager>();
            }

            return _encounterModeManager != null && _encounterModeManager.IsEncounterModeActive;
        }

        private void SyncMovementModeWithEncounterState()
        {
            if (_encounterModeManager == null)
            {
                _encounterModeManager = FindAnyObjectByType<EncounterModeManager>();
            }

            if (_encounterModeManager == null)
                return;

            EnsureEncounterMovementHandler();

            MovementMode desiredMode = _encounterModeManager.IsEncounterModeActive
                ? MovementMode.Encounter
                : MovementMode.Normal;

            if (CurrentMovementMode == desiredMode)
                return;

            CurrentMovementMode = desiredMode;
            _encounterMovementHandler?.CancelMovement();
        }

        /// <summary>
        /// True when a wall is directly in front of the first-person head/camera, used to
        /// block forward movement and prevent clipping into geometry.
        /// </summary>
        private bool IsFirstPersonWallBlockingForward()
        {
            Vector3 anchorPosition;
            if (FirstPersonHeadBone != null)
            {
                anchorPosition = FirstPersonHeadBone.position;
            }
            else
            {
                float headHeight = _controller != null ? _controller.height * 0.9f : 1.6f;
                anchorPosition = transform.position + Vector3.up * headHeight;
            }

            float checkDistance = FirstPersonWallStopDistance + FirstPersonForwardOffset + 0.01f;
            if (checkDistance <= 0.0f || FirstPersonCameraCollisionLayers == 0)
                return false;

            return Physics.Raycast(anchorPosition, transform.forward, checkDistance,
                FirstPersonCameraCollisionLayers, QueryTriggerInteraction.Ignore);
        }

        private void HandlePerspectiveToggle()
        {
            ResolveMainCamera();

            _perspectiveManager.TogglePerspective();
            CurrentPerspective = _perspectiveManager.CurrentPerspective;

            SwitchCameraController();
            UpdateMovementHandlerPerspective();

            // Snap the camera immediately so the perspective change is visible same frame.
            if (_cameraController != null)
                _cameraController.UpdateCamera();
        }

        private void SwitchCameraController()
        {
            // Sync camera rotation when switching
            if (CurrentPerspective == PerspectiveMode.FirstPerson)
            {
                if (_firstPersonCameraController == null)
                {
                    _firstPersonCameraController = new FirstPersonCameraController(
                        transform,
                        _mainCameraComponent,
                        FirstPersonForwardOffset,
                        TopClamp,
                        BottomClamp,
                        CameraAngleOverride,
                        IsCurrentDeviceMouse,
                        _threshold,
                        FirstPersonCameraRadius,
                        FirstPersonCameraCollisionLayers,
                        FirstPersonHeadBone
                    );
                }
                
                _firstPersonCameraController.SetYaw(_cameraController.Yaw);
                _firstPersonCameraController.SetPitch(_cameraController.Pitch);
                _cameraController = _firstPersonCameraController;
            }
            else
            {
                if (_thirdPersonCameraController == null)
                {
                    _thirdPersonCameraController = new ThirdPersonCameraController(
                        CinemachineCameraTarget,
                        TopClamp,
                        BottomClamp,
                        CameraAngleOverride,
                        IsCurrentDeviceMouse,
                        _threshold
                    );
                }
                
                _thirdPersonCameraController.SetYaw(_cameraController.Yaw);
                _thirdPersonCameraController.SetPitch(_cameraController.Pitch);
                _cameraController = _thirdPersonCameraController;
            }
        }

        private void UpdateMovementHandlerPerspective()
        {
            // Recreate movement handler with new perspective mode
            _movementHandler = new MovementHandler(
                _controller,
                transform,
                _mainCameraComponent,
                CurrentPerspective,
                MoveSpeed,
                SprintSpeed,
                RotationSmoothTime,
                SpeedChangeRate
            );
        }

        private void HandleToggleEncounterMode()
        {
            if (_encounterModeManager != null)
            {
                _encounterModeManager.ToggleEncounterMode();
                SyncMovementModeFromEncounterManager();
            }
        }

        private void SyncMovementModeFromEncounterManager()
        {
            if (_encounterModeManager == null)
                return;

            CurrentMovementMode = _encounterModeManager.IsEncounterModeActive
                ? MovementMode.Encounter
                : MovementMode.Normal;

            if (_encounterMovementHandler != null)
                _encounterMovementHandler.CancelMovement();
        }

        private void EnsureEncounterMovementHandler()
        {
            if (_encounterMovementHandler != null || _encounterModeManager == null)
                return;

            var manager = _encounterModeManager as EncounterModeManager
                          ?? EncounterSessionLocator.Manager;
            var gridGenerator = manager?.GridGenerator;
            if (gridGenerator == null)
                return;

            _encounterMovementHandler = new EncounterMovementHandler(
                _controller,
                transform,
                gridGenerator,
                SprintSpeed,
                RotationSmoothTime);
        }

        /// <summary>
        /// Applies an encounter grid move approved by the encounter coordinator
        /// (after local or server-side validation). Part of <see cref="IEncounterLocomotion"/>.
        /// </summary>
        public void ApplyMove(GameCore.EncounterMode.Grid.GridCell cell, int elevation)
        {
            if (CurrentMovementMode != MovementMode.Encounter || _encounterMovementHandler == null || cell == null)
                return;

            _encounterMovementHandler.SetTargetCell(cell, elevation);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius
            );
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                _audioHandler?.PlayFootstep();
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                _audioHandler?.PlayLanding();
            }
        }
    }
}

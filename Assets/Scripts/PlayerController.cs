using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace GameCore
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class PlayerController : MonoBehaviour
    {
        [Header("Perspective")]
        [Tooltip("Current perspective mode")]
        public PerspectiveMode CurrentPerspective = PerspectiveMode.ThirdPerson;

        [Tooltip("Height offset for first-person camera (eye level)")]
        public float FirstPersonCameraHeight = 1.6f;

        [Tooltip("Forward offset for first-person camera when sprinting (prevents head clipping)")]
        public float FirstPersonSprintForwardOffset = 0.15f;

        [Tooltip("How fast the camera moves forward/back when starting/stopping sprint")]
        public float SprintOffsetSmoothing = 5.0f;

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
        private PlayerInput _playerInput;
#endif

        // Handler components (SOLID - Dependency Inversion)
        private IGroundedChecker _groundedChecker;
        private IMovementHandler _movementHandler;
        private IJumpHandler _jumpHandler;
        private ICameraController _cameraController;
        private IAnimationHandler _animationHandler;
        private IAudioHandler _audioHandler;
        private IPerspectiveManager _perspectiveManager;

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
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            if (_mainCamera != null)
            {
                _mainCameraComponent = _mainCamera.GetComponent<Camera>();
            }
        }

        private void Start()
        {
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
            _playerInput = GetComponent<PlayerInput>();
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
                FirstPersonCameraHeight,
                FirstPersonSprintForwardOffset,
                SprintOffsetSmoothing,
                TopClamp,
                BottomClamp,
                CameraAngleOverride,
                isMouseDevice,
                _threshold
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

            // Initialize perspective manager
            _perspectiveManager = new PerspectiveManager(
                transform,
                CinemachineCameraTarget,
                CinemachineVirtualCamera,
                _mainCameraComponent,
                CurrentPerspective
            );
            _perspectiveManager.Initialize();

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

        private void SubscribeToEvents()
        {
            if (_input != null)
            {
#if ENABLE_INPUT_SYSTEM
                _input.OnTogglePerspective += OnTogglePerspective;
#endif
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
#if ENABLE_INPUT_SYSTEM
                _input.OnTogglePerspective -= OnTogglePerspective;
#endif
            }
        }

        private void Update()
        {
            if (_input == null) return;

            // Update grounded state
            _groundedChecker.CheckGrounded();
            Grounded = _groundedChecker.IsGrounded;

            // Process jump and gravity
            _jumpHandler.ProcessJump(_input.jump, Grounded);

            // Process movement
            _movementHandler.ProcessMovement(_input.move, _input.sprint, _input.analogMovement);
            _movementHandler.ApplyMovementWithVerticalVelocity(_jumpHandler.VerticalVelocity);

            // Update animations
            if (_hasAnimator && _animationHandler != null)
            {
                // Cache jump handler cast to avoid repeated casting
                JumpHandler jumpHandler = _jumpHandler as JumpHandler;
                bool isJumping = jumpHandler?.IsJumping ?? false;
                bool isFalling = jumpHandler?.IsFalling ?? false;
                
                _animationHandler.UpdateAnimations(
                    _movementHandler.AnimationBlend,
                    _input.analogMovement ? _input.move.magnitude : 1f,
                    Grounded,
                    isJumping,
                    isFalling
                );
            }
        }

        private void LateUpdate()
        {
            if (_input == null) return;

            // Process camera rotation
            _cameraController.ProcessRotation(_input.look, LockCameraPosition);
            _cameraController.UpdateCamera();

            // Update movement handler with camera yaw for first-person
            if (CurrentPerspective == PerspectiveMode.FirstPerson)
            {
                _movementHandler.SetYaw(_cameraController.Yaw);
                
                // Update sprint offset for first-person camera
                if (_firstPersonCameraController != null)
                {
                    _firstPersonCameraController.UpdateSprintOffset(
                        _input.sprint,
                        _input.move != Vector2.zero
                    );
                }
            }
        }

        // Removed - camera controller switching is now handled in OnTogglePerspective

        private void OnTogglePerspective()
        {
            _perspectiveManager.TogglePerspective();
            CurrentPerspective = _perspectiveManager.CurrentPerspective;
            
            // Immediately switch camera controller
            SwitchCameraController();
            
            // Update movement handler perspective mode
            UpdateMovementHandlerPerspective();
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
                        FirstPersonCameraHeight,
                        FirstPersonSprintForwardOffset,
                        SprintOffsetSmoothing,
                        TopClamp,
                        BottomClamp,
                        CameraAngleOverride,
                        IsCurrentDeviceMouse,
                        _threshold
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

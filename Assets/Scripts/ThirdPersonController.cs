 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    public enum PerspectiveMode
    {
        ThirdPerson,
        FirstPerson
    }

    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
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

        [Tooltip("Key to toggle between first-person and third-person (V key by default)")]
#if ENABLE_INPUT_SYSTEM
        public Key TogglePerspectiveKey = Key.V;
#else
        public KeyCode TogglePerspectiveKey = KeyCode.V;
#endif

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

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // first-person camera
        private Camera _firstPersonCamera;
        private float _currentSprintOffset = 0f;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            // get camera component for first-person mode
            if (_mainCamera != null)
            {
                _firstPersonCamera = _mainCamera.GetComponent<Camera>();
            }
        }

        private void Start()
        {
            if (CinemachineCameraTarget != null)
            {
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            }
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // Auto-detect Cinemachine virtual camera if not set
            // Note: This requires Cinemachine package. If you get compilation errors, 
            // manually assign the CinemachineVirtualCamera field in the inspector.
            if (CinemachineVirtualCamera == null && _mainCamera != null)
            {
                // Try to find CinemachineBrain component (Cinemachine package)
                var cinemachineBrain = _mainCamera.GetComponent("CinemachineBrain");
                if (cinemachineBrain != null)
                {
                    // Get the active virtual camera using reflection to avoid compile-time dependency
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

            // Initialize perspective mode
            UpdatePerspectiveMode();
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // Check for perspective toggle input
            CheckPerspectiveToggle();

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            if (CurrentPerspective == PerspectiveMode.FirstPerson)
            {
                // First-person camera rotation
                if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
                {
                    //Don't multiply mouse input by Time.deltaTime;
                    float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                    _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                    _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
                }

                // clamp our rotations so our values are limited 360 degrees
                _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                // Rotate the player horizontally (yaw)
                transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);

                // Smoothly interpolate sprint forward offset
                float targetSprintOffset = (_input.sprint && _input.move != Vector2.zero) 
                    ? FirstPersonSprintForwardOffset 
                    : 0f;
                _currentSprintOffset = Mathf.Lerp(_currentSprintOffset, targetSprintOffset, 
                    Time.deltaTime * SprintOffsetSmoothing);

                // Rotate the camera vertically (pitch) - position camera at eye level
                // Move camera forward when sprinting to prevent head clipping
                Vector3 cameraPosition = transform.position + Vector3.up * FirstPersonCameraHeight + 
                    transform.forward * _currentSprintOffset;
                _mainCamera.transform.position = cameraPosition;
                _mainCamera.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                    _cinemachineTargetYaw, 0.0f);
            }
            else
            {
                // Third-person camera rotation (original behavior)
                if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
                {
                    //Don't multiply mouse input by Time.deltaTime;
                    float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                    _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                    _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
                }

                // clamp our rotations so our values are limited 360 degrees
                _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                // Cinemachine will follow this target
                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                    _cinemachineTargetYaw, 0.0f);
            }
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                if (CurrentPerspective == PerspectiveMode.FirstPerson)
                {
                    // In first-person, movement is relative to camera forward direction
                    // Player rotation is already handled in CameraRotation, so we just calculate movement direction
                    _targetRotation = _cinemachineTargetYaw;
                }
                else
                {
                    // Third-person: rotate player to face movement direction relative to camera
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                      _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                        RotationSmoothTime);

                    // rotate to face input direction relative to camera position
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }


            Vector3 targetDirection;
            
            if (CurrentPerspective == PerspectiveMode.FirstPerson)
            {
                // In first-person, move relative to camera forward direction
                Vector3 forward = _mainCamera.transform.forward;
                Vector3 right = _mainCamera.transform.right;
                
                // Project forward and right vectors onto the horizontal plane
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                
                // Calculate movement direction based on input
                targetDirection = forward * inputDirection.z + right * inputDirection.x;
            }
            else
            {
                // Third-person: use the calculated target rotation
                targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            }

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void CheckPerspectiveToggle()
        {
            // Check for perspective toggle input
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current[TogglePerspectiveKey].wasPressedThisFrame)
            {
                TogglePerspective();
            }
#else
            if (Input.GetKeyDown(TogglePerspectiveKey))
            {
                TogglePerspective();
            }
#endif
        }

        /// <summary>
        /// Toggles between first-person and third-person perspective
        /// </summary>
        public void TogglePerspective()
        {
            CurrentPerspective = CurrentPerspective == PerspectiveMode.ThirdPerson 
                ? PerspectiveMode.FirstPerson 
                : PerspectiveMode.ThirdPerson;

            UpdatePerspectiveMode();
        }

        /// <summary>
        /// Updates camera and Cinemachine settings based on current perspective mode
        /// </summary>
        private void UpdatePerspectiveMode()
        {
            // Sync camera rotation when switching
            if (CurrentPerspective == PerspectiveMode.FirstPerson)
            {
                // When switching to first-person, sync the yaw with current player rotation
                _cinemachineTargetYaw = transform.eulerAngles.y;
                
                // Get current camera pitch
                if (_mainCamera != null)
                {
                    _cinemachineTargetPitch = _mainCamera.transform.eulerAngles.x;
                    // Clamp pitch
                    if (_cinemachineTargetPitch > 180f)
                        _cinemachineTargetPitch -= 360f;
                }

                // Disable Cinemachine virtual camera if available
                if (CinemachineVirtualCamera != null)
                {
                    CinemachineVirtualCamera.enabled = false;
                }
            }
            else
            {
                // When switching to third-person, sync Cinemachine target with current camera
                if (CinemachineCameraTarget != null)
                {
                    _cinemachineTargetYaw = CinemachineCameraTarget.transform.eulerAngles.y;
                    _cinemachineTargetPitch = CinemachineCameraTarget.transform.eulerAngles.x;
                    
                    // Clamp pitch
                    if (_cinemachineTargetPitch > 180f)
                        _cinemachineTargetPitch -= 360f;
                }
                else if (_mainCamera != null)
                {
                    // Fallback: use main camera rotation
                    _cinemachineTargetYaw = _mainCamera.transform.eulerAngles.y;
                    _cinemachineTargetPitch = _mainCamera.transform.eulerAngles.x;
                    if (_cinemachineTargetPitch > 180f)
                        _cinemachineTargetPitch -= 360f;
                }

                // Enable Cinemachine virtual camera if available
                if (CinemachineVirtualCamera != null)
                {
                    CinemachineVirtualCamera.enabled = true;
                }
            }
        }
    }
}
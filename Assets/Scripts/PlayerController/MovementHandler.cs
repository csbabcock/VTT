using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Handles player movement logic (Single Responsibility)
    /// </summary>
    public class MovementHandler : IMovementHandler
    {
        private readonly CharacterController _controller;
        private readonly Transform _transform;
        private readonly Camera _mainCamera;
        private readonly PerspectiveMode _perspectiveMode;
        private readonly float _moveSpeed;
        private readonly float _sprintSpeed;
        private readonly float _rotationSmoothTime;
        private readonly float _speedChangeRate;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation;
        private float _rotationVelocity;
        private float _currentYaw;

        // Cached vectors to avoid allocations
        private Vector3 _tempVector3 = Vector3.zero;
        private Vector3 _tempInputDirection = Vector3.zero;
        private Vector3 _tempMovement = Vector3.zero;
        private Vector3 _tempForward = Vector3.zero;
        private Vector3 _tempRight = Vector3.zero;

        public float CurrentSpeed => _speed;
        public float AnimationBlend => _animationBlend;

        public MovementHandler(
            CharacterController controller,
            Transform transform,
            Camera mainCamera,
            PerspectiveMode perspectiveMode,
            float moveSpeed,
            float sprintSpeed,
            float rotationSmoothTime,
            float speedChangeRate)
        {
            _controller = controller;
            _transform = transform;
            _mainCamera = mainCamera;
            _perspectiveMode = perspectiveMode;
            _moveSpeed = moveSpeed;
            _sprintSpeed = sprintSpeed;
            _rotationSmoothTime = rotationSmoothTime;
            _speedChangeRate = speedChangeRate;
        }

        public void SetYaw(float yaw)
        {
            _currentYaw = yaw;
        }

        public void ProcessMovement(Vector2 moveInput, bool isSprinting, bool analogMovement)
        {
            float targetSpeed = isSprinting ? _sprintSpeed : _moveSpeed;
            if (moveInput == Vector2.zero) targetSpeed = 0.0f;

            // Reuse cached vector to avoid allocation
            _tempVector3.Set(_controller.velocity.x, 0.0f, _controller.velocity.z);
            float currentHorizontalSpeed = _tempVector3.magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = analogMovement ? moveInput.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * _speedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * _speedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // Reuse cached vector to avoid allocation
            _tempInputDirection.Set(moveInput.x, 0.0f, moveInput.y);
            _tempInputDirection.Normalize();

            if (moveInput != Vector2.zero)
            {
                if (_perspectiveMode == PerspectiveMode.FirstPerson)
                {
                    _targetRotation = _currentYaw;
                }
                else
                {
                    _targetRotation = Mathf.Atan2(_tempInputDirection.x, _tempInputDirection.z) * Mathf.Rad2Deg +
                                      _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(_transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                        _rotationSmoothTime);
                    _transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }

            Vector3 targetDirection = CalculateMovementDirection(_tempInputDirection);
            // Store movement for later application with vertical velocity
            _pendingMovement = targetDirection.normalized * (_speed * Time.deltaTime);
        }

        private Vector3 _pendingMovement;

        public void ApplyMovementWithVerticalVelocity(float verticalVelocity)
        {
            // Reuse cached vector to avoid allocation
            _tempMovement.Set(0.0f, verticalVelocity, 0.0f);
            _tempMovement *= Time.deltaTime;
            _tempMovement += _pendingMovement;
            _controller.Move(_tempMovement);
            _pendingMovement = Vector3.zero;
        }

        private Vector3 CalculateMovementDirection(Vector3 inputDirection)
        {
            if (_perspectiveMode == PerspectiveMode.FirstPerson)
            {
                // Reuse cached vectors to avoid allocations
                Vector3 forward = _mainCamera.transform.forward;
                Vector3 right = _mainCamera.transform.right;
                
                _tempForward.Set(forward.x, 0f, forward.z);
                _tempForward.Normalize();
                
                _tempRight.Set(right.x, 0f, right.z);
                _tempRight.Normalize();
                
                _tempVector3 = _tempForward * inputDirection.z + _tempRight * inputDirection.x;
                return _tempVector3;
            }
            else
            {
                return Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            }
        }
    }
}


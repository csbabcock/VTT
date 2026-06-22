using UnityEngine;
using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Handles grid-based movement in encounter mode using CharacterController.Move().
    /// Uses vertical velocity control instead of direct position manipulation to work with CharacterController properly.
    /// Follows SOLID principles with extracted methods for single responsibility.
    /// </summary>
    public class EncounterMovementHandler : IEncounterMovementHandler
    {
        private readonly CharacterController _controller;
        private readonly Transform _transform;
        private readonly IGridGenerator _gridGenerator;
        private readonly float _sprintSpeed;
        private readonly float _rotationSmoothTime;

        private GridCell _targetCell;
        private int _targetElevation;
        private Vector3 _targetPosition;
        private bool _hasTarget;
        private float _speed;
        private float _animationBlend;
        private float _rotationVelocity;
        private bool _isJumping;
        private bool _isFalling;
        private float _verticalVelocity;
        private bool _justSetTarget;
        private Vector3 _lastPositionWhenTargetSet;

        // Cached vectors to avoid allocations
        private Vector3 _tempDirection = Vector3.zero;
        private Vector3 _tempMovement = Vector3.zero;

        public bool IsMoving => _hasTarget;
        public float CurrentSpeed => _speed;
        public float AnimationBlend => _animationBlend;
        public bool IsJumping => _isJumping;
        public bool IsFalling => _isFalling;
        public bool ShouldBeGrounded => !_hasTarget && _targetElevation == 0;

        public EncounterMovementHandler(
            CharacterController controller,
            Transform transform,
            IGridGenerator gridGenerator,
            float sprintSpeed,
            float rotationSmoothTime)
        {
            _controller = controller;
            _transform = transform;
            _gridGenerator = gridGenerator;
            _sprintSpeed = sprintSpeed;
            _rotationSmoothTime = rotationSmoothTime;
        }

        public void SetTargetCell(GridCell targetCell, int elevation)
        {
            if (targetCell == null)
            {
                CancelMovement();
                return;
            }

            Vector3 newTargetPos = EncounterPathPlanner.CalculateTargetPosition(targetCell, elevation, _gridGenerator.CellSize);
            Vector3 currentPos = _transform.position;

            if (EncounterPathPlanner.IsWithinArrivalThreshold(currentPos, newTargetPos, elevation, _gridGenerator.CellSize))
            {
                return; // Already at target, don't set new target
            }
            
            _targetCell = targetCell;
            _targetElevation = elevation;
            _hasTarget = true;
            _justSetTarget = true;
            _lastPositionWhenTargetSet = currentPos;
            _targetPosition = newTargetPos;
        }

        public void ProcessMovement(bool isGrounded)
        {
            if (!_hasTarget || _targetCell == null)
            {
                ClearMovementState();
                return;
            }

            Vector3 currentPos = _transform.position;
            float horizontalDistance = EncounterPathPlanner.HorizontalDistance(currentPos, _targetPosition);
            float verticalDistance = _targetPosition.y - currentPos.y;

            bool wasJustSet = _justSetTarget;
            if (_justSetTarget)
            {
                _justSetTarget = false;
            }

            bool hasMovedSinceTargetSet =
                EncounterPathPlanner.HasMovedTowardTarget(currentPos, _lastPositionWhenTargetSet, _targetPosition);

            if (CheckArrival(wasJustSet, hasMovedSinceTargetSet))
            {
                HandleArrival();
                return;
            }

            // Calculate 3D diagonal direction for proportional movement
            Vector3 diagonalDirection = EncounterPathPlanner.CalculateDiagonalDirection(currentPos, _targetPosition);
            Vector3 horizontalMovement = CalculateHorizontalMovementFromDiagonal(horizontalDistance, diagonalDirection);
            CalculateVerticalVelocityFromDiagonal(verticalDistance, currentPos, isGrounded, diagonalDirection);
            UpdateAnimationStates(verticalDistance);
            ApplyMovement(horizontalMovement);
        }

        public void CancelMovement()
        {
            _hasTarget = false;
            _targetCell = null;
            _targetElevation = 0;
            _speed = 0f;
            _animationBlend = 0f;
            _verticalVelocity = 0f;
            _isJumping = false;
            _isFalling = false;
            _justSetTarget = false;
            _lastPositionWhenTargetSet = Vector3.zero;
        }

        #region Private Helper Methods

        private bool CheckArrival(bool wasJustSet, bool hasMovedSinceTargetSet)
        {
            return _hasTarget && !wasJustSet && hasMovedSinceTargetSet &&
                   EncounterPathPlanner.IsWithinArrivalThreshold(
                       _transform.position, _targetPosition, _targetElevation, _gridGenerator.CellSize);
        }

        private void HandleArrival()
        {
            // Store elevation before clearing target (needed for ShouldBeGrounded)
            int arrivedElevation = _targetElevation;
            
            // Clear all movement and animation states
            _hasTarget = false;
            _targetCell = null;
            _targetElevation = arrivedElevation; // Keep elevation to know we're at ground level
            _speed = 0f;
            _animationBlend = 0f;
            _verticalVelocity = 0f;
            _isJumping = false;
            _isFalling = false;
        }

        private void ClearMovementState()
        {
            _speed = 0f;
            _animationBlend = 0f;
            _verticalVelocity = 0f;
            _isJumping = false;
            _isFalling = false;
        }

        /// <summary>
        /// Calculates horizontal movement from the 3D diagonal direction.
        /// This ensures horizontal and vertical movement are proportional for diagonal travel.
        /// </summary>
        private Vector3 CalculateHorizontalMovementFromDiagonal(float horizontalDistance, Vector3 diagonalDirection)
        {
            if (horizontalDistance > 0.01f && diagonalDirection.sqrMagnitude > 0.0001f)
            {
                // Extract horizontal direction from 3D direction (project to XZ plane)
                _tempDirection.Set(diagonalDirection.x, 0f, diagonalDirection.z);
                float horizontalMagnitude = _tempDirection.magnitude;
                
                if (horizontalMagnitude > 0.0001f)
                {
                    // Normalize horizontal direction for rotation
                    Vector3 normalizedHorizontal = _tempDirection / horizontalMagnitude;
                    _tempDirection = normalizedHorizontal;
                    RotateTowardDirection();
                    
                    // For movement, use the horizontal component directly scaled by sprintSpeed
                    // This maintains proportional movement: if dir is normalized, moving at speed s
                    // means horizontal = (dir.x, 0, dir.z) * s * deltaTime
                    _speed = _sprintSpeed;
                    _animationBlend = _sprintSpeed;
                    return new Vector3(diagonalDirection.x, 0f, diagonalDirection.z) * (_speed * Time.deltaTime);
                }
                else
                {
                    _tempDirection = Vector3.zero;
                    _speed = 0f;
                    _animationBlend = 0f;
                    return Vector3.zero;
                }
            }
            else
            {
                _speed = 0f;
                _animationBlend = 0f;
                return Vector3.zero;
            }
        }

        private void RotateTowardDirection()
        {
            float targetRotation = Mathf.Atan2(_tempDirection.x, _tempDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(
                _transform.eulerAngles.y,
                targetRotation,
                ref _rotationVelocity,
                _rotationSmoothTime
            );
            _transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        /// <summary>
        /// Calculates vertical velocity from the 3D diagonal direction.
        /// This ensures vertical movement matches horizontal movement proportionally for diagonal travel.
        /// </summary>
        private void CalculateVerticalVelocityFromDiagonal(float verticalDistance, Vector3 currentPos, bool isGrounded, Vector3 diagonalDirection)
        {
            float cellSize = _gridGenerator.CellSize;
            float significantVerticalThreshold = cellSize * EncounterMovementConstants.SIGNIFICANT_VERTICAL_THRESHOLD_MULTIPLIER;
            bool needsVerticalMovement = false;
            
            if (_targetElevation > 0)
            {
                needsVerticalMovement = Mathf.Abs(verticalDistance) > significantVerticalThreshold;
            }
            else if (_targetElevation == 0)
            {
                needsVerticalMovement = currentPos.y > _targetPosition.y;
            }

            if (needsVerticalMovement && diagonalDirection.sqrMagnitude > 0.0001f)
            {
                // For diagonal movement, calculate vertical velocity to match the 3D direction proportion
                // Since diagonalDirection is normalized, moving at speed s means:
                // - Horizontal speed = s * sqrt(dir.x^2 + dir.z^2)
                // - Vertical speed = s * dir.y
                // This creates a direct diagonal line
                float horizontalComponent = Mathf.Sqrt(diagonalDirection.x * diagonalDirection.x + diagonalDirection.z * diagonalDirection.z);
                
                if (horizontalComponent > 0.0001f)
                {
                    // Calculate target vertical velocity: dir.y * sprintSpeed
                    // This ensures vertical movement matches horizontal movement proportionally
                    float targetVerticalVelocity = diagonalDirection.y * _sprintSpeed;
                    
                    // Smooth the transition to avoid sudden jumps
                    _verticalVelocity = Mathf.Lerp(
                        _verticalVelocity,
                        targetVerticalVelocity,
                        Time.deltaTime * EncounterMovementConstants.VELOCITY_SMOOTHING
                    );
                    
                    // Clamp to prevent overshooting
                    float maxVerticalMove = Mathf.Abs(_verticalVelocity * Time.deltaTime);
                    if (maxVerticalMove > Mathf.Abs(verticalDistance))
                    {
                        _verticalVelocity = verticalDistance / Time.deltaTime;
                    }
                }
                else
                {
                    // Pure vertical movement (shouldn't happen in grid-based movement, but handle gracefully)
                    CalculateVerticalVelocityForMovement(verticalDistance, cellSize);
                }
            }
            else
            {
                _verticalVelocity = 0f;
            }
            
            // Force descent if needed (for ground level targets)
            if (_targetElevation == 0 && currentPos.y > _targetPosition.y + 0.01f && isGrounded)
            {
                float forceDescentSpeed = _sprintSpeed * EncounterMovementConstants.FORCE_DESCENT_SPEED_MULTIPLIER;
                _verticalVelocity = -forceDescentSpeed;
            }
        }

        private void CalculateVerticalVelocityForMovement(float verticalDistance, float cellSize)
        {
            float verticalSpeed = _sprintSpeed * EncounterMovementConstants.BASE_VERTICAL_SPEED_MULTIPLIER;
            float maxDistance = cellSize * EncounterMovementConstants.MAX_VERTICAL_DISTANCE_FOR_CALCULATION;
            float distanceFactor = Mathf.Clamp01(Mathf.Abs(verticalDistance) / maxDistance);
            float speedMultiplier;
            
            if (_targetElevation == 0 && verticalDistance < 0)
            {
                if (Mathf.Abs(verticalDistance) < EncounterMovementConstants.VERTICAL_DISTANCE_CLOSE_THRESHOLD)
                {
                    speedMultiplier = EncounterMovementConstants.CLOSE_TO_GROUND_SPEED_MULTIPLIER;
                }
                else
                {
                    speedMultiplier = Mathf.Max(
                        EncounterMovementConstants.MIN_SPEED_MULTIPLIER,
                        distanceFactor * 0.8f
                    );
                }
            }
            else
            {
                speedMultiplier = EncounterMovementConstants.MIN_SPEED_MULTIPLIER +
                                 (distanceFactor * (EncounterMovementConstants.MAX_SPEED_MULTIPLIER - EncounterMovementConstants.MIN_SPEED_MULTIPLIER));
            }
            
            float targetVerticalVelocity = Mathf.Sign(verticalDistance) * verticalSpeed * speedMultiplier;
            _verticalVelocity = Mathf.Lerp(
                _verticalVelocity,
                targetVerticalVelocity,
                Time.deltaTime * EncounterMovementConstants.VELOCITY_SMOOTHING
            );
            
            // Clamp to prevent overshooting
            float maxVerticalMove = Mathf.Abs(_verticalVelocity * Time.deltaTime);
            if (maxVerticalMove > Mathf.Abs(verticalDistance))
            {
                _verticalVelocity = verticalDistance / Time.deltaTime;
            }
        }

        private void UpdateAnimationStates(float verticalDistance)
        {
            float cellSize = _gridGenerator.CellSize;
            float significantThreshold = cellSize * EncounterMovementConstants.ANIMATION_SIGNIFICANT_THRESHOLD_MULTIPLIER;
            bool isAscending = _targetElevation > 0 && verticalDistance > significantThreshold;
            bool isDescending = verticalDistance < -significantThreshold;
            
            if (_targetElevation == 0)
            {
                UpdateAnimationStatesForGroundLevel(verticalDistance, isDescending);
            }
            else
            {
                UpdateAnimationStatesForElevated(isAscending, isDescending);
            }
        }

        private void UpdateAnimationStatesForGroundLevel(float verticalDistance, bool isDescending)
        {
            if (Mathf.Abs(verticalDistance) < EncounterMovementConstants.VERTICAL_DISTANCE_CLOSE_THRESHOLD &&
                verticalDistance >= -0.05f)
            {
                _isJumping = false;
                _isFalling = false;
            }
            else if (isDescending &&
                     _verticalVelocity < -EncounterMovementConstants.MIN_VERTICAL_VELOCITY_FOR_ANIMATION &&
                     Mathf.Abs(verticalDistance) > EncounterMovementConstants.VERTICAL_DISTANCE_MIN_FOR_FALLING)
            {
                _isJumping = false;
                _isFalling = true;
            }
            else
            {
                _isJumping = false;
                _isFalling = false;
            }
        }

        private void UpdateAnimationStatesForElevated(bool isAscending, bool isDescending)
        {
            if (isAscending && _verticalVelocity > EncounterMovementConstants.MIN_VERTICAL_VELOCITY_FOR_ANIMATION)
            {
                _isJumping = true;
                _isFalling = false;
            }
            else if (isDescending && _verticalVelocity < -EncounterMovementConstants.MIN_VERTICAL_VELOCITY_FOR_ANIMATION)
            {
                _isJumping = false;
                _isFalling = true;
            }
            else
            {
                _isJumping = false;
                _isFalling = false;
            }
        }

        private void ApplyMovement(Vector3 horizontalMovement)
        {
            _tempMovement.Set(0f, _verticalVelocity, 0f);
            _tempMovement *= Time.deltaTime;
            _tempMovement += horizontalMovement;
            _controller.Move(_tempMovement);
        }

        #endregion
    }
}

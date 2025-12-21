using UnityEngine;
using GameCore.EncounterMode.Grid;

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
        private Vector3 _tempVector3 = Vector3.zero;
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

            Vector3 newTargetPos = CalculateTargetPosition(targetCell, elevation);
            Vector3 currentPos = _transform.position;
            
            if (IsAlreadyAtTarget(currentPos, newTargetPos, elevation))
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

        public void ProcessMovement(bool isGrounded, float verticalVelocity)
        {
            if (!_hasTarget || _targetCell == null)
            {
                ClearMovementState();
                return;
            }

            Vector3 currentPos = _transform.position;
            float horizontalDistance = CalculateHorizontalDistance(currentPos);
            float verticalDistance = _targetPosition.y - currentPos.y;
            
            bool wasJustSet = _justSetTarget;
            if (_justSetTarget)
            {
                _justSetTarget = false;
            }
            
            bool hasMovedSinceTargetSet = HasMovedTowardTarget(currentPos);
            
            if (CheckArrival(horizontalDistance, verticalDistance, wasJustSet, hasMovedSinceTargetSet))
            {
                HandleArrival();
                return;
            }

            Vector3 horizontalMovement = CalculateHorizontalMovement(horizontalDistance);
            CalculateVerticalVelocity(verticalDistance, currentPos, isGrounded);
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

        private Vector3 CalculateTargetPosition(GridCell targetCell, int elevation)
        {
            float cellSize = _gridGenerator.CellSize;
            float groundLevelY = targetCell.WorldPosition.y;
            float elevationHeight = elevation * cellSize;
            
            return new Vector3(
                targetCell.WorldPosition.x,
                elevation == 0 ? groundLevelY : groundLevelY + elevationHeight,
                targetCell.WorldPosition.z
            );
        }

        private bool IsAlreadyAtTarget(Vector3 currentPos, Vector3 targetPos, int elevation)
        {
            Vector3 horizontalDiff = new Vector3(
                targetPos.x - currentPos.x,
                0f,
                targetPos.z - currentPos.z
            );
            float horizontalDistance = horizontalDiff.magnitude;
            float verticalDistance = targetPos.y - currentPos.y;
            
            float cellSize = _gridGenerator.CellSize;
            float horizontalThreshold = Mathf.Max(
                cellSize * EncounterMovementConstants.HORIZONTAL_THRESHOLD_MULTIPLIER,
                EncounterMovementConstants.MIN_HORIZONTAL_THRESHOLD
            );
            float verticalThreshold = elevation == 0
                ? EncounterMovementConstants.GROUND_LEVEL_VERTICAL_THRESHOLD
                : EncounterMovementConstants.ELEVATED_VERTICAL_THRESHOLD;
            
            if (elevation == 0)
            {
                return horizontalDistance < horizontalThreshold &&
                       (verticalDistance >= 0 && verticalDistance <= verticalThreshold);
            }
            else
            {
                return horizontalDistance < horizontalThreshold &&
                       Mathf.Abs(verticalDistance) < verticalThreshold;
            }
        }

        private float CalculateHorizontalDistance(Vector3 currentPos)
        {
            _tempVector3.Set(
                _targetPosition.x - currentPos.x,
                0f,
                _targetPosition.z - currentPos.z
            );
            return _tempVector3.magnitude;
        }

        private bool HasMovedTowardTarget(Vector3 currentPos)
        {
            float distanceMoved = Vector3.Distance(currentPos, _lastPositionWhenTargetSet);
            float initialDistToTarget = Vector3.Distance(_lastPositionWhenTargetSet, _targetPosition);
            float currentDistToTarget = Vector3.Distance(currentPos, _targetPosition);
            
            return distanceMoved > EncounterMovementConstants.MIN_MOVEMENT_DISTANCE &&
                   currentDistToTarget <= initialDistToTarget + EncounterMovementConstants.MOVEMENT_TOLERANCE;
        }

        private bool CheckArrival(float horizontalDistance, float verticalDistance, bool wasJustSet, bool hasMovedSinceTargetSet)
        {
            float cellSize = _gridGenerator.CellSize;
            float horizontalThreshold = Mathf.Max(
                cellSize * EncounterMovementConstants.HORIZONTAL_THRESHOLD_MULTIPLIER,
                EncounterMovementConstants.MIN_HORIZONTAL_THRESHOLD
            );
            
            if (_targetElevation == 0)
            {
                float verticalThreshold = EncounterMovementConstants.GROUND_LEVEL_VERTICAL_THRESHOLD;
                return _hasTarget && !wasJustSet && hasMovedSinceTargetSet &&
                       horizontalDistance < horizontalThreshold &&
                       (verticalDistance >= 0 && verticalDistance <= verticalThreshold);
            }
            else
            {
                float verticalThreshold = EncounterMovementConstants.ELEVATED_VERTICAL_THRESHOLD;
                return _hasTarget && !wasJustSet && hasMovedSinceTargetSet &&
                       horizontalDistance < horizontalThreshold &&
                       Mathf.Abs(verticalDistance) < verticalThreshold;
            }
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

        private Vector3 CalculateHorizontalMovement(float horizontalDistance)
        {
            if (horizontalDistance > 0.01f)
            {
                if (_tempVector3.sqrMagnitude > 0.0001f)
                {
                    _tempDirection = _tempVector3.normalized;
                }
                else
                {
                    _tempDirection = Vector3.zero;
                }

                if (_tempDirection.sqrMagnitude > 0.0001f)
                {
                    RotateTowardDirection();
                }

                _speed = _sprintSpeed;
                _animationBlend = _sprintSpeed;
                return _tempDirection * (_speed * Time.deltaTime);
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

        private void CalculateVerticalVelocity(float verticalDistance, Vector3 currentPos, bool isGrounded)
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

            if (needsVerticalMovement)
            {
                CalculateVerticalVelocityForMovement(verticalDistance, cellSize);
            }
            else
            {
                _verticalVelocity = 0f;
            }
            
            // Force descent if needed
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

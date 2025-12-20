using UnityEngine;
using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Handles grid-based movement in encounter mode using CharacterController.Move().
    /// Uses vertical velocity control instead of direct position manipulation to work with CharacterController properly.
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
        private float _verticalVelocity; // Vertical velocity for CharacterController.Move()
        private bool _justSetTarget; // Flag to prevent immediate arrival when target is just set
        private Vector3 _lastPositionWhenTargetSet; // Track position when target was set to detect if we've moved

        // Cached vectors to avoid allocations
        private Vector3 _tempVector3 = Vector3.zero;
        private Vector3 _tempDirection = Vector3.zero;
        private Vector3 _tempMovement = Vector3.zero;

        public bool IsMoving => _hasTarget;
        public float CurrentSpeed => _speed;
        public float AnimationBlend => _animationBlend;
        public bool IsJumping => _isJumping;
        public bool IsFalling => _isFalling;
        public bool ShouldBeGrounded => !_hasTarget && _targetElevation == 0; // Grounded when no target and at ground level

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

            
            // Calculate target position first to check if we're already there
            float cellSize = _gridGenerator.CellSize;
            float groundLevelY = targetCell.WorldPosition.y;
            float elevationHeight = elevation * cellSize;
            
            Vector3 newTargetPos = new Vector3(
                targetCell.WorldPosition.x,
                elevation == 0 ? groundLevelY : groundLevelY + elevationHeight,
                targetCell.WorldPosition.z
            );
            
            // Check if we're already at the target position using the same thresholds as arrival check
            // This prevents teleportation when selecting a cell we're already at or very close to
            Vector3 currentPos = _transform.position;
            Vector3 horizontalDiff = new Vector3(
                newTargetPos.x - currentPos.x,
                0f,
                newTargetPos.z - currentPos.z
            );
            float horizontalDistance = horizontalDiff.magnitude;
            float verticalDistance = newTargetPos.y - currentPos.y;
            
            float horizontalThreshold = Mathf.Max(cellSize * 0.5f, 0.5f);
            float verticalThreshold = elevation == 0 ? 0.02f : 0.5f;
            
            // Use the same arrival logic to determine if we're already at the target
            bool alreadyAtTarget = false;
            if (elevation == 0)
            {
                // For ground level, must be at or below target Y
                alreadyAtTarget = horizontalDistance < horizontalThreshold && 
                                 (verticalDistance >= 0 && verticalDistance <= verticalThreshold);
            }
            else
            {
                // For elevated positions, allow being slightly above or below
                alreadyAtTarget = horizontalDistance < horizontalThreshold && 
                                 Mathf.Abs(verticalDistance) < verticalThreshold;
            }
            
            // If we're already at the target, don't set a new target - just stay where we are
            // This prevents teleportation when selecting the same cell or a very close cell
            if (alreadyAtTarget)
            {
                // Don't set a target - we're already there
                // No position manipulation - let CharacterController handle it naturally
                return; // Don't set target, we're already there
            }
            
            _targetCell = targetCell;
            _targetElevation = elevation;
            _hasTarget = true;
            _justSetTarget = true; // Mark that we just set a target to prevent immediate arrival
            _lastPositionWhenTargetSet = currentPos; // Track position when target is set
            
            // Set target position (already calculated above)
            _targetPosition = newTargetPos;
        }

        public void ProcessMovement(bool isGrounded, float verticalVelocity)
        {
            // In encounter mode, we control vertical velocity ourselves
            // We ignore the passed verticalVelocity parameter and calculate our own
            
            if (!_hasTarget || _targetCell == null)
            {
                // No target - don't apply any movement, let normal physics handle it
                // This prevents interference with normal mode
                _speed = 0f;
                _animationBlend = 0f;
                _verticalVelocity = 0f;
                _isJumping = false;
                _isFalling = false;
                return;
            }

            Vector3 currentPos = _transform.position;
            
            // Calculate distances
            _tempVector3.Set(
                _targetPosition.x - currentPos.x,
                0f,
                _targetPosition.z - currentPos.z
            );
            float horizontalDistance = _tempVector3.magnitude;
            float verticalDistance = _targetPosition.y - currentPos.y;

            // Clear the "just set target" flag after calculating distances
            // This prevents immediate arrival when a new target is set at the current position
            // But we check it BEFORE clearing to prevent teleportation on the first frame
            bool wasJustSet = _justSetTarget;
            if (_justSetTarget)
            {
                _justSetTarget = false;
            }
            
            // Check if we've moved since the target was set (prevents teleportation when new target is at current position)
            // Note: If we were already at the target when SetTargetCell was called, we returned early and didn't set a target
            // We need to check if we've moved TOWARD the target, not just moved in general
            float distanceMovedSinceTargetSet = Vector3.Distance(currentPos, _lastPositionWhenTargetSet);
            float initialDistToTarget = Vector3.Distance(_lastPositionWhenTargetSet, _targetPosition);
            float currentDistToTarget = Vector3.Distance(currentPos, _targetPosition);
            
            // We've moved toward the target if:
            // 1. We've moved at least 5cm from the starting position, AND
            // 2. We're closer to the target than we were when the target was set (or at least not further away)
            // This prevents teleportation when the character is already close to the target
            bool hasMovedTowardTarget = distanceMovedSinceTargetSet > 0.05f && currentDistToTarget <= initialDistToTarget + 0.1f;
            bool hasMovedSinceTargetSet = hasMovedTowardTarget;
            
            // Check if we've arrived
            float cellSize = _gridGenerator.CellSize;
            float horizontalThreshold = Mathf.Max(cellSize * 0.5f, 0.5f);
            
            // For elevation 0 (ground level), use very strict threshold to ensure we reach ground
            // For elevated positions, allow more leeway
            float verticalThreshold;
            bool hasArrived;
            if (_targetElevation == 0)
            {
                // Ground level - must be at or below target Y (not above)
                // verticalDistance = targetY - currentY
                // When descending: currentY > targetY, so verticalDistance is negative
                // We want: verticalDistance >= 0 (meaning currentY <= targetY, we're at or below target)
                verticalThreshold = 0.02f;
                // For descending to ground, we must be at or below target (verticalDistance >= 0)
                // Allow small tolerance for being slightly below (verticalDistance <= threshold)
                // DO NOT allow negative verticalDistance (above target) - that means we haven't reached ground yet
                // Don't consider arrived if we just set the target (prevents immediate teleportation)
                // Also require that we've moved since the target was set (prevents teleportation when new target is at current position)
                // Allow arrival when at exact position as long as we didn't just set the target and we've moved
                hasArrived = _hasTarget && !wasJustSet && hasMovedSinceTargetSet && horizontalDistance < horizontalThreshold && 
                            (verticalDistance >= 0 && verticalDistance <= verticalThreshold);
            }
            else
            {
                verticalThreshold = 0.5f;
                // Don't consider arrived if we just set the target (prevents immediate teleportation)
                // Also require that we've moved since the target was set (prevents teleportation when new target is at current position)
                // Allow arrival when at exact position as long as we didn't just set the target and we've moved
                hasArrived = _hasTarget && !wasJustSet && hasMovedSinceTargetSet && horizontalDistance < horizontalThreshold && 
                            Mathf.Abs(verticalDistance) < verticalThreshold;
            }

            if (hasArrived)
            {
                // Arrived at target - no position manipulation, just clear the target
                // Let CharacterController naturally maintain position through interpolation
                
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
                
                return;
            }

            // Calculate horizontal movement
            Vector3 horizontalMovement = Vector3.zero;
            
            if (horizontalDistance > 0.01f)
            {
                // Normalize direction
                if (_tempVector3.sqrMagnitude > 0.0001f)
                {
                    _tempDirection = _tempVector3.normalized;
                }
                else
                {
                    _tempDirection = Vector3.zero;
                }

                // Rotate character to face movement direction
                if (_tempDirection.sqrMagnitude > 0.0001f)
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

                // Use sprint speed for horizontal movement
                _speed = _sprintSpeed;
                _animationBlend = _sprintSpeed;
                horizontalMovement = _tempDirection * (_speed * Time.deltaTime);
            }
            else
            {
                // Close enough horizontally
                _speed = 0f;
                _animationBlend = 0f;
            }

            // Calculate vertical velocity to reach target Y position
            // Use a simple approach: calculate velocity needed to reach target
            float significantVerticalThreshold = cellSize * 0.5f;
            bool needsVerticalMovement = false;
            
            if (_targetElevation > 0)
            {
                // Moving to elevated position
                needsVerticalMovement = Mathf.Abs(verticalDistance) > significantVerticalThreshold;
            }
            else if (_targetElevation == 0)
            {
                // Moving to ground level - always try to reach it if above
                // Continue descending until we're at or below target (verticalDistance >= 0)
                // Use a very small threshold to ensure we keep descending until we reach ground
                needsVerticalMovement = currentPos.y > _targetPosition.y;
            }

            if (needsVerticalMovement)
            {
                // Calculate vertical velocity to smoothly move toward target
                // Use a proportional controller approach
                float verticalSpeed = _sprintSpeed * 0.7f; // Base vertical speed
                
                // For elevation 0, ensure we have enough speed to reach ground
                // For other elevations, scale speed based on distance
                float maxDistance = cellSize * 2f;
                float distanceFactor = Mathf.Clamp01(Mathf.Abs(verticalDistance) / maxDistance);
                float speedMultiplier;
                
                if (_targetElevation == 0 && verticalDistance < 0)
                {
                    // Descending to ground - ensure we have enough speed to reach it
                    // When very close, use a minimum speed to push through any grounding detection
                    if (Mathf.Abs(verticalDistance) < 0.1f)
                    {
                        // Very close - use minimum speed to ensure we reach ground
                        speedMultiplier = 0.5f;
                    }
                    else
                    {
                        speedMultiplier = Mathf.Max(0.4f, distanceFactor * 0.8f);
                    }
                }
                else
                {
                    speedMultiplier = 0.4f + (distanceFactor * 0.6f); // Range: 0.4 to 1.0
                }
                
                // Calculate target velocity
                float targetVerticalVelocity = Mathf.Sign(verticalDistance) * verticalSpeed * speedMultiplier;
                
                // Smooth the velocity change
                float velocitySmoothing = 10f;
                _verticalVelocity = Mathf.Lerp(_verticalVelocity, targetVerticalVelocity, Time.deltaTime * velocitySmoothing);
                
                // Clamp to prevent overshooting
                float maxVerticalMove = Mathf.Abs(_verticalVelocity * Time.deltaTime);
                if (maxVerticalMove > Mathf.Abs(verticalDistance))
                {
                    _verticalVelocity = verticalDistance / Time.deltaTime;
                }
            }
            else
            {
                // No vertical movement needed - zero out velocity
                _verticalVelocity = 0f;
            }
            
            // Special case: If we're descending to ground level and very close, 
            // ensure we continue descending even if CharacterController thinks we're grounded
            if (_targetElevation == 0 && currentPos.y > _targetPosition.y + 0.01f && isGrounded)
            {
                // Force descent even if grounded - we need to reach the exact target position
                float forceDescentSpeed = _sprintSpeed * 0.3f; // Slow but steady descent
                _verticalVelocity = -forceDescentSpeed;
            }

            // Update animation states
            float significantThreshold = cellSize * 0.5f;
            bool isAscending = _targetElevation > 0 && verticalDistance > significantThreshold;
            bool isDescending = verticalDistance < -significantThreshold;
            
            // For ground level, clear air animations when close to target or when arrived
            if (_targetElevation == 0)
            {
                // When at or very close to ground level, clear air animations
                // Also check if we're actually at the target (verticalDistance is very small or positive)
                // Clear falling animation when we've arrived or are very close (within 0.1f)
                if (hasArrived || (Mathf.Abs(verticalDistance) < 0.1f && verticalDistance >= -0.05f))
                {
                    _isJumping = false;
                    _isFalling = false;
                }
                else if (isDescending && _verticalVelocity < -0.1f && Mathf.Abs(verticalDistance) > 0.2f)
                {
                    // Still descending to ground and far enough away
                    _isJumping = false;
                    _isFalling = true;
                }
                else
                {
                    // Not descending or very close - clear air animations
                    _isJumping = false;
                    _isFalling = false;
                }
            }
            else if (isAscending && _verticalVelocity > 0.1f)
            {
                _isJumping = true;
                _isFalling = false;
            }
            else if (isDescending && _verticalVelocity < -0.1f)
            {
                _isJumping = false;
                _isFalling = true;
            }
            else
            {
                _isJumping = false;
                _isFalling = false;
            }

            // Apply movement using CharacterController.Move() - all movement through interpolation
            // No direct position manipulation - let CharacterController handle all movement naturally
            _tempMovement.Set(0f, _verticalVelocity, 0f);
            _tempMovement *= Time.deltaTime;
            _tempMovement += horizontalMovement;
            
            _controller.Move(_tempMovement);
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
    }
}

# Encounter Mode Movement & Animation Plan

## Overview
This document outlines the plan for implementing grid-based character movement in encounter mode with proper animation support. The system will maintain the same animation system used in normal movement mode while adapting to grid-based movement.

## Requirements
1. **Maintain existing animations**: Use the same `AnimationHandler` and animator parameters
2. **Ground movement**: Use sprint animation when moving on the ground
3. **Air movement**: 
   - Use jump animation when ascending (moving upward)
   - Use idle falling animation when descending/falling
4. **Grid-based movement**: Move character to selected grid cell when clicked
5. **Pathfinding**: Calculate path from current position to target cell

## Architecture

### Components to Create

#### 1. `IEncounterMovementHandler` (Interface)
- Follows SOLID principles (Dependency Inversion)
- Defines contract for encounter movement

**Methods:**
- `void SetTargetCell(GridCell targetCell, int elevation)`
- `void ProcessMovement(bool isGrounded, float verticalVelocity)`
- `void CancelMovement()`
- `bool IsMoving { get; }`
- `float CurrentSpeed { get; }`
- `float AnimationBlend { get; }`

#### 2. `EncounterMovementHandler` (Implementation)
- Handles grid-based movement logic
- Calculates path from current position to target
- Manages movement state (idle, moving, jumping, falling)
- Integrates with `CharacterController` for actual movement

**Key Features:**
- Simple pathfinding (straight-line or A* if obstacles needed)
- Handles elevation changes (jumping up, falling down)
- Rotates character to face movement direction
- Uses sprint speed for ground movement
- Tracks movement state for animation updates

#### 3. Integration Points

**PlayerController.cs:**
- When `CurrentMovementMode == MovementMode.Encounter`:
  - Disable normal movement input processing
  - Use `EncounterMovementHandler` instead of `MovementHandler`
  - Still use `JumpHandler` for gravity/vertical velocity
  - Still use `AnimationHandler` for animations
  - Still use `GroundedChecker` for ground detection

**GridSelector.cs:**
- Add event: `System.Action<GridCell, int> OnCellSelected`
- Fire event when cell is selected (in `SelectHoveredCell()`)

**EncounterModeManager.cs:**
- Subscribe to `GridSelector.OnCellSelected` event
- Pass selected cell to `EncounterMovementHandler`

## Movement Logic

### Path Calculation
1. Get current grid cell from player position
2. Calculate path from current cell to target cell
3. For now: Simple straight-line path (can be upgraded to A* later)
4. Handle elevation changes:
   - If target elevation > current elevation: Jump up
   - If target elevation < current elevation: Fall down
   - If same elevation: Move horizontally

### Movement States
- **Idle**: No target cell, character is stationary
- **Moving**: Moving horizontally toward target cell
- **Ascending**: Moving upward (jump animation)
- **Descending**: Moving downward (freefall animation)
- **Arrived**: Reached target cell

### Animation Integration

The existing `AnimationHandler` uses these parameters:
- `Speed` (float): Current movement speed
- `MotionSpeed` (float): Input magnitude (0-1)
- `Grounded` (bool): Is character on ground
- `Jump` (bool): Is character jumping
- `FreeFall` (bool): Is character falling

**Encounter Mode Animation Strategy:**
- **Ground Movement**: 
  - Set `Speed` to sprint speed value
  - Set `MotionSpeed` to 1.0
  - Set `Grounded` to true
  - Set `Jump` to false
  - Set `FreeFall` to false
  
- **Ascending (Jumping Up)**:
  - Set `Speed` to 0 (or small value)
  - Set `MotionSpeed` to 0
  - Set `Grounded` to false
  - Set `Jump` to true
  - Set `FreeFall` to false
  
- **Descending (Falling)**:
  - Set `Speed` to 0 (or small value)
  - Set `MotionSpeed` to 0
  - Set `Grounded` to false
  - Set `Jump` to false
  - Set `FreeFall` to true

- **Idle**:
  - Set `Speed` to 0
  - Set `MotionSpeed` to 0
  - Set `Grounded` based on actual ground check
  - Set `Jump` to false
  - Set `FreeFall` to false

## Implementation Steps

1. ✅ Create `IEncounterMovementHandler` interface
2. ✅ Create `EncounterMovementHandler` class
3. ✅ Add event system to `GridSelector` for cell selection
4. ✅ Integrate encounter movement into `PlayerController`
5. ✅ Update `EncounterModeManager` to coordinate movement
6. ✅ Test animations in encounter mode

## Technical Details

### Grid Cell to World Position
- Use `GridCell.GetPositionAtElevation(cellSize, elevationLevel)` to get target position
- Account for character controller height/radius when positioning

### Movement Speed
- Use `SprintSpeed` from `PlayerController` for ground movement
- Vertical movement uses gravity/jump physics from `JumpHandler`

### Rotation
- Rotate character to face movement direction
- Use smooth rotation similar to `MovementHandler`

### Arrival Detection
- Check if character is within threshold distance of target
- Consider both horizontal and vertical distance
- Account for elevation level

## Future Enhancements
- A* pathfinding for obstacle avoidance
- Animation blending for smoother transitions
- Support for diagonal movement
- Movement speed modifiers (difficult terrain, etc.)


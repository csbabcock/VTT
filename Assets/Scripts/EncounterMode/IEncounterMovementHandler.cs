using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Interface for handling encounter mode grid-based movement.
    /// Follows SOLID principles (Dependency Inversion).
    /// </summary>
    public interface IEncounterMovementHandler
    {
        /// <summary>
        /// Sets the target grid cell to move to.
        /// </summary>
        /// <param name="targetCell">The target grid cell</param>
        /// <param name="elevation">The elevation level at the target (0 = ground)</param>
        void SetTargetCell(GridCell targetCell, int elevation);

        /// <summary>
        /// Processes movement toward the target cell.
        /// Should be called every frame when in encounter mode.
        /// </summary>
        /// <param name="isGrounded">Whether the character is currently grounded</param>
        void ProcessMovement(bool isGrounded);

        /// <summary>
        /// Cancels the current movement and stops the character.
        /// </summary>
        void CancelMovement();

        /// <summary>
        /// Whether the character is currently moving toward a target.
        /// </summary>
        bool IsMoving { get; }

        /// <summary>
        /// Current movement speed (for animation blending).
        /// </summary>
        float CurrentSpeed { get; }

        /// <summary>
        /// Animation blend value (for animation system).
        /// </summary>
        float AnimationBlend { get; }

        /// <summary>
        /// Whether the character is currently jumping (ascending).
        /// </summary>
        bool IsJumping { get; }

        /// <summary>
        /// Whether the character is currently falling (descending).
        /// </summary>
        bool IsFalling { get; }

        /// <summary>
        /// Whether the character should be considered grounded in encounter mode.
        /// This is true when at ground level (elevation 0) and arrived at target.
        /// </summary>
        bool ShouldBeGrounded { get; }
    }
}


using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Interface for handling player movement
    /// </summary>
    public interface IMovementHandler
    {
        void ProcessMovement(Vector2 moveInput, bool isSprinting, bool analogMovement);
        void ApplyMovementWithVerticalVelocity(float verticalVelocity);
        void SetYaw(float yaw);
        float CurrentSpeed { get; }
        float AnimationBlend { get; }
    }
}


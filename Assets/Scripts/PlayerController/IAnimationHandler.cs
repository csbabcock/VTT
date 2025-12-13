namespace GameCore
{
    /// <summary>
    /// Interface for handling animations
    /// </summary>
    public interface IAnimationHandler
    {
        void UpdateAnimations(float speed, float motionSpeed, bool isGrounded, bool isJumping, bool isFalling);
        void Initialize();
    }
}


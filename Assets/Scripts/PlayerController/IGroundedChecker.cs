namespace GameCore
{
    /// <summary>
    /// Interface for checking if the player is grounded
    /// </summary>
    public interface IGroundedChecker
    {
        bool IsGrounded { get; }
        void CheckGrounded();
    }
}


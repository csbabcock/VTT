namespace GameCore
{
    /// <summary>
    /// Interface for handling jump and gravity
    /// </summary>
    public interface IJumpHandler
    {
        float VerticalVelocity { get; }
        void ProcessJump(bool jumpInput, bool isGrounded);
    }
}


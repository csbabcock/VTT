namespace GameCore
{
    /// <summary>
    /// Interface for managing perspective mode
    /// </summary>
    public interface IPerspectiveManager
    {
        PerspectiveMode CurrentPerspective { get; }
        void TogglePerspective();
        void Initialize();
    }
}


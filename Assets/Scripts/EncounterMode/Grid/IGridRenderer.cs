namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Interface for grid rendering functionality
    /// </summary>
    public interface IGridRenderer
    {
        void SetVisible(bool visible);
        void UpdateVisualization();
    }
}


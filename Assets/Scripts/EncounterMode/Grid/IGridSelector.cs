namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Interface for grid cell selection functionality
    /// </summary>
    public interface IGridSelector
    {
        GridCell SelectedCell { get; }
        GridCell HoveredCell { get; }
        int SelectedElevation { get; }
        int MaxElevation { get; }
        void UpdateSelection();
        void ClearSelection();
        void SetMaxElevation(int maxElevation);
    }
}


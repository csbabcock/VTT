namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Represents the current state of a drag operation.
    /// </summary>
    public struct DragState
    {
        public bool IsDragging;
        public int RolledScoreIndex; // Which rolled score is being dragged (-1 if none)
        public int SourceAbilityIndex; // Which ability the drag started from (-1 if from pool)
        public bool IsDraggingFromAbility; // True if dragging from ability, false if from pool
        public int ScoreValue; // The actual score value being dragged

        public static DragState None => new DragState
        {
            IsDragging = false,
            RolledScoreIndex = -1,
            SourceAbilityIndex = -1,
            IsDraggingFromAbility = false,
            ScoreValue = -1
        };
    }
}

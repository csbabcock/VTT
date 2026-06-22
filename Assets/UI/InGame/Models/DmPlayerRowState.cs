namespace GameCore.UI.InGame.Models
{
    /// <summary>Compact player row data for the DM player list.</summary>
    public struct DmPlayerRowState
    {
        public int OwnerId;
        public string DisplayName;
        public int CurrentHp;
        public int MaxHp;
        public string StatusSummary;
        public bool IsSelected;
        public bool IsCurrentTurn;
    }
}

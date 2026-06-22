namespace GameCore.UI.InGame.Models
{
    /// <summary>Compact player row data for the DM player list.</summary>
    public struct DmPlayerRowState
    {
        public int OwnerId;
        public string DisplayName;
        public int CurrentHitPoints;
        public int MaxHitPoints;
        public int TemporaryHitPoints;
        public uint ConditionFlags;
        public int DeathSaveSuccesses;
        public int DeathSaveFailures;
        public bool IsSelected;
    }
}

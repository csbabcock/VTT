namespace GameCore.Combat.ActionEconomy
{
    public interface IActionEconomyTracker
    {
        bool CanSpend(ActionCostKind cost);
        bool TrySpend(ActionCostKind cost);
        void ResetForNewTurn();
    }
}

namespace GameCore.Combat.ActionEconomy
{
    /// <summary>
    /// Tracks per-turn 5e action economy. Pure logic with no Unity dependencies.
    /// </summary>
    public sealed class ActionEconomyTracker : IActionEconomyTracker
    {
        private bool _actionUsed;
        private bool _bonusActionUsed;
        private bool _reactionUsed;

        public bool CanSpend(ActionCostKind cost) => cost switch
        {
            ActionCostKind.None => true,
            ActionCostKind.Action => !_actionUsed,
            ActionCostKind.BonusAction => !_bonusActionUsed,
            ActionCostKind.Reaction => !_reactionUsed,
            _ => false,
        };

        public bool TrySpend(ActionCostKind cost)
        {
            if (!CanSpend(cost))
                return false;

            switch (cost)
            {
                case ActionCostKind.Action:
                    _actionUsed = true;
                    return true;
                case ActionCostKind.BonusAction:
                    _bonusActionUsed = true;
                    return true;
                case ActionCostKind.Reaction:
                    _reactionUsed = true;
                    return true;
                case ActionCostKind.None:
                    return true;
                default:
                    return false;
            }
        }

        public void ResetForNewTurn()
        {
            _actionUsed = false;
            _bonusActionUsed = false;
            _reactionUsed = false;
        }
    }
}

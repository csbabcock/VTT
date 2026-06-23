namespace GameCore.Combat.Targeting
{
    /// <summary>Tracks whether the player is choosing an attack target.</summary>
    public sealed class AttackTargetingSession
    {
        public bool IsActive { get; private set; }
        public IAttackDefinition ActiveAttack { get; private set; }

        public void Begin(IAttackDefinition attack)
        {
            ActiveAttack = attack;
            IsActive = attack != null;
        }

        public void Cancel()
        {
            ActiveAttack = null;
            IsActive = false;
        }
    }
}

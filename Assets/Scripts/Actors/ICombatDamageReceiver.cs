namespace GameCore.Actors
{
    /// <summary>
    /// Optional component for applying validated combat damage from another actor.
    /// Network implementations handle server authority; offline sheets mutate locally.
    /// </summary>
    public interface ICombatDamageReceiver
    {
        void RequestDamageFromAttacker(int amount, int attackerOwnerId);
    }
}

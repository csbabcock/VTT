namespace GameCore.Combat
{
    /// <summary>
    /// Anything that can be targeted and take damage (characters, destructible props, etc.).
    /// </summary>
    public interface IDamageable
    {
        string DisplayName { get; }
        int ArmorClass { get; }
        int CurrentHitPoints { get; }
        int MaxHitPoints { get; }
        bool IsDestroyed { get; }
        void ApplyDamage(int amount);
    }
}

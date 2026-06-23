namespace GameCore.Combat
{
    /// <summary>
    /// Describes an attack option independent of attacker or target.
    /// New attacks (weapons, spells, racial traits) implement this interface.
    /// </summary>
    public interface IAttackDefinition
    {
        string AttackId { get; }
        string DisplayName { get; }

        /// <summary>Weapon name used for ruleset stat lookup (e.g. "Unarmed Strike").</summary>
        string WeaponName { get; }

        AttackRangeKind RangeKind { get; }
        ActionCostKind Cost { get; }
    }
}

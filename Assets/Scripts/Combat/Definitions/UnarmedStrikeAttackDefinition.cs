namespace GameCore.Combat.Definitions
{
    /// <summary>PHB unarmed strike: 1 + STR bludgeoning, melee 5 ft, costs an Action in encounter mode.</summary>
    public sealed class UnarmedStrikeAttackDefinition : IAttackDefinition
    {
        public const string AttackIdValue = "unarmed_strike";
        public const string WeaponNameValue = "Unarmed Strike";

        public static readonly UnarmedStrikeAttackDefinition Instance = new();

        public string AttackId => AttackIdValue;
        public string DisplayName => WeaponNameValue;
        public string WeaponName => WeaponNameValue;
        public AttackRangeKind RangeKind => AttackRangeKind.Melee5Feet;
        public ActionCostKind Cost => ActionCostKind.Action;
    }
}

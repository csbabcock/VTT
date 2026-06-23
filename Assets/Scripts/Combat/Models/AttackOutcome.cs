namespace GameCore.Combat.Models
{
    public enum AttackResultKind
    {
        Miss,
        Hit,
    }

    /// <summary>Outcome of resolving an attack roll against a target AC.</summary>
    public readonly struct AttackOutcome
    {
        public AttackResultKind ResultKind { get; }
        public int DamageAmount { get; }
        public bool IsCritical { get; }
        public int AttackRollNatural { get; }
        public int AttackRollTotal { get; }
        public int TargetArmorClass { get; }

        public bool DidHit => ResultKind == AttackResultKind.Hit;

        public static AttackOutcome Miss(
            int attackRollNatural,
            int attackRollTotal,
            int targetArmorClass)
        {
            return new AttackOutcome(
                AttackResultKind.Miss,
                damageAmount: 0,
                isCritical: false,
                attackRollNatural,
                attackRollTotal,
                targetArmorClass);
        }

        public static AttackOutcome Hit(
            int damageAmount,
            bool isCritical,
            int attackRollNatural,
            int attackRollTotal,
            int targetArmorClass)
        {
            return new AttackOutcome(
                AttackResultKind.Hit,
                damageAmount,
                isCritical,
                attackRollNatural,
                attackRollTotal,
                targetArmorClass);
        }

        private AttackOutcome(
            AttackResultKind resultKind,
            int damageAmount,
            bool isCritical,
            int attackRollNatural,
            int attackRollTotal,
            int targetArmorClass)
        {
            ResultKind = resultKind;
            DamageAmount = damageAmount;
            IsCritical = isCritical;
            AttackRollNatural = attackRollNatural;
            AttackRollTotal = attackRollTotal;
            TargetArmorClass = targetArmorClass;
        }
    }
}

namespace GameCore.Combat.Models
{
    /// <summary>Full result of attempting a combat action.</summary>
    public readonly struct CombatActionResult
    {
        public bool Succeeded { get; }
        public CombatFailureReason FailureReason { get; }
        public AttackOutcome AttackOutcome { get; }
        public string AttackerName { get; }
        public string TargetName { get; }
        public string AttackDisplayName { get; }

        public static CombatActionResult Failed(
            CombatFailureReason reason,
            string attackerName,
            string targetName,
            string attackDisplayName)
        {
            return new CombatActionResult(
                succeeded: false,
                reason,
                default,
                attackerName,
                targetName,
                attackDisplayName);
        }

        public static CombatActionResult Completed(
            AttackOutcome outcome,
            string attackerName,
            string targetName,
            string attackDisplayName)
        {
            return new CombatActionResult(
                succeeded: true,
                CombatFailureReason.TargetDestroyed,
                outcome,
                attackerName,
                targetName,
                attackDisplayName);
        }

        private CombatActionResult(
            bool succeeded,
            CombatFailureReason failureReason,
            AttackOutcome attackOutcome,
            string attackerName,
            string targetName,
            string attackDisplayName)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            AttackOutcome = attackOutcome;
            AttackerName = attackerName ?? string.Empty;
            TargetName = targetName ?? string.Empty;
            AttackDisplayName = attackDisplayName ?? string.Empty;
        }
    }
}

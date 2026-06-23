namespace GameCore.Combat.Models
{
    public enum CombatFailureReason
    {
        TargetDestroyed,
        NotYourTurn,
        ActionAlreadyUsed,
        UnknownAttack,
        OutOfRange,
        SelfTarget,
        InvalidTarget,
        NoPermissionToApplyDamage,
    }
}

namespace GameCore.PlayerData
{
    /// <summary>Validation and clamping rules for <see cref="CharacterCombatState"/> fields.</summary>
    public static class CharacterCombatStateRules
    {
        public const int MaxDeathSaveCount = 3;
        public const int MaxExhaustionLevel = 6;

        public static int ClampDeathSaveCount(int value)
        {
            if (value < 0)
                return 0;
            return value > MaxDeathSaveCount ? MaxDeathSaveCount : value;
        }

        public static byte ClampExhaustion(int value)
        {
            if (value < 0)
                return 0;
            return (byte)(value > MaxExhaustionLevel ? MaxExhaustionLevel : value);
        }

        public static int ClampTemporaryHitPoints(int value) => value < 0 ? 0 : value;
    }
}

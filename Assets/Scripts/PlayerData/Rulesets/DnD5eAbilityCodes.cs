namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Maps D&amp;D 5e ability abbreviations to the canonical STR..CHA array index order used in character data.
    /// </summary>
    public static class DnD5eAbilityCodes
    {
        public const int StrIndex = 0;
        public const int DexIndex = 1;
        public const int ConIndex = 2;
        public const int IntIndex = 3;
        public const int WisIndex = 4;
        public const int ChaIndex = 5;

        /// <summary>Returns -1 when <paramref name="code"/> is null, empty, or not STR/DEX/CON/INT/WIS/CHA.</summary>
        public static int IndexFromCode(string code)
        {
            return TryIndexFromCode(code, out int index) ? index : -1;
        }

        public static bool TryIndexFromCode(string code, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(code))
                return false;
            switch (code.Trim().ToUpperInvariant())
            {
                case "STR":
                    index = StrIndex;
                    return true;
                case "DEX":
                    index = DexIndex;
                    return true;
                case "CON":
                    index = ConIndex;
                    return true;
                case "INT":
                    index = IntIndex;
                    return true;
                case "WIS":
                    index = WisIndex;
                    return true;
                case "CHA":
                    index = ChaIndex;
                    return true;
                default:
                    return false;
            }
        }
    }
}

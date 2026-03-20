using System.Text.RegularExpressions;
using GameCore.PlayerData.Rulesets;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Replaces PHB-style ability modifier phrases with the character's current modifier,
    /// styled for UI Toolkit rich text and abbrev tag, e.g. 10 + 2 (DEX) -1 (CON).
    /// Normalizes "10 + +2" → "10 + 2" and " + -1" → " -1" without breaking tags.
    /// </summary>
    public static class AbilityModifierTextInterpolator
    {
        private static readonly string[] AbilityAbbrev = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

        private static readonly Regex AbilityModifierPhrase = new Regex(
            @"\b(?:your\s+)?(Strength|Dexterity|Constitution|Intelligence|Wisdom|Charisma)\s+modifier\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex PlusPlus = new Regex(@"\+\s*\+", RegexOptions.Compiled);
        /// <summary>Turns " + -1" into " -1" for plain numeric negatives.</summary>
        private static readonly Regex PlusThenNegative = new Regex(@"\+\s*(-\d+)", RegexOptions.Compiled);
        /// <summary>
        /// Rich-text negatives use &lt;b&gt;&lt;color=...&gt;-N&lt;/color&gt;&lt;/b&gt;; strip a preceding +
        /// so we never show "+ -1" when the mod is negative.
        /// </summary>
        private static readonly Regex PlusBeforeNegativeRichModifier = new Regex(
            @"\+\s*(?=<b><color=[^>]+>-\d+</color></b>)",
            RegexOptions.Compiled);

        private static readonly Regex YourAcEquals = new Regex(
            @"\byour\s+AC\s+equals\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Same as <see cref="InterpolateWithMeta"/> but drops the substitution flag.
        /// </summary>
        public static string Interpolate(string text, int[] abilityScores, IRulesetCalculator calculator)
        {
            return InterpolateWithMeta(text, abilityScores, calculator).Text;
        }

        /// <summary>
        /// Expands ability modifier phrases. <see cref="InterpolationResult.Substituted"/> is true if
        /// at least one assigned ability's phrase was replaced with a live value.
        /// </summary>
        public static InterpolationResult InterpolateWithMeta(
            string text,
            int[] abilityScores,
            IRulesetCalculator calculator)
        {
            if (string.IsNullOrEmpty(text) || calculator == null)
                return new InterpolationResult(text, false);
            if (abilityScores == null || abilityScores.Length < 6)
                return new InterpolationResult(text, false);

            bool substituted = false;
            string result = AbilityModifierPhrase.Replace(text, m =>
            {
                int idx = AbilityNameToIndex(m.Groups[1].Value);
                if (idx < 0 || abilityScores[idx] < 0)
                    return m.Value;
                substituted = true;
                int mod = calculator.CalculateAbilityModifier(abilityScores[idx]);
                return FormatRichModifier(mod, AbilityAbbrev[idx]);
            });

            result = NormalizeSignedModifierArithmetic(result);
            result = YourAcEquals.Replace(result, "AC equals");
            return new InterpolationResult(result, substituted);
        }

        /// <summary>Rich-text: value without a leading + (the line already uses + from rules text), then (DEX).</summary>
        private static string FormatRichModifier(int mod, string abbrev)
        {
            string numStr = mod.ToString();
            string numColor = mod > 0 ? "#4CF490" : mod < 0 ? "#FF4C4C" : "#CED3E0";
            string modRich = $"<b><color={numColor}>{numStr}</color></b>";
            string hintRich = $"<size=12><color=#B6C0CF> ({abbrev})</color></size>";
            return modRich + hintRich;
        }

        private static string NormalizeSignedModifierArithmetic(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            text = PlusPlus.Replace(text, "+ ");
            text = PlusBeforeNegativeRichModifier.Replace(text, " ");
            text = PlusThenNegative.Replace(text, " $1");
            return text;
        }

        private static int AbilityNameToIndex(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;
            switch (name.ToLowerInvariant())
            {
                case "strength": return 0;
                case "dexterity": return 1;
                case "constitution": return 2;
                case "intelligence": return 3;
                case "wisdom": return 4;
                case "charisma": return 5;
                default: return -1;
            }
        }

        public readonly struct InterpolationResult
        {
            public string Text { get; }
            /// <summary>True when at least one phrase became a live modifier + hint.</summary>
            public bool Substituted { get; }

            public InterpolationResult(string text, bool substituted)
            {
                Text = text ?? string.Empty;
                Substituted = substituted;
            }
        }
    }
}

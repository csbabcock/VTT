using System.Collections.Generic;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Factory for creating ruleset calculators.
    /// Follows Factory Pattern and Dependency Inversion Principle.
    /// Allows easy addition of new rulesets without modifying existing code (Open/Closed Principle).
    /// </summary>
    public static class RulesetCalculatorFactory
    {
        private static readonly Dictionary<string, IRulesetCalculator> _calculators = new Dictionary<string, IRulesetCalculator>();

        static RulesetCalculatorFactory()
        {
            // Register default calculators
            RegisterCalculator(new DnD5eRulesetCalculator());
        }

        /// <summary>
        /// Registers a ruleset calculator.
        /// </summary>
        public static void RegisterCalculator(IRulesetCalculator calculator)
        {
            if (calculator != null)
            {
                _calculators[calculator.RulesetId] = calculator;
            }
        }

        /// <summary>
        /// Gets a calculator for the specified ruleset.
        /// </summary>
        /// <param name="rulesetId">Ruleset identifier (e.g., "DnD5e")</param>
        /// <returns>The calculator, or null if not found</returns>
        public static IRulesetCalculator GetCalculator(string rulesetId)
        {
            if (string.IsNullOrEmpty(rulesetId))
                return _calculators["DnD5e"]; // Default to D&D 5e

            return _calculators.TryGetValue(rulesetId, out var calculator) ? calculator : null;
        }

        /// <summary>
        /// Gets the default calculator (D&D 5e).
        /// </summary>
        public static IRulesetCalculator GetDefaultCalculator()
        {
            return GetCalculator("DnD5e");
        }

        /// <summary>
        /// Gets all registered ruleset IDs.
        /// </summary>
        public static IEnumerable<string> GetAvailableRulesets()
        {
            return _calculators.Keys;
        }
    }
}


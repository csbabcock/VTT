using System.Collections.Generic;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Factory for creating character data adapters.
    /// Follows Factory Pattern and Dependency Inversion Principle.
    /// </summary>
    public static class RulesetAdapterFactory
    {
        private static readonly Dictionary<string, ICharacterDataAdapter> _adapters = new Dictionary<string, ICharacterDataAdapter>();

        static RulesetAdapterFactory()
        {
            // Register default adapters
            RegisterAdapter(new DnD5eCharacterDataAdapter());
        }

        /// <summary>
        /// Registers a character data adapter.
        /// </summary>
        public static void RegisterAdapter(ICharacterDataAdapter adapter)
        {
            if (adapter != null)
            {
                _adapters[adapter.RulesetId] = adapter;
            }
        }

        /// <summary>
        /// Gets an adapter for the specified ruleset.
        /// </summary>
        /// <param name="rulesetId">Ruleset identifier (e.g., "DnD5e")</param>
        /// <returns>The adapter, or null if not found</returns>
        public static ICharacterDataAdapter GetAdapter(string rulesetId)
        {
            if (string.IsNullOrEmpty(rulesetId))
                return _adapters["DnD5e"]; // Default to D&D 5e

            return _adapters.TryGetValue(rulesetId, out var adapter) ? adapter : null;
        }

        /// <summary>
        /// Gets the default adapter (D&D 5e).
        /// </summary>
        public static ICharacterDataAdapter GetDefaultAdapter()
        {
            return GetAdapter("DnD5e");
        }
    }
}


using System;
using System.Collections.Generic;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Caches <see cref="RulesetContentService"/> instances per ruleset id so calculators and UI share one loaded copy.
    /// </summary>
    public static class RulesetContentQueryProvider
    {
        private static readonly Dictionary<string, IRulesetContentQuery> Cache = new Dictionary<string, IRulesetContentQuery>(StringComparer.Ordinal);

        public static IRulesetContentQuery GetOrCreate(string rulesetId)
        {
            if (string.IsNullOrEmpty(rulesetId))
                rulesetId = "DnD5e";

            lock (Cache)
            {
                if (!Cache.TryGetValue(rulesetId, out IRulesetContentQuery query))
                {
                    query = new RulesetContentService(rulesetId);
                    Cache[rulesetId] = query;
                }

                return query;
            }
        }
    }
}

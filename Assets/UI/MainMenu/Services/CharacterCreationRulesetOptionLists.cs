using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.PlayerData.Rulesets;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Builds sorted (id, displayName) option lists for character-creation dropdowns from ruleset content.
    /// </summary>
    public static class CharacterCreationRulesetOptionLists
    {
        /// <summary>
        /// Race, class, and background lists sorted by display name for stable UI ordering.
        /// </summary>
        public static (
            List<(string id, string displayName)> classes,
            List<(string id, string displayName)> races,
            List<(string id, string displayName)> backgrounds)
            CreateSortedRaceClassBackground(IRulesetContentQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            List<(string, string)> classes = BuildSortedOptions(
                query.GetClasses(),
                c => c.id,
                c => c.name);
            List<(string, string)> races = BuildSortedOptions(
                query.GetRaces(),
                r => r.id,
                r => r.name);
            List<(string, string)> backgrounds = BuildSortedOptions(
                query.GetBackgrounds(),
                b => b.id,
                b => b.name);

            return (classes, races, backgrounds);
        }

        private static List<(string id, string displayName)> BuildSortedOptions<T>(
            IEnumerable<T> items,
            Func<T, string> getId,
            Func<T, string> getName)
        {
            return items
                .Where(x => !string.IsNullOrEmpty(getId(x)))
                .OrderBy(x => getName(x) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(x =>
                {
                    string id = getId(x);
                    string name = getName(x);
                    return (id, string.IsNullOrEmpty(name) ? id : name);
                })
                .ToList();
        }
    }
}

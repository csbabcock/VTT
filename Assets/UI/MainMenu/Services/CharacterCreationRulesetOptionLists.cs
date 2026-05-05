using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;

namespace GameCore.UI.MainMenu.Services
{
    public class RaceOptionData
    {
        public string Id { get; }
        public string DisplayName { get; }
        public bool IsGroupOnly { get; }
        public IReadOnlyList<RaceOptionData> Children { get; }

        public RaceOptionData(
            string id,
            string displayName,
            bool isGroupOnly = false,
            IReadOnlyList<RaceOptionData> children = null)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IsGroupOnly = isGroupOnly;
            Children = children ?? Array.Empty<RaceOptionData>();
        }
    }

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
            List<RaceOptionData> races,
            List<(string id, string displayName)> backgrounds)
            CreateSortedRaceClassBackground(IRulesetContentQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            List<(string, string)> classes = BuildSortedOptions(
                query.GetClasses(),
                c => c.id,
                c => c.name);
            List<RaceOptionData> races = BuildGroupedRaceOptions(query.GetRaces());
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

        private static List<RaceOptionData> BuildGroupedRaceOptions(
            IEnumerable<RaceDefinition> races)
        {
            var result = new List<RaceOptionData>();
            if (races == null)
                return result;

            List<RaceDefinition> valid = races
                .Where(r => r != null && !string.IsNullOrEmpty(r.id))
                .ToList();

            var byId = valid.ToDictionary(r => r.id, StringComparer.OrdinalIgnoreCase);
            var childrenByParent = valid
                .Where(r => !string.IsNullOrEmpty(r.parentId))
                .GroupBy(r => r.parentId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (RaceDefinition race in valid
                         .Where(r => string.IsNullOrEmpty(r.parentId))
                         .OrderBy(r => r.sortName ?? r.name ?? r.id, StringComparer.OrdinalIgnoreCase))
            {
                if (childrenByParent.TryGetValue(race.id, out List<RaceDefinition> children) && children.Count > 0)
                {
                    List<RaceOptionData> childOptions = children
                        .OrderBy(r => r.sortName ?? r.name ?? r.id, StringComparer.OrdinalIgnoreCase)
                        .Select(child => new RaceOptionData(
                            child.id,
                            string.IsNullOrEmpty(child.name) ? child.id : child.name))
                        .ToList();
                    result.Add(new RaceOptionData(
                        race.id,
                        string.IsNullOrEmpty(race.name) ? race.id : race.name,
                        true,
                        childOptions));
                }
                else
                {
                    result.Add(new RaceOptionData(
                        race.id,
                        string.IsNullOrEmpty(race.name) ? race.id : race.name,
                        race.isGroupOnly));
                }
            }

            foreach (RaceDefinition orphan in valid
                         .Where(r => !string.IsNullOrEmpty(r.parentId) && !byId.ContainsKey(r.parentId))
                         .OrderBy(r => r.sortName ?? r.name ?? r.id, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(new RaceOptionData(
                    orphan.id,
                    string.IsNullOrEmpty(orphan.name) ? orphan.id : orphan.name));
            }

            return result;
        }
    }
}

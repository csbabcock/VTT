using System.Collections.Generic;
using System.Linq;
using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Backward-compatible façade over <see cref="IRulesetContentQuery"/> for character creation display strings.
    /// Prefer injecting <see cref="IRulesetContentQuery"/> into presenters.
    /// </summary>
    public static class CharacterCreationDataService
    {
        private const string DefaultRulesetId = "DnD5e";

        private static IRulesetContentQuery Query => RulesetContentQueryProvider.GetOrCreate(DefaultRulesetId);

        public static string[] AvailableClasses =>
            Query.GetClasses().Select(c => c.name).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n).ToArray();

        public static string[] AvailableRaces =>
            Query.GetRaces().Select(r => r.name).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n).ToArray();

        public static string[] AvailableBackgrounds =>
            Query.GetBackgrounds().Select(b => b.name).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n).ToArray();

        public static string GetRaceDescription(string raceId)
        {
            if (string.IsNullOrEmpty(raceId) || !Query.TryGetRace(raceId, out RaceDefinition race))
                return string.Empty;
            return race.description ?? string.Empty;
        }

        public static string GetClassDescription(string classId)
        {
            if (string.IsNullOrEmpty(classId) || !Query.TryGetClass(classId, out ClassDefinition c))
                return string.Empty;
            return c.description ?? string.Empty;
        }

        public static string GetBackgroundDescription(string backgroundId)
        {
            if (string.IsNullOrEmpty(backgroundId) || !Query.TryGetBackground(backgroundId, out BackgroundDefinition b))
                return string.Empty;
            return b.description ?? string.Empty;
        }

        public static List<FeatureData> GetRaceFeatures(string raceId)
        {
            var features = new List<FeatureData>();
            if (string.IsNullOrEmpty(raceId) || !Query.TryGetRace(raceId, out RaceDefinition race) ||
                race.features == null)
                return features;
            foreach (FeatureDefinition feat in race.features)
            {
                if (feat != null)
                    features.Add(new FeatureData(feat.name, feat.description));
            }

            return features;
        }

        public static List<FeatureData> GetBackgroundFeatures(string backgroundId)
        {
            var features = new List<FeatureData>();
            if (string.IsNullOrEmpty(backgroundId) ||
                !Query.TryGetBackground(backgroundId, out BackgroundDefinition background) || background.features == null)
                return features;
            foreach (FeatureDefinition feat in background.features)
            {
                if (feat != null)
                    features.Add(new FeatureData(feat.name, feat.description));
            }

            return features;
        }
    }

    /// <summary>
    /// Data structure for character features.
    /// </summary>
    public class FeatureData
    {
        public string Name { get; }
        public string Description { get; }
        /// <summary>True when description includes live ability modifier substitution (rich text).</summary>
        public bool HasLiveAbilityHints { get; }

        public FeatureData(string name, string description, bool hasLiveAbilityHints = false)
        {
            Name = name;
            Description = description;
            HasLiveAbilityHints = hasLiveAbilityHints;
        }
    }
}

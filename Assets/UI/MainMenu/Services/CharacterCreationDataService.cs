using System.Collections.Generic;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Service providing character creation data (classes, races, backgrounds).
    /// This now acts as a lightweight adapter over JSON-backed ruleset content.
    /// </summary>
    public static class CharacterCreationDataService
    {
        private const string DefaultRulesetId = "DnD5e";
        
        private static PlayerData.Rulesets.RulesetContentService _contentService;

        private static PlayerData.Rulesets.RulesetContentService ContentService
        {
            get
            {
                if (_contentService == null)
                {
                    _contentService = new PlayerData.Rulesets.RulesetContentService(DefaultRulesetId);
                }

                return _contentService;
            }
        }

        public static string[] AvailableClasses
        {
            get
            {
                var classes = ContentService.GetAvailableClasses();
                var names = new List<string>();
                foreach (var c in classes)
                {
                    if (!string.IsNullOrEmpty(c.name))
                    {
                        names.Add(c.name);
                    }
                }
                return names.ToArray();
            }
        }

        public static string[] AvailableRaces
        {
            get
            {
                var races = ContentService.GetAvailableRaces();
                var names = new List<string>();
                foreach (var r in races)
                {
                    if (!string.IsNullOrEmpty(r.name))
                    {
                        names.Add(r.name);
                    }
                }
                return names.ToArray();
            }
        }

        public static string[] AvailableBackgrounds
        {
            get
            {
                var backgrounds = ContentService.GetAvailableBackgrounds();
                var names = new List<string>();
                foreach (var b in backgrounds)
                {
                    if (!string.IsNullOrEmpty(b.name))
                    {
                        names.Add(b.name);
                    }
                }
                return names.ToArray();
            }
        }

        /// <summary>
        /// Gets description for a race by display name.
        /// </summary>
        public static string GetRaceDescription(string raceName)
        {
            if (string.IsNullOrEmpty(raceName))
                return string.Empty;

            foreach (var race in ContentService.GetAvailableRaces())
            {
                if (race.name == raceName)
                {
                    return race.description ?? $"Description for {raceName}.";
                }
            }

            return $"Description for {raceName}.";
        }

        /// <summary>
        /// Gets description for a class by display name.
        /// </summary>
        public static string GetClassDescription(string className)
        {
            if (string.IsNullOrEmpty(className))
                return string.Empty;

            foreach (var c in ContentService.GetAvailableClasses())
            {
                if (c.name == className)
                {
                    return c.description ?? $"Description for {className}.";
                }
            }

            return $"Description for {className}.";
        }

        /// <summary>
        /// Gets features for a race by display name.
        /// </summary>
        public static List<FeatureData> GetRaceFeatures(string raceName)
        {
            var features = new List<FeatureData>();
            if (string.IsNullOrEmpty(raceName))
                return features;

            foreach (var race in ContentService.GetAvailableRaces())
            {
                if (race.name == raceName && race.features != null)
                {
                    foreach (var feat in race.features)
                    {
                        features.Add(new FeatureData(feat.name, feat.description));
                    }
                    break;
                }
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

        public FeatureData(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}

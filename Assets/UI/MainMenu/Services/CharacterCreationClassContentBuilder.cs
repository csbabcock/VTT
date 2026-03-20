using System.Collections.Generic;
using System.Linq;
using GameCore.PlayerData.Rulesets.Definitions;
using GameCore.UI.MainMenu;
using UnityEngine;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Builds class-related content for the character creation UI from <see cref="ClassDefinition"/> (SRP: data → presentation DTOs).
    /// </summary>
    public static class CharacterCreationClassContentBuilder
    {
        private const string QuickBuildHeading = "Quick Build";

        public static string ClassFeaturesSectionHeading(int characterLevel)
        {
            int lv = Mathf.Clamp(characterLevel, CharacterCreationModel.MinCharacterLevel,
                CharacterCreationModel.MaxCharacterLevel);
            return $"Class features (levels 1–{lv})";
        }

        /// <summary>
        /// Converts JSON <see cref="ClassDescriptionSection"/> entries into UI sections (same order as data).
        /// </summary>
        public static List<CharacterDetailSection> BuildStructuredDescription(ClassDefinition cls)
        {
            if (cls?.descriptionSections == null || cls.descriptionSections.Count == 0)
                return null;

            var list = new List<CharacterDetailSection>(cls.descriptionSections.Count);
            foreach (ClassDescriptionSection sec in cls.descriptionSections)
            {
                if (sec == null || string.IsNullOrEmpty(sec.heading) && string.IsNullOrEmpty(sec.body))
                    continue;
                list.Add(new CharacterDetailSection(sec.heading, sec.body));
            }

            return list.Count > 0 ? list : null;
        }

        /// <summary>
        /// Features at or below <paramref name="characterLevel"/> ordered by level then document order.
        /// </summary>
        public static List<FeatureData> BuildFeaturesThroughLevel(ClassDefinition cls, int characterLevel)
        {
            var list = new List<FeatureData>();
            if (cls?.featuresByLevel == null)
                return list;

            int cap = Mathf.Clamp(characterLevel, CharacterCreationModel.MinCharacterLevel,
                CharacterCreationModel.MaxCharacterLevel);

            foreach (ClassFeatureByLevelDefinition fl in cls.featuresByLevel.OrderBy(x => x.level))
            {
                if (fl?.feature == null || fl.level > cap)
                    continue;
                string title = $"{fl.level}. {fl.feature.name}";
                list.Add(new FeatureData(title, fl.feature.description));
            }

            return list;
        }

        public static bool IsQuickBuildHeading(string heading) =>
            !string.IsNullOrEmpty(heading) &&
            heading.Equals(QuickBuildHeading, System.StringComparison.OrdinalIgnoreCase);
    }
}

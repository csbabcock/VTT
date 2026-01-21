using System.Collections.Generic;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Service providing character creation data (classes, races, backgrounds).
    /// Follows Single Responsibility Principle - only provides static data definitions.
    /// </summary>
    public static class CharacterCreationDataService
    {
        public static readonly string[] AvailableClasses = new[]
        {
            "Cleric", "Fighter", "Wizard", "Rogue", "Barbarian", 
            "Ranger", "Bard", "Paladin", "Druid"
        };

        public static readonly string[] AvailableRaces = new[]
        {
            "Hill Dwarf", "High Elf", "Human", "Dragonborn", "Half-Orc", 
            "Tiefling", "Halfling", "Gnome", "Half-Elf"
        };

        public static readonly string[] AvailableBackgrounds = new[]
        {
            "Acolyte", "Soldier", "Criminal", "Folk Hero", "Noble", "Sage"
        };

        /// <summary>
        /// Gets description for a race.
        /// </summary>
        public static string GetRaceDescription(string raceName)
        {
            return _raceDescriptions.TryGetValue(raceName, out var description) 
                ? description 
                : $"Description for {raceName}.";
        }

        /// <summary>
        /// Gets description for a class.
        /// </summary>
        public static string GetClassDescription(string className)
        {
            return _classDescriptions.TryGetValue(className, out var description) 
                ? description 
                : $"Description for {className}.";
        }

        /// <summary>
        /// Gets features for a race.
        /// </summary>
        public static List<FeatureData> GetRaceFeatures(string raceName)
        {
            return _raceFeatures.TryGetValue(raceName, out var features) 
                ? features 
                : new List<FeatureData>();
        }

        private static readonly Dictionary<string, string> _raceDescriptions = new()
        {
            {
                "Hill Dwarf",
                "Hill dwarves are known for their keen senses, deep intuition, and remarkable resilience. " +
                "Hardy and dependable, they have adapted to life in rugged mountainous terrain, developing " +
                "exceptional fortitude and wisdom through generations of living in harmony with stone and earth."
            }
        };

        private static readonly Dictionary<string, string> _classDescriptions = new()
        {
            // Can be expanded with actual class descriptions
        };

        private static readonly Dictionary<string, List<FeatureData>> _raceFeatures = new()
        {
            {
                "Hill Dwarf",
                new List<FeatureData>
                {
                    new FeatureData(
                        "Dwarven Resilience",
                        "You have advantage on saving throws against poison, and you have resistance against poison damage."
                    ),
                    new FeatureData(
                        "Dwarven Toughness",
                        "Your hit point maximum increases by 1, and it increases by 1 every time you gain a level."
                    ),
                    new FeatureData(
                        "Stonecunning",
                        "Whenever you make an Intelligence (History) check related to the origin of stonework, " +
                        "you are considered proficient in the History skill and add double your proficiency bonus to the check."
                    )
                }
            }
        };
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

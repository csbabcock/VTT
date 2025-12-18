using System.Collections.Generic;

namespace GameCore.UI.InGame.Models
{
    /// <summary>
    /// Represents the result of a dice roll with breakdown information.
    /// Follows Single Responsibility Principle by only holding roll data.
    /// </summary>
    public struct RollResult
    {
        /// <summary>
        /// The name of the character who made the roll.
        /// </summary>
        public string CharacterName { get; set; }

        /// <summary>
        /// The type of roll (e.g., "Strength Check", "Athletics", "Longsword Attack").
        /// </summary>
        public string RollType { get; set; }

        /// <summary>
        /// Individual die results (e.g., [15, 3] for a d20 roll of 15 with +3 modifier).
        /// </summary>
        public List<int> DieResults { get; set; }

        /// <summary>
        /// The modifier added to the roll (e.g., +3 from Strength).
        /// </summary>
        public int Modifier { get; set; }

        /// <summary>
        /// Additional modifiers (e.g., proficiency bonus, magic item bonuses).
        /// </summary>
        public List<ModifierBreakdown> ModifierBreakdowns { get; set; }

        /// <summary>
        /// The total result of the roll.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// The die type used (e.g., 20 for d20, 8 for d8).
        /// </summary>
        public int DieType { get; set; }

        /// <summary>
        /// The number of dice rolled (e.g., 1 for d20, 2 for 2d6).
        /// </summary>
        public int NumberOfDice { get; set; }

        /// <summary>
        /// Whether this roll was a critical hit (natural 20) or critical miss (natural 1).
        /// </summary>
        public bool IsCritical { get; set; }

        /// <summary>
        /// Whether this roll was a critical miss (natural 1).
        /// </summary>
        public bool IsCriticalMiss { get; set; }
    }

    /// <summary>
    /// Represents a breakdown of a modifier source.
    /// </summary>
    public struct ModifierBreakdown
    {
        /// <summary>
        /// The source of the modifier (e.g., "Strength", "Proficiency", "Magic Item").
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The value of the modifier.
        /// </summary>
        public int Value { get; set; }
    }
}


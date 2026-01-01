namespace GameCore.PlayerData
{
    /// <summary>
    /// Enumeration of all D&D 5e skills.
    /// Using enum instead of strings prevents typos and makes code type-safe.
    /// </summary>
    public enum DnD5eSkill
    {
        Acrobatics,
        AnimalHandling,
        Arcana,
        Athletics,
        Deception,
        History,
        Insight,
        Intimidation,
        Investigation,
        Medicine,
        Nature,
        Perception,
        Performance,
        Persuasion,
        Religion,
        SleightOfHand,
        Stealth,
        Survival
    }

    /// <summary>
    /// Extension methods for DnD5eSkill enum.
    /// </summary>
    public static class DnD5eSkillExtensions
    {
        /// <summary>
        /// Gets the display name of the skill (with proper spacing).
        /// </summary>
        public static string GetDisplayName(this DnD5eSkill skill)
        {
            return skill switch
            {
                DnD5eSkill.AnimalHandling => "Animal Handling",
                DnD5eSkill.SleightOfHand => "Sleight of Hand",
                _ => skill.ToString()
            };
        }

        /// <summary>
        /// Gets the ability score associated with this skill.
        /// </summary>
        public static string GetAbilityScore(this DnD5eSkill skill)
        {
            return skill switch
            {
                DnD5eSkill.Acrobatics => "DEX",
                DnD5eSkill.AnimalHandling => "WIS",
                DnD5eSkill.Arcana => "INT",
                DnD5eSkill.Athletics => "STR",
                DnD5eSkill.Deception => "CHA",
                DnD5eSkill.History => "INT",
                DnD5eSkill.Insight => "WIS",
                DnD5eSkill.Intimidation => "CHA",
                DnD5eSkill.Investigation => "INT",
                DnD5eSkill.Medicine => "WIS",
                DnD5eSkill.Nature => "INT",
                DnD5eSkill.Perception => "WIS",
                DnD5eSkill.Performance => "CHA",
                DnD5eSkill.Persuasion => "CHA",
                DnD5eSkill.Religion => "INT",
                DnD5eSkill.SleightOfHand => "DEX",
                DnD5eSkill.Stealth => "DEX",
                DnD5eSkill.Survival => "WIS",
                _ => "STR"
            };
        }

        /// <summary>
        /// Converts a skill name string to enum (for backward compatibility).
        /// </summary>
        public static DnD5eSkill? FromString(string skillName)
        {
            if (string.IsNullOrEmpty(skillName))
                return null;

            // Normalize the string
            string normalized = skillName.Replace(" ", "").Replace("-", "");
            
            if (System.Enum.TryParse<DnD5eSkill>(normalized, true, out var skill))
            {
                return skill;
            }

            // Handle special cases
            return skillName.ToLower() switch
            {
                "animal handling" => DnD5eSkill.AnimalHandling,
                "sleight of hand" => DnD5eSkill.SleightOfHand,
                _ => null
            };
        }
    }
}


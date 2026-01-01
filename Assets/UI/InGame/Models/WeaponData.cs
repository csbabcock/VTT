namespace GameCore.UI.InGame.Models
{
    /// <summary>
    /// Represents weapon data including attack bonus and damage properties.
    /// Follows Single Responsibility Principle by only holding weapon data.
    /// </summary>
    public class WeaponData
    {
        public string WeaponName { get; set; }
        public int AttackBonus { get; set; }
        public int DamageDice { get; set; }
        public int DamageDieType { get; set; }
        public int DamageModifier { get; set; }

        /// <summary>
        /// Gets weapon data for a character's equipped weapons (legacy method).
        /// Uses simplified calculation for backward compatibility.
        /// NOTE: This is a fallback method. Prefer using ruleset calculator through adapter.
        /// </summary>
        [System.Obsolete("Use ruleset calculator through ICharacterDataAdapter.GetWeaponData() instead")]
        public static WeaponData GetWeaponData(string weaponName, CharacterData characterData)
        {
            // Simplified calculation for legacy support
            // This doesn't properly check weapon proficiency or handle all weapon types
            return weaponName switch
            {
                "Longsword" => new WeaponData
                {
                    WeaponName = "Longsword",
                    AttackBonus = characterData.GetAbilityModifier("STR") + characterData.ProficiencyBonus,
                    DamageDice = 1,
                    DamageDieType = 8,
                    DamageModifier = characterData.GetAbilityModifier("STR")
                },
                "Shortbow" => new WeaponData
                {
                    WeaponName = "Shortbow",
                    AttackBonus = characterData.GetAbilityModifier("DEX") + characterData.ProficiencyBonus,
                    DamageDice = 1,
                    DamageDieType = 6,
                    DamageModifier = characterData.GetAbilityModifier("DEX")
                },
                _ => new WeaponData
                {
                    WeaponName = weaponName,
                    AttackBonus = 0,
                    DamageDice = 1,
                    DamageDieType = 4,
                    DamageModifier = 0
                }
            };
        }

        /// <summary>
        /// Gets weapon data using D&D 5e character data (preferred method).
        /// Properly checks weapon proficiency and uses correct ability modifiers.
        /// </summary>
        public static WeaponData GetWeaponData(string weaponName, GameCore.PlayerData.DnD5eCharacterData characterData)
        {
            return GameCore.PlayerData.DnD5eWeaponCalculator.CalculateWeaponData(weaponName, characterData);
        }
    }
}


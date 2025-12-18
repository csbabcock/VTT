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
        /// Gets weapon data for a character's equipped weapons.
        /// In a full implementation, this would query the character's inventory/equipment.
        /// </summary>
        public static WeaponData GetWeaponData(string weaponName, CharacterData characterData)
        {
            // Calculate attack bonus and damage modifier based on weapon type and character stats
            return weaponName switch
            {
                "Longsword" => new WeaponData
                {
                    WeaponName = "Longsword",
                    AttackBonus = characterData.GetAbilityModifier("STR") + characterData.ProficiencyBonus, // +3 STR + 2 proficiency
                    DamageDice = 1,
                    DamageDieType = 8,
                    DamageModifier = characterData.GetAbilityModifier("STR") // +3 STR
                },
                "Shortbow" => new WeaponData
                {
                    WeaponName = "Shortbow",
                    AttackBonus = characterData.GetAbilityModifier("DEX") + characterData.ProficiencyBonus, // +2 DEX + 2 proficiency
                    DamageDice = 1,
                    DamageDieType = 6,
                    DamageModifier = characterData.GetAbilityModifier("DEX") // +2 DEX
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
    }
}


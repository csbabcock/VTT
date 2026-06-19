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
        /// Gets weapon data using D&D 5e character data (preferred method).
        /// Properly checks weapon proficiency and uses correct ability modifiers.
        /// </summary>
        public static WeaponData GetWeaponData(string weaponName, GameCore.PlayerData.DnD5eCharacterData characterData)
        {
            return GameCore.PlayerData.DnD5eWeaponCalculator.CalculateWeaponData(weaponName, characterData);
        }
    }
}


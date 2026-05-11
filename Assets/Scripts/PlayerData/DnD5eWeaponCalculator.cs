using System.Collections.Generic;
using GameCore.PlayerData;
using GameCore.UI.InGame.Models;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Calculates weapon attack and damage bonuses according to D&D 5e rules.
    /// Checks weapon proficiency and uses appropriate ability modifier.
    /// </summary>
    public static class DnD5eWeaponCalculator
    {
        private static readonly string[] KnownWeaponNames =
        {
            "Club",
            "Dagger",
            "Greatclub",
            "Handaxe",
            "Javelin",
            "Light Hammer",
            "Mace",
            "Quarterstaff",
            "Sickle",
            "Spear",
            "Light Crossbow",
            "Dart",
            "Shortbow",
            "Sling",
            "Battleaxe",
            "Flail",
            "Glaive",
            "Greataxe",
            "Greatsword",
            "Halberd",
            "Lance",
            "Longsword",
            "Maul",
            "Morningstar",
            "Pike",
            "Rapier",
            "Scimitar",
            "Shortsword",
            "Trident",
            "War Pick",
            "Warhammer",
            "Whip",
            "Blowgun",
            "Hand Crossbow",
            "Heavy Crossbow",
            "Longbow",
            "Net"
        };

        /// <summary>
        /// Weapon properties for D&D 5e weapons.
        /// </summary>
        public struct WeaponProperties
        {
            public string name;
            public int damageDice;
            public int damageDieType;
            public string damageType; // "Slashing", "Piercing", "Bludgeoning"
            public string abilityModifier; // "STR" or "DEX" - which ability to use
            public bool isFinesse; // Can use STR or DEX
            public bool isRanged; // Uses DEX for attack
            public bool isTwoHanded; // Requires two hands
            public string weaponCategory; // "Simple" or "Martial"
        }

        public static IReadOnlyList<string> GetKnownWeaponNames() => KnownWeaponNames;

        public static IReadOnlyList<WeaponProperties> GetAllWeaponProperties()
        {
            var weapons = new List<WeaponProperties>(KnownWeaponNames.Length);
            foreach (string weaponName in KnownWeaponNames)
            {
                WeaponProperties? properties = GetWeaponProperties(weaponName);
                if (properties.HasValue)
                    weapons.Add(properties.Value);
            }

            return weapons;
        }

        /// <summary>
        /// Gets weapon properties for a weapon by name.
        /// </summary>
        public static WeaponProperties? GetWeaponProperties(string weaponName)
        {
            return weaponName switch
            {
                // Simple Melee Weapons
                "Club" => new WeaponProperties
                {
                    name = "Club",
                    damageDice = 1,
                    damageDieType = 4,
                    damageType = "Bludgeoning",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Simple"
                },
                "Dagger" => new WeaponProperties
                {
                    name = "Dagger",
                    damageDice = 1,
                    damageDieType = 4,
                    damageType = "Piercing",
                    abilityModifier = "STR", // Can use DEX if finesse
                    isFinesse = true,
                    isRanged = false,
                    weaponCategory = "Simple"
                },
                "Greatclub" => new WeaponProperties
                {
                    name = "Greatclub",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Bludgeoning",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    isTwoHanded = true,
                    weaponCategory = "Simple"
                },
                "Handaxe" => new WeaponProperties
                {
                    name = "Handaxe",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Slashing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Simple"
                },
                "Javelin" => new WeaponProperties
                {
                    name = "Javelin",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Piercing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Simple"
                },
                "Light Hammer" => new WeaponProperties
                {
                    name = "Light Hammer",
                    damageDice = 1,
                    damageDieType = 4,
                    damageType = "Bludgeoning",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Simple"
                },
                "Mace" => new WeaponProperties
                {
                    name = "Mace",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Bludgeoning",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Simple"
                },
                "Quarterstaff" => new WeaponProperties
                {
                    name = "Quarterstaff",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Bludgeoning",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Simple"
                },
                "Sickle" => new WeaponProperties
                {
                    name = "Sickle",
                    damageDice = 1,
                    damageDieType = 4,
                    damageType = "Slashing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Simple"
                },
                "Spear" => new WeaponProperties
                {
                    name = "Spear",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Piercing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Simple"
                },

                // Simple Ranged Weapons
                "Light Crossbow" => new WeaponProperties
                {
                    name = "Light Crossbow",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Piercing",
                    abilityModifier = "DEX",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Simple"
                },
                "Dart" => new WeaponProperties
                {
                    name = "Dart",
                    damageDice = 1,
                    damageDieType = 4,
                    damageType = "Piercing",
                    abilityModifier = "DEX", // Can use STR if finesse
                    isFinesse = true,
                    isRanged = true,
                    weaponCategory = "Simple"
                },
                "Shortbow" => new WeaponProperties
                {
                    name = "Shortbow",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Piercing",
                    abilityModifier = "DEX",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Simple"
                },
                "Sling" => new WeaponProperties
                {
                    name = "Sling",
                    damageDice = 1,
                    damageDieType = 4,
                    damageType = "Bludgeoning",
                    abilityModifier = "DEX",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Simple"
                },

                // Martial Melee Weapons
                "Battleaxe" => new WeaponProperties
                {
                    name = "Battleaxe",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Slashing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Flail" => new WeaponProperties
                {
                    name = "Flail",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Bludgeoning",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Glaive" => new WeaponProperties
                {
                    name = "Glaive",
                    damageDice = 1,
                    damageDieType = 10,
                    damageType = "Slashing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    isTwoHanded = true,
                    weaponCategory = "Martial"
                },
                "Greataxe" => new WeaponProperties
                {
                    name = "Greataxe",
                    damageDice = 1,
                    damageDieType = 12,
                    damageType = "Slashing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    isTwoHanded = true,
                    weaponCategory = "Martial"
                },
                "Greatsword" => new WeaponProperties
                {
                    name = "Greatsword",
                    damageDice = 2,
                    damageDieType = 6,
                    damageType = "Slashing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    isTwoHanded = true,
                    weaponCategory = "Martial"
                },
                "Halberd" => new WeaponProperties
                {
                    name = "Halberd",
                    damageDice = 1,
                    damageDieType = 10,
                    damageType = "Slashing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    isTwoHanded = true,
                    weaponCategory = "Martial"
                },
                "Lance" => new WeaponProperties
                {
                    name = "Lance",
                    damageDice = 1,
                    damageDieType = 12,
                    damageType = "Piercing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Longsword" => new WeaponProperties
                {
                    name = "Longsword",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Slashing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Maul" => new WeaponProperties
                {
                    name = "Maul",
                    damageDice = 2,
                    damageDieType = 6,
                    damageType = "Bludgeoning",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    isTwoHanded = true,
                    weaponCategory = "Martial"
                },
                "Morningstar" => new WeaponProperties
                {
                    name = "Morningstar",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Piercing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Pike" => new WeaponProperties
                {
                    name = "Pike",
                    damageDice = 1,
                    damageDieType = 10,
                    damageType = "Piercing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    isTwoHanded = true,
                    weaponCategory = "Martial"
                },
                "Rapier" => new WeaponProperties
                {
                    name = "Rapier",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Piercing",
                    abilityModifier = "STR", // Can use DEX if finesse
                    isFinesse = true,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Scimitar" => new WeaponProperties
                {
                    name = "Scimitar",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Slashing",
                    abilityModifier = "STR", // Can use DEX if finesse
                    isFinesse = true,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Shortsword" => new WeaponProperties
                {
                    name = "Shortsword",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Piercing",
                    abilityModifier = "STR", // Can use DEX if finesse
                    isFinesse = true,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Trident" => new WeaponProperties
                {
                    name = "Trident",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Piercing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "War Pick" => new WeaponProperties
                {
                    name = "War Pick",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Piercing",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Warhammer" => new WeaponProperties
                {
                    name = "Warhammer",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Bludgeoning",
                    abilityModifier = "STR",
                    isFinesse = false,
                    isRanged = false,
                    weaponCategory = "Martial"
                },
                "Whip" => new WeaponProperties
                {
                    name = "Whip",
                    damageDice = 1,
                    damageDieType = 4,
                    damageType = "Slashing",
                    abilityModifier = "STR", // Can use DEX if finesse
                    isFinesse = true,
                    isRanged = false,
                    weaponCategory = "Martial"
                },

                // Martial Ranged Weapons
                "Blowgun" => new WeaponProperties
                {
                    name = "Blowgun",
                    damageDice = 1,
                    damageDieType = 1,
                    damageType = "Piercing",
                    abilityModifier = "DEX",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Martial"
                },
                "Hand Crossbow" => new WeaponProperties
                {
                    name = "Hand Crossbow",
                    damageDice = 1,
                    damageDieType = 6,
                    damageType = "Piercing",
                    abilityModifier = "DEX",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Martial"
                },
                "Heavy Crossbow" => new WeaponProperties
                {
                    name = "Heavy Crossbow",
                    damageDice = 1,
                    damageDieType = 10,
                    damageType = "Piercing",
                    abilityModifier = "DEX",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Martial"
                },
                "Longbow" => new WeaponProperties
                {
                    name = "Longbow",
                    damageDice = 1,
                    damageDieType = 8,
                    damageType = "Piercing",
                    abilityModifier = "DEX",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Martial"
                },
                "Net" => new WeaponProperties
                {
                    name = "Net",
                    damageDice = 0,
                    damageDieType = 0,
                    damageType = "",
                    abilityModifier = "DEX",
                    isFinesse = false,
                    isRanged = true,
                    weaponCategory = "Martial"
                },

                _ => null
            };
        }

        /// <summary>
        /// Calculates weapon data for a character using D&D 5e rules.
        /// Checks weapon proficiency and uses appropriate ability modifier.
        /// </summary>
        public static WeaponData CalculateWeaponData(string weaponName, DnD5eCharacterData characterData)
        {
            var properties = GetWeaponProperties(weaponName);
            if (!properties.HasValue)
            {
                // Unknown weapon - return default
                return new WeaponData
                {
                    WeaponName = weaponName,
                    AttackBonus = 0,
                    DamageDice = 1,
                    DamageDieType = 4,
                    DamageModifier = 0
                };
            }

            var props = properties.Value;

            // Determine which ability modifier to use
            string abilityModifier = DetermineAbilityModifier(props, characterData);
            int abilityMod = characterData.GetAbilityModifier(abilityModifier);

            // Check if character is proficient with this weapon
            bool isProficient = IsProficientWithWeapon(weaponName, props, characterData);
            int proficiencyBonus = isProficient ? characterData.proficiencyBonus : 0;

            // Attack bonus = ability modifier + proficiency bonus (if proficient)
            int attackBonus = abilityMod + proficiencyBonus;

            // Damage modifier = ability modifier (always, even if not proficient)
            // Exception: Ranged weapons with thrown property use STR for damage
            int damageMod = abilityMod;
            if (props.isRanged && !props.isFinesse)
            {
                // Ranged weapons don't add ability modifier to damage unless they're thrown
                // For simplicity, we'll use the ability modifier (most ranged weapons don't add it)
                // This can be refined later for specific weapons
            }

            return new WeaponData
            {
                WeaponName = props.name,
                AttackBonus = attackBonus,
                DamageDice = props.damageDice,
                DamageDieType = props.damageDieType,
                DamageModifier = damageMod
            };
        }

        /// <summary>
        /// Determines which ability modifier to use for a weapon.
        /// Handles finesse weapons (can use STR or DEX, whichever is higher).
        /// </summary>
        private static string DetermineAbilityModifier(WeaponProperties props, DnD5eCharacterData characterData)
        {
            // Ranged weapons always use DEX
            if (props.isRanged)
            {
                return "DEX";
            }

            // Finesse weapons can use STR or DEX (whichever is higher)
            if (props.isFinesse)
            {
                int strMod = characterData.strengthModifier;
                int dexMod = characterData.dexterityModifier;
                return strMod >= dexMod ? "STR" : "DEX";
            }

            // Melee weapons use STR by default
            return "STR";
        }

        /// <summary>
        /// Checks if the character is proficient with a weapon.
        /// </summary>
        private static bool IsProficientWithWeapon(string weaponName, WeaponProperties props, DnD5eCharacterData characterData)
        {
            // Check if weapon name is in proficient weapons list
            if (characterData.proficientWeapons.Contains(weaponName))
            {
                return true;
            }

            // Check if weapon category is in proficient weapons list
            if (characterData.proficientWeapons.Contains(props.weaponCategory))
            {
                return true;
            }

            // Check if "Simple" or "Martial" is in the list
            if (characterData.proficientWeapons.Contains("Simple") && props.weaponCategory == "Simple")
            {
                return true;
            }

            if (characterData.proficientWeapons.Contains("Martial") && props.weaponCategory == "Martial")
            {
                return true;
            }

            return false;
        }
    }
}


using System;
using System.Collections.Generic;
using GameCore.Combat.ActionEconomy;
using GameCore.Combat.Models;
using GameCore.PlayerData;
using GameCore.PlayerData.Rulesets;

namespace GameCore.Combat.Services
{
    /// <summary>
    /// Builds attack and damage modifiers from a character sheet and ruleset weapon data.
    /// </summary>
    public sealed class AttackStatBuilder
    {
        private readonly IRulesetCalculator _calculator;

        public AttackStatBuilder(IRulesetCalculator calculator)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public bool TryBuild(string weaponName, ICharacterSheet sheet, out AttackStats stats)
        {
            stats = default;
            if (sheet == null)
                return false;

            var properties = _calculator.GetWeaponProperties(weaponName);
            if (!properties.HasValue)
                return false;

            var props = properties.Value;
            int strengthModifier = sheet.GetAbilityModifier("STR");
            int dexterityModifier = sheet.GetAbilityModifier("DEX");
            int abilityModifier = _calculator.GetWeaponAbilityModifier(
                weaponName,
                strengthModifier,
                dexterityModifier);

            var proficientWeapons = sheet.ProficientWeapons != null
                ? new List<string>(sheet.ProficientWeapons)
                : new List<string>();

            bool isProficient = _calculator.IsProficientWithWeapon(weaponName, proficientWeapons);
            int attackBonus = _calculator.CalculateWeaponAttackBonus(
                weaponName,
                abilityModifier,
                isProficient,
                sheet.Level);
            int damageModifier = _calculator.CalculateWeaponDamageModifier(weaponName, abilityModifier);

            stats = new AttackStats(
                attackBonus,
                props.FlatBaseDamage,
                props.DamageDice,
                props.DamageDieType,
                damageModifier,
                props.DamageType);

            return true;
        }

        public readonly struct AttackStats
        {
            public AttackStats(
                int attackBonus,
                int flatBaseDamage,
                int damageDice,
                int damageDieType,
                int damageAbilityModifier,
                string damageType)
            {
                AttackBonus = attackBonus;
                FlatBaseDamage = flatBaseDamage;
                DamageDice = damageDice;
                DamageDieType = damageDieType;
                DamageAbilityModifier = damageAbilityModifier;
                DamageType = damageType ?? string.Empty;
            }

            public int AttackBonus { get; }
            public int FlatBaseDamage { get; }
            public int DamageDice { get; }
            public int DamageDieType { get; }
            public int DamageAbilityModifier { get; }
            public string DamageType { get; }

            public bool UsesFlatDamage => FlatBaseDamage > 0 && DamageDice == 0;
        }
    }
}

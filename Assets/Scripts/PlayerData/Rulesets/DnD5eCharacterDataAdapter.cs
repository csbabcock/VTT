using System.Collections.Generic;
using System.Linq;
using GameCore.UI.InGame.Models;
using GameCore.PlayerData;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// Adapter that exposes a <see cref="DnD5eCharacterData"/> as the ruleset-agnostic
    /// dictionaries/weapon data the character sheet UI consumes.
    /// Follows the Adapter Pattern so generic UI code stays decoupled from ruleset models.
    /// </summary>
    public class DnD5eCharacterDataAdapter : ICharacterDataAdapter
    {
        public string RulesetId => "DnD5e";

        public Dictionary<string, int> GetAbilityScores(object rulesetData)
        {
            if (rulesetData is DnD5eCharacterData data)
            {
                return new Dictionary<string, int>
                {
                    { "STR", data.strength },
                    { "DEX", data.dexterity },
                    { "CON", data.constitution },
                    { "INT", data.intelligence },
                    { "WIS", data.wisdom },
                    { "CHA", data.charisma }
                };
            }
            throw new System.ArgumentException($"Expected DnD5eCharacterData, got {rulesetData?.GetType()}");
        }

        public Dictionary<string, int> GetAbilityModifiers(object rulesetData, IRulesetCalculator calculator)
        {
            var scores = GetAbilityScores(rulesetData);
            var modifiers = new Dictionary<string, int>();
            foreach (var kvp in scores)
            {
                modifiers[kvp.Key] = calculator.CalculateAbilityModifier(kvp.Value);
            }
            return modifiers;
        }

        public Dictionary<string, int> GetSkillModifiers(object rulesetData, IRulesetCalculator calculator)
        {
            if (!(rulesetData is DnD5eCharacterData data))
                throw new System.ArgumentException($"Expected DnD5eCharacterData, got {rulesetData?.GetType()}");

            var modifiers = new Dictionary<string, int>();
            var abilityModifiers = GetAbilityModifiers(rulesetData, calculator);
            var proficientSkills = GetProficientSkills(rulesetData);

            foreach (DnD5eSkill skill in System.Enum.GetValues(typeof(DnD5eSkill)))
            {
                string skillId = skill.ToString();
                string skillName = skill.GetDisplayName();
                string abilityName = skill.GetAbilityScore();

                bool isProficient = proficientSkills.Contains(skillName);
                bool hasExpertise = data.IsExpertInSkill(skill);
                int abilityModifier = abilityModifiers[abilityName];
                int skillModifier = calculator.CalculateSkillModifier(abilityModifier, isProficient, hasExpertise, data.level);

                modifiers[skillName] = skillModifier;
            }

            return modifiers;
        }

        public List<string> GetProficientSkills(object rulesetData)
        {
            if (rulesetData is DnD5eCharacterData data)
            {
                return data.GetProficientSkills().Select(s => s.GetDisplayName()).ToList();
            }
            throw new System.ArgumentException($"Expected DnD5eCharacterData, got {rulesetData?.GetType()}");
        }

        public WeaponData GetWeaponData(string weaponName, object rulesetData, IRulesetCalculator calculator)
        {
            if (!(rulesetData is DnD5eCharacterData data))
                throw new System.ArgumentException($"Expected DnD5eCharacterData, got {rulesetData?.GetType()}");

            var abilityModifiers = GetAbilityModifiers(rulesetData, calculator);
            var weaponProperties = calculator.GetWeaponProperties(weaponName);

            if (!weaponProperties.HasValue)
            {
                return new WeaponData
                {
                    WeaponName = weaponName,
                    AttackBonus = 0,
                    DamageDice = 0,
                    DamageDieType = 0,
                    DamageModifier = 0
                };
            }

            var props = weaponProperties.Value;
            int strMod = abilityModifiers["STR"];
            int dexMod = abilityModifiers["DEX"];
            int abilityMod = calculator.GetWeaponAbilityModifier(weaponName, strMod, dexMod);
            bool isProficient = calculator.IsProficientWithWeapon(weaponName, data.proficientWeapons);

            int attackBonus = calculator.CalculateWeaponAttackBonus(weaponName, abilityMod, isProficient, data.level);
            int damageModifier = calculator.CalculateWeaponDamageModifier(weaponName, abilityMod);

            return new WeaponData
            {
                WeaponName = props.Name,
                AttackBonus = attackBonus,
                DamageDice = props.DamageDice,
                DamageDieType = props.DamageDieType,
                DamageModifier = damageModifier
            };
        }
    }
}


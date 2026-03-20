using System.Collections.Generic;
using GameCore.PlayerData;

namespace GameCore.PlayerData.Rulesets
{
    /// <summary>
    /// D&amp;D 5e ruleset calculator implementation.
    /// Follows Strategy Pattern - encapsulates all D&amp;D 5e specific calculation logic.
    /// </summary>
    public class DnD5eRulesetCalculator : IRulesetCalculator
    {
        private readonly IRulesetContentQuery _contentQuery;

        public string RulesetId => "DnD5e";

        /// <param name="contentQuery">When null, uses <see cref="RulesetContentQueryProvider"/> for DnD5e skills list.</param>
        public DnD5eRulesetCalculator(IRulesetContentQuery contentQuery = null)
        {
            _contentQuery = contentQuery ?? RulesetContentQueryProvider.GetOrCreate("DnD5e");
        }

        public int CalculateAbilityModifier(int abilityScore)
        {
            return (abilityScore - 10) / 2;
        }

        public int CalculateProficiencyBonus(int level)
        {
            return (level - 1) / 4 + 2;
        }

        public int CalculateSkillModifier(int abilityModifier, bool isProficient, int level)
        {
            int modifier = abilityModifier;
            if (isProficient)
            {
                modifier += CalculateProficiencyBonus(level);
            }
            return modifier;
        }

        public int CalculateSavingThrowModifier(int abilityModifier, bool isProficient, int level)
        {
            return CalculateSkillModifier(abilityModifier, isProficient, level);
        }

        public int CalculateWeaponAttackBonus(string weaponName, int abilityModifier, bool isProficient, int level)
        {
            int bonus = abilityModifier;
            if (isProficient)
            {
                bonus += CalculateProficiencyBonus(level);
            }
            return bonus;
        }

        public int CalculateWeaponDamageModifier(string weaponName, int abilityModifier)
        {
            // Proficiency does NOT apply to damage in D&D 5e
            return abilityModifier;
        }

        public int GetWeaponAbilityModifier(string weaponName, int strengthModifier, int dexterityModifier)
        {
            var properties = GetWeaponProperties(weaponName);
            if (!properties.HasValue)
            {
                return strengthModifier; // Default to STR
            }

            var props = properties.Value;

            // Ranged weapons use DEX
            if (props.IsRanged)
            {
                return dexterityModifier;
            }

            // Finesse weapons use higher of STR or DEX
            if (props.IsFinesse)
            {
                return strengthModifier >= dexterityModifier ? strengthModifier : dexterityModifier;
            }

            // Melee weapons use STR
            return strengthModifier;
        }

        public WeaponProperties? GetWeaponProperties(string weaponName)
        {
            // Use existing DnD5eWeaponCalculator for weapon data
            var props = DnD5eWeaponCalculator.GetWeaponProperties(weaponName);
            if (!props.HasValue)
                return null;

            var p = props.Value;
            return new WeaponProperties
            {
                Name = p.name,
                DamageDice = p.damageDice,
                DamageDieType = p.damageDieType,
                DamageType = p.damageType,
                IsFinesse = p.isFinesse,
                IsRanged = p.isRanged,
                Category = p.weaponCategory
            };
        }

        public bool IsProficientWithWeapon(string weaponName, List<string> proficientWeapons)
        {
            if (proficientWeapons == null || proficientWeapons.Count == 0)
                return false;

            var properties = GetWeaponProperties(weaponName);
            if (!properties.HasValue)
                return false;

            var props = properties.Value;

            // Check exact weapon name
            if (proficientWeapons.Contains(weaponName))
                return true;

            // Check category (e.g., "Simple", "Martial")
            if (proficientWeapons.Contains(props.Category))
                return true;

            return false;
        }

        public Dictionary<string, string> GetAvailableSkills()
        {
            var skills = new Dictionary<string, string>();
            foreach (var s in _contentQuery.GetSkills())
            {
                if (!string.IsNullOrEmpty(s.id))
                    skills[s.id] = string.IsNullOrEmpty(s.name) ? s.id : s.name;
            }

            if (skills.Count > 0)
                return skills;

            foreach (DnD5eSkill skill in System.Enum.GetValues(typeof(DnD5eSkill)))
                skills[skill.ToString()] = skill.GetDisplayName();
            return skills;
        }

        public string GetSkillAbilityScore(string skillId)
        {
            if (_contentQuery.TryGetSkill(skillId, out var s) && !string.IsNullOrEmpty(s.ability))
                return s.ability;

            if (System.Enum.TryParse<DnD5eSkill>(skillId, out DnD5eSkill byEnum))
                return byEnum.GetAbilityScore();

            string tail = skillId;
            if (!string.IsNullOrEmpty(skillId) && skillId.StartsWith("skill.", System.StringComparison.Ordinal))
                tail = skillId.Substring("skill.".Length);

            DnD5eSkill? fromPretty = DnD5eSkillExtensions.FromString(tail.Replace('_', ' '));
            if (fromPretty.HasValue)
                return fromPretty.Value.GetAbilityScore();

            return "STR";
        }
    }
}

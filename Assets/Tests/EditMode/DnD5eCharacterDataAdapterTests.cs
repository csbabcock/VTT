using System;
using System.Collections.Generic;
using NUnit.Framework;
using GameCore.PlayerData;
using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="DnD5eCharacterDataAdapter"/> — the path the character sheet UI
    /// uses to read abilities, skills, and weapon stats from a sheet.
    /// </summary>
    public class DnD5eCharacterDataAdapterTests
    {
        private DnD5eCharacterDataAdapter _adapter;
        private DnD5eRulesetCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _adapter = new DnD5eCharacterDataAdapter();
            _calculator = new DnD5eRulesetCalculator(new EmptyContentQuery());
        }

        [Test]
        public void GetSkillModifiers_AppliesExpertiseThroughCalculator()
        {
            // Level 5 -> +3 proficiency, DEX 14 -> +2 ability, Stealth expertise.
            var data = new DnD5eCharacterData { level = 5, dexterity = 14 };
            data.SetExpertiseSkills(new List<DnD5eSkill> { DnD5eSkill.Stealth });

            var modifiers = _adapter.GetSkillModifiers(data, _calculator);

            Assert.AreEqual(8, modifiers["Stealth"]);
        }

        [Test]
        public void GetProficientSkills_IncludesExpertiseOnlySkills()
        {
            var data = new DnD5eCharacterData();
            data.SetExpertiseSkills(new List<DnD5eSkill> { DnD5eSkill.Deception });

            var proficient = _adapter.GetProficientSkills(data);

            Assert.Contains("Deception", proficient);
        }

        [Test]
        public void GetWeaponData_Longsword_UsesStrengthAndProficiency()
        {
            var data = new DnD5eCharacterData
            {
                level = 5,
                strength = 16,
                proficientWeapons = new List<string> { "Martial" },
            };

            var weapon = _adapter.GetWeaponData("Longsword", data, _calculator);

            Assert.AreEqual("Longsword", weapon.WeaponName);
            Assert.AreEqual(6, weapon.AttackBonus);   // +3 STR + +3 proficiency
            Assert.AreEqual(3, weapon.DamageModifier); // proficiency does not apply to damage
            Assert.AreEqual(1, weapon.DamageDice);
            Assert.AreEqual(8, weapon.DamageDieType);
        }

        private sealed class EmptyContentQuery : IRulesetContentQuery
        {
            public string RulesetId => "DnD5e";
            public IReadOnlyCollection<RaceDefinition> GetRaces() => Array.Empty<RaceDefinition>();
            public IReadOnlyCollection<ClassDefinition> GetClasses() => Array.Empty<ClassDefinition>();
            public IReadOnlyCollection<BackgroundDefinition> GetBackgrounds() => Array.Empty<BackgroundDefinition>();
            public IReadOnlyCollection<SkillDefinition> GetSkills() => Array.Empty<SkillDefinition>();
            public IReadOnlyCollection<SpellDefinition> GetSpells() => Array.Empty<SpellDefinition>();
            public IReadOnlyCollection<RuleTopicDefinition> GetRuleTopics() => Array.Empty<RuleTopicDefinition>();
            public bool TryGetRace(string raceId, out RaceDefinition race) { race = null; return false; }
            public bool TryGetClass(string classId, out ClassDefinition classDef) { classDef = null; return false; }
            public bool TryGetBackground(string backgroundId, out BackgroundDefinition background) { background = null; return false; }
            public bool TryGetSkill(string skillId, out SkillDefinition skill) { skill = null; return false; }
            public bool TryGetSpell(string spellId, out SpellDefinition spell) { spell = null; return false; }
            public bool TryGetRuleTopic(string topicId, out RuleTopicDefinition topic) { topic = null; return false; }
        }
    }
}

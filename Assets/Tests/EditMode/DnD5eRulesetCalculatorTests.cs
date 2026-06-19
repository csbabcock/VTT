using System;
using System.Collections.Generic;
using NUnit.Framework;
using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Pure-logic tests for the DnD5e ruleset calculator. These exercise the math the
    /// in-game UI and (soon) networked combat depend on, with no scene or file I/O.
    /// </summary>
    public class DnD5eRulesetCalculatorTests
    {
        private DnD5eRulesetCalculator _calc;

        [SetUp]
        public void SetUp()
        {
            // Inject an empty content query so the calculator never touches disk.
            _calc = new DnD5eRulesetCalculator(new EmptyContentQuery());
        }

        [TestCase(10, 0)]
        [TestCase(8, -1)]
        [TestCase(12, 1)]
        [TestCase(14, 2)]
        [TestCase(16, 3)]
        [TestCase(20, 5)]
        public void AbilityModifier_MatchesDnD5eRules(int score, int expected)
        {
            Assert.AreEqual(expected, _calc.CalculateAbilityModifier(score));
        }

        [TestCase(1, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(8, 3)]
        [TestCase(9, 4)]
        [TestCase(20, 6)]
        public void ProficiencyBonus_ScalesWithLevel(int level, int expected)
        {
            Assert.AreEqual(expected, _calc.CalculateProficiencyBonus(level));
        }

        [Test]
        public void SkillModifier_AddsProficiencyOnlyWhenProficient()
        {
            // Level 1 proficiency bonus = 2, ability modifier = 3.
            Assert.AreEqual(5, _calc.CalculateSkillModifier(3, isProficient: true, level: 1));
            Assert.AreEqual(3, _calc.CalculateSkillModifier(3, isProficient: false, level: 1));
        }

        [Test]
        public void SkillModifier_DoublesProficiencyWithExpertise()
        {
            // Level 5 proficiency bonus = 3, ability modifier = 2.
            Assert.AreEqual(5, _calc.CalculateSkillModifier(2, isProficient: true, hasExpertise: false, level: 5));
            Assert.AreEqual(8, _calc.CalculateSkillModifier(2, isProficient: true, hasExpertise: true, level: 5));
            // Expertise implies proficiency even if the proficient flag is not set.
            Assert.AreEqual(8, _calc.CalculateSkillModifier(2, isProficient: false, hasExpertise: true, level: 5));
        }

        [Test]
        public void SavingThrowModifier_UsesSameFormulaAsSkills()
        {
            Assert.AreEqual(
                _calc.CalculateSkillModifier(2, true, 5),
                _calc.CalculateSavingThrowModifier(2, true, 5));
        }

        [Test]
        public void WeaponDamageModifier_DoesNotIncludeProficiency()
        {
            // Damage equals the ability modifier regardless of proficiency in 5e.
            Assert.AreEqual(3, _calc.CalculateWeaponDamageModifier("Longsword", 3));
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

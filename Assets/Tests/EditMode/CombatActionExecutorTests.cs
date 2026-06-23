using System;
using System.Collections.Generic;
using GameCore.Combat;
using GameCore.Combat.ActionEconomy;
using GameCore.Combat.Adapters;
using GameCore.Combat.Definitions;
using GameCore.Combat.Models;
using GameCore.Combat.Services;
using GameCore.PlayerData;
using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class CombatActionExecutorTests
    {
        private DnD5eRulesetCalculator _calculator;

        [SetUp]
        public void SetUp() => _calculator = new DnD5eRulesetCalculator(new EmptyContentQuery());

        [Test]
        public void UnarmedStrike_WithStr16_HitsAc18For4Damage()
        {
            var attacker = CreateAttacker(strength: 16, level: 1);
            var target = new FakeDamageable { ArmorClass = 18, CurrentHitPoints = 20 };

            var result = ExecuteWithRoll(attacker, target, naturalRoll: 15);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.AttackOutcome.DidHit);
            Assert.AreEqual(4, result.AttackOutcome.DamageAmount);
            Assert.AreEqual(16, target.CurrentHitPoints);
        }

        [Test]
        public void UnarmedStrike_Natural1_MissesWithoutDamage()
        {
            var attacker = CreateAttacker(strength: 16, level: 1);
            var target = new FakeDamageable { ArmorClass = 10, CurrentHitPoints = 20 };

            var result = ExecuteWithRoll(attacker, target, naturalRoll: 1);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.AttackOutcome.DidHit);
            Assert.AreEqual(20, target.CurrentHitPoints);
        }

        [Test]
        public void UnarmedStrike_NotProficient_ExcludesProficiencyFromAttack()
        {
            var attacker = CreateAttacker(strength: 10, level: 5);
            var target = new FakeDamageable { ArmorClass = 11, CurrentHitPoints = 20 };

            // d20=10 + STR 0 = 10, misses AC 11 without proficiency.
            var result = ExecuteWithRoll(attacker, target, naturalRoll: 10);

            Assert.IsFalse(result.AttackOutcome.DidHit);
        }

        [Test]
        public void UnarmedStrike_MonkProficient_IncludesProficiencyOnAttack()
        {
            var data = new DnD5eCharacterData
            {
                characterName = "Monk",
                level = 5,
                strength = 10,
            };
            data.proficientWeapons.Add("Unarmed");
            var attacker = new SheetAttackParticipant(data.characterName, data);
            var target = new FakeDamageable { ArmorClass = 13, CurrentHitPoints = 20 };

            // d20=10 + prof(3) = 13, hits AC 13.
            var result = ExecuteWithRoll(attacker, target, naturalRoll: 10);

            Assert.IsTrue(result.AttackOutcome.DidHit);
            Assert.AreEqual(1, result.AttackOutcome.DamageAmount);
        }

        [Test]
        public void OutOfEncounter_AllowsMultipleAttacks()
        {
            var attacker = CreateAttacker(strength: 16, level: 1);
            var target = new FakeDamageable { ArmorClass = 10, CurrentHitPoints = 30 };
            var economy = new ActionEconomyTracker();

            var first = ExecuteWithRoll(attacker, target, naturalRoll: 15, economy);
            var second = ExecuteWithRoll(attacker, target, naturalRoll: 15, economy);

            Assert.IsTrue(first.Succeeded);
            Assert.IsTrue(second.Succeeded);
            Assert.AreEqual(22, target.CurrentHitPoints);
        }

        [Test]
        public void InEncounter_SecondActionAttack_IsRejected()
        {
            var attacker = CreateAttacker(strength: 16, level: 1);
            var target = new FakeDamageable { ArmorClass = 10, CurrentHitPoints = 30 };
            var economy = new ActionEconomyTracker();
            var context = new EncounterContext(isEncounterActive: true, isLocalTurnActive: true);

            var first = ExecuteWithRoll(attacker, target, naturalRoll: 15, economy, context);
            var second = ExecuteWithRoll(attacker, target, naturalRoll: 15, economy, context);

            Assert.IsTrue(first.Succeeded);
            Assert.IsFalse(second.Succeeded);
            Assert.AreEqual(CombatFailureReason.ActionAlreadyUsed, second.FailureReason);
            Assert.AreEqual(26, target.CurrentHitPoints);
        }

        [Test]
        public void InEncounter_NotYourTurn_IsRejected()
        {
            var attacker = CreateAttacker(strength: 16, level: 1);
            var target = new FakeDamageable { ArmorClass = 10, CurrentHitPoints = 20 };
            var context = new EncounterContext(isEncounterActive: true, isLocalTurnActive: false);

            var result = ExecuteWithRoll(attacker, target, naturalRoll: 15, context: context);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CombatFailureReason.NotYourTurn, result.FailureReason);
            Assert.AreEqual(20, target.CurrentHitPoints);
        }

        [Test]
        public void TargetDestroyed_IsRejected()
        {
            var attacker = CreateAttacker(strength: 16, level: 1);
            var target = new FakeDamageable { ArmorClass = 10, CurrentHitPoints = 0 };

            var result = ExecuteWithRoll(attacker, target, naturalRoll: 15);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CombatFailureReason.TargetDestroyed, result.FailureReason);
        }

        private CombatActionResult ExecuteWithRoll(
            IAttackParticipant attacker,
            FakeDamageable target,
            int naturalRoll,
            IActionEconomyTracker economy = null,
            EncounterContext? context = null)
        {
            var executor = new CombatActionExecutor(
                new AttackStatBuilder(_calculator),
                new AttackResolutionService(),
                new QueueRandomSource(naturalRoll));

            return executor.TryExecute(
                UnarmedStrikeAttackDefinition.Instance,
                attacker,
                target,
                context ?? EncounterContext.OutOfEncounter,
                economy);
        }

        private static SheetAttackParticipant CreateAttacker(int strength, int level)
        {
            var data = new DnD5eCharacterData
            {
                characterName = "Fighter",
                level = level,
                strength = strength,
            };
            return new SheetAttackParticipant(data.characterName, data);
        }

        private sealed class FakeDamageable : IDamageable
        {
            public string DisplayName { get; set; } = "Target";
            public int ArmorClass { get; set; }
            public int CurrentHitPoints { get; set; }
            public int MaxHitPoints { get; set; } = 100;
            public bool IsDestroyed => CurrentHitPoints <= 0;

            public void ApplyDamage(int amount)
            {
                if (amount <= 0)
                    return;

                CurrentHitPoints = Math.Max(0, CurrentHitPoints - amount);
            }
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

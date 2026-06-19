using System.Collections.Generic;
using NUnit.Framework;
using GameCore.Actors;
using GameCore.PlayerData;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Tests that the ruleset-agnostic ICharacterSheet projection over DnD5eCharacterData
    /// returns the expected values, and that the data services expose a sheet.
    /// </summary>
    public class CharacterSheetTests
    {
        [Test]
        public void DnD5eCharacterData_ExposesRulesetAgnosticSheet()
        {
            var data = new DnD5eCharacterData
            {
                characterName = "Tordek",
                level = 5,
                strength = 16,
                dexterity = 14,
            };

            ICharacterSheet sheet = data;

            Assert.AreEqual("DnD5e", sheet.RulesetId);
            Assert.AreEqual("Tordek", sheet.CharacterName);
            Assert.AreEqual(5, sheet.Level);
            Assert.AreEqual(16, sheet.GetAbilityScore("STR"));
            Assert.AreEqual(3, sheet.GetAbilityModifier("STR"));
            Assert.AreEqual(2, sheet.GetAbilityModifier("DEX"));
            Assert.AreEqual(0, sheet.GetAbilityScore("NOPE"));
        }

        [Test]
        public void DnD5eCharacterData_ProficiencyBonusScalesWithLevel()
        {
            ICharacterSheet sheet = new DnD5eCharacterData { level = 5 };
            Assert.AreEqual(3, sheet.ProficiencyBonus);
        }

        [Test]
        public void DnD5eCharacterData_ExpertiseDoublesProficiencyAndImpliesProficiency()
        {
            // Level 5 -> proficiency bonus +3, DEX 14 -> modifier +2.
            var data = new DnD5eCharacterData { level = 5, dexterity = 14 };
            data.SetExpertiseSkills(new List<DnD5eSkill> { DnD5eSkill.Acrobatics });

            // Acrobatics (DEX): +2 ability + (2 x 3) expertise = +8.
            Assert.AreEqual(8, data.GetSkillModifier(DnD5eSkill.Acrobatics));
            Assert.IsTrue(data.IsExpertInSkill(DnD5eSkill.Acrobatics));
            Assert.IsTrue(data.IsProficientInSkill(DnD5eSkill.Acrobatics));
        }

        [Test]
        public void DnD5eCharacterData_ProficiencyWithoutExpertiseIsSingle()
        {
            var data = new DnD5eCharacterData { level = 5, dexterity = 14 };
            data.SetProficientSkills(new List<DnD5eSkill> { DnD5eSkill.Acrobatics });

            // +2 ability + 3 proficiency = +5 (no expertise doubling).
            Assert.AreEqual(5, data.GetSkillModifier(DnD5eSkill.Acrobatics));
            Assert.IsFalse(data.IsExpertInSkill(DnD5eSkill.Acrobatics));
        }

        [Test]
        public void LocalPlayerDataService_ProvidesSheet()
        {
            var service = new LocalPlayerDataService();

            ICharacterSheet sheet = service.GetCharacterSheet();

            Assert.IsNotNull(sheet);
            Assert.AreEqual("DnD5e", sheet.RulesetId);
        }

        [Test]
        public void ICharacterSheet_ExposesSkillReads_ForExpertise()
        {
            // Level 5 -> proficiency +3, DEX 14 -> +2 modifier.
            var data = new DnD5eCharacterData { level = 5, dexterity = 14 };
            data.SetExpertiseSkills(new List<DnD5eSkill> { DnD5eSkill.Acrobatics });

            ICharacterSheet sheet = data;

            Assert.AreEqual("DEX", sheet.GetSkillAbility("Acrobatics"));
            Assert.IsTrue(sheet.IsProficientInSkill("Acrobatics"));
            Assert.IsTrue(sheet.HasExpertiseInSkill("Acrobatics"));
            // +2 ability + (2 x 3) expertise = +8.
            Assert.AreEqual(8, sheet.GetSkillModifier("Acrobatics"));
        }

        [Test]
        public void ICharacterSheet_ProficiencyWithoutExpertise_IsSingle()
        {
            var data = new DnD5eCharacterData { level = 5, dexterity = 14 };
            data.SetProficientSkills(new List<DnD5eSkill> { DnD5eSkill.Acrobatics });

            ICharacterSheet sheet = data;

            Assert.IsTrue(sheet.IsProficientInSkill("Acrobatics"));
            Assert.IsFalse(sheet.HasExpertiseInSkill("Acrobatics"));
            // +2 ability + 3 proficiency = +5.
            Assert.AreEqual(5, sheet.GetSkillModifier("Acrobatics"));
        }

        [Test]
        public void ICharacterSheet_UnknownSkill_ReturnsDefaults()
        {
            ICharacterSheet sheet = new DnD5eCharacterData();

            Assert.AreEqual(string.Empty, sheet.GetSkillAbility("NotASkill"));
            Assert.AreEqual(0, sheet.GetSkillModifier("NotASkill"));
            Assert.IsFalse(sheet.IsProficientInSkill("NotASkill"));
            Assert.IsFalse(sheet.HasExpertiseInSkill("NotASkill"));
        }

        [Test]
        public void PlayerDataAsset_ToDnD5eCharacterData_MapsProficientSkills()
        {
            var asset = ScriptableObject.CreateInstance<PlayerDataAsset>();
            asset.characterName = "Preset";
            asset.proficientSkills = new List<string> { "Athletics" };

            ICharacterSheet sheet = asset.ToDnD5eCharacterData();

            Assert.AreEqual("Preset", sheet.CharacterName);
            Assert.IsTrue(sheet.IsProficientInSkill("Athletics"));

            Object.DestroyImmediate(asset);
        }
    }

    /// <summary>
    /// Tests for the actor registry seam that decouples gameplay systems from the
    /// single local player.
    /// </summary>
    public class ActorRegistryTests
    {
        [SetUp]
        public void SetUp() => ActorRegistry.Clear();

        [TearDown]
        public void TearDown() => ActorRegistry.Clear();

        [Test]
        public void Register_SetsLocalActor_ForLocalPlayerOnly()
        {
            var remote = new FakeActor(ownerId: 2, isLocal: false);
            var local = new FakeActor(ownerId: 1, isLocal: true);

            ActorRegistry.Register(remote);
            ActorRegistry.Register(local);

            Assert.AreSame(local, ActorRegistry.LocalActor);
            Assert.AreEqual(2, ActorRegistry.Actors.Count);
            Assert.AreSame(remote, ActorRegistry.GetByOwner(2));
        }

        [Test]
        public void Unregister_LocalActor_ClearsLocalActor()
        {
            var local = new FakeActor(ownerId: 1, isLocal: true);

            ActorRegistry.Register(local);
            ActorRegistry.Unregister(local);

            Assert.IsNull(ActorRegistry.LocalActor);
            Assert.AreEqual(0, ActorRegistry.Actors.Count);
        }

        [Test]
        public void Register_IsIdempotent()
        {
            var local = new FakeActor(ownerId: 1, isLocal: true);

            ActorRegistry.Register(local);
            ActorRegistry.Register(local);

            Assert.AreEqual(1, ActorRegistry.Actors.Count);
        }

        private sealed class FakeActor : IActor
        {
            public FakeActor(int ownerId, bool isLocal)
            {
                OwnerId = ownerId;
                IsLocalPlayer = isLocal;
            }

            public int OwnerId { get; }
            public bool IsLocalPlayer { get; }
            public string DisplayName => "Fake";
            public ICharacterSheet Sheet => null;
            public IPlayerDataService DataService => null;
            public Transform Transform => null;
        }
    }
}

using System.Collections.Generic;
using GameCore.PlayerData;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Locks in the JSON serialization round-trip for character data before refactoring.
    /// Uses string in/out only (no file I/O).
    /// </summary>
    public class PlayerDataJsonLoaderTests
    {
        private static DnD5eCharacterData MakeSampleCharacter()
        {
            var data = new DnD5eCharacterData
            {
                characterName = "Gideon",
                level = 5,
                characterClass = "Fighter",
                race = "Human",
                strength = 16,
                dexterity = 14,
                constitution = 15,
                intelligence = 10,
                wisdom = 12,
                charisma = 8,
                maxHitPoints = 44,
                currentHitPoints = 30,
                armorClass = 18,
                walkingSpeed = 30,
            };
            data.proficientSavingThrows.Add("STR");
            data.proficientSavingThrows.Add("CON");
            data.SetProficientSkills(new List<DnD5eSkill> { DnD5eSkill.Athletics, DnD5eSkill.Perception });
            data.SetExpertiseSkills(new List<DnD5eSkill> { DnD5eSkill.Athletics });
            return data;
        }

        [Test]
        public void RoundTrip_PreservesScalarFields()
        {
            DnD5eCharacterData original = MakeSampleCharacter();

            string json = PlayerDataJsonLoader.ToJson(original);
            DnD5eCharacterData loaded = PlayerDataJsonLoader.LoadFromJson(json);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Gideon", loaded.characterName);
            Assert.AreEqual(5, loaded.level);
            Assert.AreEqual("Fighter", loaded.characterClass);
            Assert.AreEqual(16, loaded.strength);
            Assert.AreEqual(44, loaded.maxHitPoints);
            Assert.AreEqual(30, loaded.currentHitPoints);
            Assert.AreEqual(18, loaded.armorClass);
        }

        [Test]
        public void RoundTrip_PreservesDerivedModifiers()
        {
            DnD5eCharacterData loaded = PlayerDataJsonLoader.LoadFromJson(
                PlayerDataJsonLoader.ToJson(MakeSampleCharacter()));

            Assert.AreEqual(3, loaded.strengthModifier);  // (16-10)/2
            Assert.AreEqual(3, loaded.proficiencyBonus);  // level 5
            Assert.AreEqual(2, loaded.initiativeModifier); // DEX 14 -> +2
        }

        [Test]
        public void RoundTrip_PreservesSavingThrowProficiencies()
        {
            DnD5eCharacterData loaded = PlayerDataJsonLoader.LoadFromJson(
                PlayerDataJsonLoader.ToJson(MakeSampleCharacter()));

            Assert.IsTrue(loaded.IsProficientInSavingThrow("STR"));
            Assert.IsTrue(loaded.IsProficientInSavingThrow("CON"));
            Assert.IsFalse(loaded.IsProficientInSavingThrow("DEX"));
        }

        [Test]
        public void RoundTrip_PreservesSkillProficiencyAndExpertise()
        {
            DnD5eCharacterData loaded = PlayerDataJsonLoader.LoadFromJson(
                PlayerDataJsonLoader.ToJson(MakeSampleCharacter()));

            Assert.IsTrue(loaded.IsProficientInSkill(DnD5eSkill.Athletics));
            Assert.IsTrue(loaded.IsProficientInSkill(DnD5eSkill.Perception));
            Assert.IsTrue(loaded.IsExpertInSkill(DnD5eSkill.Athletics));
            Assert.IsFalse(loaded.IsExpertInSkill(DnD5eSkill.Perception));
        }

        [Test]
        public void LoadFromJson_WithEmptyObject_ReturnsDefaults()
        {
            DnD5eCharacterData loaded = PlayerDataJsonLoader.LoadFromJson("{}");

            Assert.IsNotNull(loaded);
            Assert.AreEqual("", loaded.characterName);
            Assert.AreEqual(1, loaded.level);
            Assert.AreEqual(10, loaded.strength);
        }
    }
}

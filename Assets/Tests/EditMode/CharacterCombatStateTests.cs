using NUnit.Framework;
using GameCore.PlayerData;

namespace GameCore.Tests.EditMode
{
    public class CharacterCombatStateTests
    {
        [Test]
        public void FromSheet_AndApplyToSheet_RoundTrip()
        {
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 5,
                constitution = 14,
                currentHitPoints = 18,
                temporaryHitPoints = 5,
                deathSaveSuccesses = 1,
                deathSaveFailures = 2,
                exhaustionLevel = 3,
                hasInspiration = true,
                conditions = { "Poisoned", "Prone" },
            };

            CharacterCombatState state = CharacterCombatState.FromSheet(data);
            var copy = new DnD5eCharacterData
            {
                characterClass = data.characterClass,
                level = data.level,
                constitution = data.constitution,
                maxHitPoints = data.maxHitPoints,
            };
            state.ApplyToSheet(copy);

            Assert.AreEqual(18, copy.currentHitPoints);
            Assert.AreEqual(5, copy.temporaryHitPoints);
            Assert.AreEqual(1, copy.deathSaveSuccesses);
            Assert.AreEqual(2, copy.deathSaveFailures);
            Assert.AreEqual(3, copy.exhaustionLevel);
            Assert.IsTrue(copy.hasInspiration);
            CollectionAssert.Contains(copy.conditions, "Poisoned");
            CollectionAssert.Contains(copy.conditions, "Prone");
        }

        [Test]
        public void FromSheet_ClampsCurrentHitPointsToMax()
        {
            var data = new DnD5eCharacterData
            {
                characterClass = "Wizard",
                level = 3,
                constitution = 10,
                currentHitPoints = 999,
            };

            CharacterCombatState state = CharacterCombatState.FromSheet(data);
            int max = CharacterHitPoints.GetDisplayMaxHp(data);

            Assert.AreEqual(max, state.CurrentHitPoints);
        }
    }
}

using GameCore.PlayerData;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class CharacterHitPointsFullHealthTests
    {
        [Test]
        public void EnsureFullHealth_SetsCurrentToDerivedMax()
        {
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 3,
                constitution = 14,
                currentHitPoints = 5,
            };

            CharacterHitPoints.EnsureFullHealth(data);

            int expectedMax = CharacterHitPoints.GetDisplayMaxHp(data);
            Assert.AreEqual(expectedMax, data.currentHitPoints);
            Assert.AreEqual(expectedMax, data.maxHitPoints);
        }
    }
}

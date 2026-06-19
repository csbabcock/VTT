using NUnit.Framework;
using GameCore.PlayerData;

namespace GameCore.Tests.EditMode
{
    public class CharacterHitPointsTests
    {
        [Test]
        public void ClampCurrent_ClampsToZeroAndMax()
        {
            Assert.AreEqual(0, CharacterHitPoints.ClampCurrent(-5, 20));
            Assert.AreEqual(20, CharacterHitPoints.ClampCurrent(99, 20));
            Assert.AreEqual(7, CharacterHitPoints.ClampCurrent(7, 20));
        }

        [Test]
        public void ClampCurrent_FloorsMaxAtOne()
        {
            // Invalid/zero max is treated as at least 1 for clamping bounds.
            Assert.AreEqual(1, CharacterHitPoints.ClampCurrent(5, 0));
            Assert.AreEqual(0, CharacterHitPoints.ClampCurrent(0, 0));
            Assert.AreEqual(1, CharacterHitPoints.ClampCurrent(1, 0));
        }

        [Test]
        public void GetDisplayMaxHp_UsesClassLevelAndConstitution()
        {
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 5,
                constitution = 14,
            };

            int max = CharacterHitPoints.GetDisplayMaxHp(data);

            Assert.Greater(max, 0);
            Assert.AreEqual(max, CharacterHitPoints.GetDisplayMaxHp(data));
        }
    }
}

using NUnit.Framework;
using GameCore.PlayerData;

namespace GameCore.Tests.EditMode
{
    public class CharacterCombatStateRulesTests
    {
        [Test]
        public void ClampDeathSaveCount_ClampsToZeroAndThree()
        {
            Assert.AreEqual(0, CharacterCombatStateRules.ClampDeathSaveCount(-1));
            Assert.AreEqual(3, CharacterCombatStateRules.ClampDeathSaveCount(9));
            Assert.AreEqual(2, CharacterCombatStateRules.ClampDeathSaveCount(2));
        }

        [Test]
        public void ClampExhaustion_ClampsToZeroAndSix()
        {
            Assert.AreEqual(0, CharacterCombatStateRules.ClampExhaustion(-2));
            Assert.AreEqual(6, CharacterCombatStateRules.ClampExhaustion(99));
            Assert.AreEqual(4, CharacterCombatStateRules.ClampExhaustion(4));
        }

        [Test]
        public void ClampTemporaryHitPoints_FloorsAtZero()
        {
            Assert.AreEqual(0, CharacterCombatStateRules.ClampTemporaryHitPoints(-3));
            Assert.AreEqual(7, CharacterCombatStateRules.ClampTemporaryHitPoints(7));
        }
    }
}

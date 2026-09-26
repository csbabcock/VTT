using GameCore.Combat.Models;
using GameCore.UI.InGame.Services;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public sealed class CombatLogOutcomeTests
    {
        [TestCase(1)]
        [TestCase(8)]
        public void Miss_IsVisibleInRollCard_WithRollTotalPreserved(int naturalRoll)
        {
            var outcome = AttackOutcome.Miss(naturalRoll, naturalRoll + 3, 16);
            var result = CombatActionResult.Completed(outcome, "Attacker", "Target", "Unarmed Strike");
            var entry = new GameLogService().FormatCombatAttackRoll(result);
            Assert.AreEqual("TO HIT - MISS", entry.SubActionType);
            Assert.AreEqual(naturalRoll + 3, entry.Result);
            Assert.AreEqual("log-attack-miss", entry.CssClass);
            StringAssert.Contains("MISS", entry.FullMessage);
        }

        [TestCase(false, "TO HIT - HIT")]
        [TestCase(true, "TO HIT - CRITICAL HIT")]
        public void Hit_IsVisibleInRollCard(bool critical, string label)
        {
            var outcome = AttackOutcome.Hit(13, critical, 20, 23, 16);
            var result = CombatActionResult.Completed(outcome, "Attacker", "Target", "Unarmed Strike");
            var entry = new GameLogService().FormatCombatAttackRoll(result);
            Assert.AreEqual(label, entry.SubActionType);
            Assert.AreEqual(23, entry.Result);
        }

        [Test]
        public void RejectedAttack_IsNotLabeledAsMiss()
        {
            var result = CombatActionResult.Failed(CombatFailureReason.OutOfRange, "Attacker", "Target", "Strike");
            var entry = new GameLogService().FormatCombatAttackRoll(result);
            Assert.AreNotEqual("TO HIT - MISS", entry.SubActionType);
            StringAssert.Contains("out of melee range", entry.FullMessage);
        }
    }
}

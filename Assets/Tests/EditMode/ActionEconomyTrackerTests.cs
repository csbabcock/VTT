using GameCore.Combat;
using GameCore.Combat.ActionEconomy;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class ActionEconomyTrackerTests
    {
        [Test]
        public void TrySpend_Action_OnlyOncePerTurn()
        {
            var tracker = new ActionEconomyTracker();

            Assert.IsTrue(tracker.TrySpend(ActionCostKind.Action));
            Assert.IsFalse(tracker.TrySpend(ActionCostKind.Action));
        }

        [Test]
        public void TrySpend_None_DoesNotBlockAction()
        {
            var tracker = new ActionEconomyTracker();

            Assert.IsTrue(tracker.TrySpend(ActionCostKind.None));
            Assert.IsTrue(tracker.TrySpend(ActionCostKind.Action));
        }

        [Test]
        public void ResetForNewTurn_RestoresAction()
        {
            var tracker = new ActionEconomyTracker();
            tracker.TrySpend(ActionCostKind.Action);

            tracker.ResetForNewTurn();

            Assert.IsTrue(tracker.CanSpend(ActionCostKind.Action));
        }

        [Test]
        public void BonusActionAndReaction_AreIndependent()
        {
            var tracker = new ActionEconomyTracker();

            Assert.IsTrue(tracker.TrySpend(ActionCostKind.BonusAction));
            Assert.IsTrue(tracker.TrySpend(ActionCostKind.Reaction));
            Assert.IsTrue(tracker.TrySpend(ActionCostKind.Action));
        }
    }
}

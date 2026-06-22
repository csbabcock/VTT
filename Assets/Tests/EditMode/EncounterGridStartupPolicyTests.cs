using GameCore.EncounterMode.Services;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class EncounterGridStartupPolicyTests
    {
        [Test]
        public void ResolveStartupAction_ShowsGrid_WhenEncounterAlreadyActive()
        {
            Assert.AreEqual(
                EncounterGridStartupAction.ShowForActiveEncounter,
                EncounterGridStartupPolicy.ResolveStartupAction(isEncounterActive: true));
        }

        [Test]
        public void ResolveStartupAction_HidesGrid_WhenEncounterInactive()
        {
            Assert.AreEqual(
                EncounterGridStartupAction.HideUntilEncounter,
                EncounterGridStartupPolicy.ResolveStartupAction(isEncounterActive: false));
        }

        [Test]
        public void ShouldRefreshPresentation_ReturnsTrueOnlyWhenEncounterActive()
        {
            Assert.IsTrue(EncounterGridStartupPolicy.ShouldRefreshPresentation(isEncounterActive: true));
            Assert.IsFalse(EncounterGridStartupPolicy.ShouldRefreshPresentation(isEncounterActive: false));
        }
    }
}

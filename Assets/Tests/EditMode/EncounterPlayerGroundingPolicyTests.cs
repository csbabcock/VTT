using GameCore.EncounterMode.Services;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class EncounterPlayerGroundingPolicyTests
    {
        [Test]
        public void ShouldApplyIdleGravity_WhenEncounterIdle()
        {
            Assert.IsTrue(EncounterPlayerGroundingPolicy.ShouldApplyIdleGravity(
                isEncounterMovementMode: true,
                isMovingOnGrid: false));
        }

        [Test]
        public void ShouldApplyIdleGravity_FalseWhileMovingOnGrid()
        {
            Assert.IsFalse(EncounterPlayerGroundingPolicy.ShouldApplyIdleGravity(
                isEncounterMovementMode: true,
                isMovingOnGrid: true));
        }

        [Test]
        public void ShouldApplyIdleGravity_FalseOutsideEncounterMode()
        {
            Assert.IsFalse(EncounterPlayerGroundingPolicy.ShouldApplyIdleGravity(
                isEncounterMovementMode: false,
                isMovingOnGrid: false));
        }
    }
}

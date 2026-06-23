using GameCore.Combat.Targeting;
using GameCore.EncounterMode.Grid;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class MeleeReachValidatorTests
    {
        [Test]
        public void IsWithinMeleeReachFeet_AllowsFiveFeet()
        {
            Assert.IsTrue(MeleeReachValidator.IsWithinMeleeReachFeet(5));
            Assert.IsFalse(MeleeReachValidator.IsWithinMeleeReachFeet(10));
        }

        [Test]
        public void IsWithinMeleeReachCells_AllowsAdjacentCell()
        {
            var from = new GridCell(0, 0, Vector3.zero);
            var adjacent = new GridCell(1, 0, Vector3.zero);
            var twoAway = new GridCell(2, 0, Vector3.zero);

            Assert.IsTrue(MeleeReachValidator.IsWithinMeleeReachCells(from, adjacent));
            Assert.IsFalse(MeleeReachValidator.IsWithinMeleeReachCells(from, twoAway));
        }

        [Test]
        public void IsWithinMeleeReachWorld_UsesHorizontalDistance()
        {
            Assert.IsTrue(MeleeReachValidator.IsWithinMeleeReachWorld(
                Vector3.zero,
                new Vector3(1.5f, 0f, 0f),
                feetPerWorldUnit: 1.524f));

            Assert.IsFalse(MeleeReachValidator.IsWithinMeleeReachWorld(
                Vector3.zero,
                new Vector3(3f, 0f, 0f),
                feetPerWorldUnit: 1.524f));
        }
    }
}

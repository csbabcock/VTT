using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class EncounterPathPlannerTests
    {
        private const float CellSize = 2f;

        [Test]
        public void CalculateTargetPosition_GroundLevel_UsesCellWorldHeight()
        {
            var cell = new GridCell(0, 0, new Vector3(5, 2, 7));

            Vector3 pos = EncounterPathPlanner.CalculateTargetPosition(cell, 0, CellSize);

            Assert.AreEqual(new Vector3(5, 2, 7), pos);
        }

        [Test]
        public void CalculateTargetPosition_Elevated_AddsElevationHeight()
        {
            var cell = new GridCell(0, 0, new Vector3(5, 2, 7));

            Vector3 pos = EncounterPathPlanner.CalculateTargetPosition(cell, 2, CellSize);

            Assert.AreEqual(new Vector3(5, 6, 7), pos); // 2 + 2 * 2
        }

        [Test]
        public void HorizontalDistance_IgnoresVertical()
        {
            float dist = EncounterPathPlanner.HorizontalDistance(new Vector3(0, 5, 0), new Vector3(3, 99, 4));

            Assert.AreEqual(5f, dist, 0.0001f);
        }

        [Test]
        public void CalculateDiagonalDirection_ReturnsNormalizedDirection()
        {
            Vector3 dir = EncounterPathPlanner.CalculateDiagonalDirection(Vector3.zero, new Vector3(3, 0, 4));

            Assert.AreEqual(0.6f, dir.x, 0.0001f);
            Assert.AreEqual(0f, dir.y, 0.0001f);
            Assert.AreEqual(0.8f, dir.z, 0.0001f);
        }

        [Test]
        public void CalculateDiagonalDirection_Coincident_ReturnsZero()
        {
            Vector3 dir = EncounterPathPlanner.CalculateDiagonalDirection(Vector3.one, Vector3.one);

            Assert.AreEqual(Vector3.zero, dir);
        }

        [Test]
        public void HasMovedTowardTarget_WhenProgressing_IsTrue()
        {
            bool moved = EncounterPathPlanner.HasMovedTowardTarget(
                new Vector3(1, 0, 0), Vector3.zero, new Vector3(10, 0, 0));

            Assert.IsTrue(moved);
        }

        [Test]
        public void HasMovedTowardTarget_WhenBarelyMoved_IsFalse()
        {
            bool moved = EncounterPathPlanner.HasMovedTowardTarget(
                new Vector3(0.01f, 0, 0), Vector3.zero, new Vector3(10, 0, 0));

            Assert.IsFalse(moved);
        }

        [Test]
        public void HasMovedTowardTarget_WhenMovingAway_IsFalse()
        {
            bool moved = EncounterPathPlanner.HasMovedTowardTarget(
                new Vector3(-1, 0, 0), Vector3.zero, new Vector3(10, 0, 0));

            Assert.IsFalse(moved);
        }

        [Test]
        public void IsWithinArrivalThreshold_GroundLevel_WithinTolerance_IsTrue()
        {
            bool arrived = EncounterPathPlanner.IsWithinArrivalThreshold(
                Vector3.zero, new Vector3(0.5f, 0, 0), 0, CellSize);

            Assert.IsTrue(arrived);
        }

        [Test]
        public void IsWithinArrivalThreshold_GroundLevel_TooFar_IsFalse()
        {
            bool arrived = EncounterPathPlanner.IsWithinArrivalThreshold(
                Vector3.zero, new Vector3(1.5f, 0, 0), 0, CellSize);

            Assert.IsFalse(arrived);
        }

        [Test]
        public void IsWithinArrivalThreshold_GroundLevel_TargetBelow_IsFalse()
        {
            bool arrived = EncounterPathPlanner.IsWithinArrivalThreshold(
                Vector3.zero, new Vector3(0.5f, -0.5f, 0), 0, CellSize);

            Assert.IsFalse(arrived);
        }

        [Test]
        public void IsWithinArrivalThreshold_Elevated_WithinSymmetricTolerance_IsTrue()
        {
            bool arrived = EncounterPathPlanner.IsWithinArrivalThreshold(
                Vector3.zero, new Vector3(0.5f, 0.3f, 0), 1, CellSize);

            Assert.IsTrue(arrived);
        }

        [Test]
        public void IsWithinArrivalThreshold_Elevated_OutsideVertical_IsFalse()
        {
            bool arrived = EncounterPathPlanner.IsWithinArrivalThreshold(
                Vector3.zero, new Vector3(0.5f, 0.6f, 0), 1, CellSize);

            Assert.IsFalse(arrived);
        }
    }
}

using GameCore.Combat.Targeting;
using GameCore.EncounterMode.Grid;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class MeleeApproachPositionsTests
    {
        private const float CellSize = 1.524f;
        private const float BodyRadius = 0.25f;
        private const float ExpectedStandoff = BodyRadius * 2f + 0.08f;

        [Test]
        public void ResolveGridMeleeApproachPosition_PlacesAttackerAtContactStandoff()
        {
            var attackerPosition = new Vector3(0f, 0f, 0f);
            var targetPosition = new Vector3(10f, 0f, 0f);
            var approachCell = new GridCell(1, 0, new Vector3(5f, 0f, 0f));

            Vector3 approach = MeleeApproachPositions.ResolveGridMeleeApproachPosition(
                attackerPosition,
                targetPosition,
                approachCell,
                ExpectedStandoff);

            float distance = HorizontalDistance(approach, targetPosition);
            Assert.LessOrEqual(distance, ExpectedStandoff + 0.01f);
            Assert.Greater(distance, ExpectedStandoff - 0.01f);
        }

        [Test]
        public void ResolveFreeMeleeApproachPosition_PlacesAttackerAtContactStandoff()
        {
            var attacker = new Vector3(0f, 0f, 0f);
            var target = new Vector3(10f, 0f, 0f);

            Vector3 approach = MeleeApproachPositions.ResolveFreeMeleeApproachPosition(
                attacker,
                target,
                ExpectedStandoff);

            float distance = HorizontalDistance(approach, target);
            Assert.LessOrEqual(distance, ExpectedStandoff + 0.01f);
            Assert.Greater(distance, ExpectedStandoff - 0.01f);
        }

        [Test]
        public void ComputeApproachStandoff_UsesBodyRadiiInsteadOfFullCellWidth()
        {
            var attacker = new GameObject("attacker").transform;
            var target = new GameObject("target").transform;
            attacker.gameObject.AddComponent<CharacterController>().radius = BodyRadius;
            target.gameObject.AddComponent<CharacterController>().radius = BodyRadius;

            float standoff = MeleeStandoff.ComputeApproachStandoff(attacker, target, CellSize);

            Assert.Less(standoff, CellSize);
            Assert.AreEqual(ExpectedStandoff, standoff, 0.001f);

            Object.DestroyImmediate(attacker.gameObject);
            Object.DestroyImmediate(target.gameObject);
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            float dx = from.x - to.x;
            float dz = from.z - to.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}

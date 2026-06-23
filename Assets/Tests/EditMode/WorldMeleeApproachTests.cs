using GameCore.Combat.Targeting;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class WorldMeleeApproachTests
    {
        [Test]
        public void TrySnapIntoRange_MovesAttackerAdjacent()
        {
            var attacker = new GameObject("attacker").transform;
            var target = new GameObject("target").transform;
            attacker.position = new Vector3(0f, 0f, 0f);
            target.position = new Vector3(5f, 0f, 0f);

            Assert.IsTrue(WorldMeleeApproach.TrySnapIntoRange(attacker, target, meleeRangeWorldUnits: 1.524f));
            Assert.LessOrEqual(Vector3.Distance(attacker.position, target.position), 1.524f + 0.1f);

            Object.DestroyImmediate(attacker.gameObject);
            Object.DestroyImmediate(target.gameObject);
        }
    }
}

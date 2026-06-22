using NUnit.Framework;
using GameCore.DmTools;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class DmFlyCameraMathTests
    {
        [Test]
        public void ScaleSpeed_AppliesFastMultiplierWhenRequested()
        {
            Assert.AreEqual(10f, DmFlyCameraMath.ScaleSpeed(10f, false, 3f));
            Assert.AreEqual(30f, DmFlyCameraMath.ScaleSpeed(10f, true, 3f));
        }

        [Test]
        public void ComputeFlyMoveDelta_NormalizesDiagonalInput()
        {
            var rotation = Quaternion.identity;
            Vector3 delta = DmFlyCameraMath.ComputeFlyMoveDelta(new Vector2(1f, 1f), 0f, rotation, 10f, 1f);
            Assert.AreEqual(10f, delta.magnitude, 0.001f);
        }

        [Test]
        public void ApplyPan_MovesOppositeMouseDeltaInCameraSpace()
        {
            var rotation = Quaternion.LookRotation(Vector3.forward);
            Vector3 position = DmFlyCameraMath.ApplyPan(Vector3.zero, rotation, new Vector2(10f, 0f), 0.01f);
            Assert.Less(position.x, 0f);
        }

        [Test]
        public void ApplyZoom_MovesForwardAlongCameraFacing()
        {
            var rotation = Quaternion.LookRotation(Vector3.forward);
            Vector3 position = DmFlyCameraMath.ApplyZoom(Vector3.zero, rotation, 2f);
            Assert.AreEqual(2f, position.z, 0.001f);
        }
    }
}

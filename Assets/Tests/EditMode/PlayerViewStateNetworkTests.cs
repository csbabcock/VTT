using GameCore.DmTools;
using GameCore.Networking;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class PlayerViewStateNetworkTests
    {
        [Test]
        public void FromCamera_CapturesPoseAndProjection()
        {
            var cameraObject = new GameObject("TestCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(10f, 20f, 0f));
            camera.fieldOfView = 55f;
            camera.orthographic = false;

            PlayerViewStateNetwork state = PlayerViewStateNetwork.FromCamera(camera);

            Assert.AreEqual(new Vector3(1f, 2f, 3f), state.Position);
            Assert.AreEqual(Quaternion.Euler(10f, 20f, 0f), state.Rotation);
            Assert.AreEqual(55f, state.FieldOfView);
            Assert.IsFalse(state.IsOrthographic);

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void ApplyTo_RestoresPoseAndProjection()
        {
            var sourceObject = new GameObject("SourceCamera");
            var source = sourceObject.AddComponent<Camera>();
            source.transform.SetPositionAndRotation(new Vector3(4f, 5f, 6f), Quaternion.Euler(-15f, 45f, 0f));
            source.fieldOfView = 70f;

            var targetObject = new GameObject("TargetCamera");
            var target = targetObject.AddComponent<Camera>();

            PlayerViewStateNetwork.FromCamera(source).ApplyTo(target);

            Assert.AreEqual(source.transform.position, target.transform.position);
            Assert.AreEqual(source.transform.rotation, target.transform.rotation);
            Assert.AreEqual(source.fieldOfView, target.fieldOfView);
            Assert.AreEqual(source.orthographic, target.orthographic);

            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(targetObject);
        }
    }

    public class DmPlayerSpectateLocatorTests
    {
        private bool _wasSpectating;
        private int _previousOwnerId;

        [SetUp]
        public void SetUp()
        {
            _wasSpectating = DmPlayerSpectateLocator.IsSpectating;
            _previousOwnerId = DmPlayerSpectateLocator.SpectatedOwnerId;
        }

        [TearDown]
        public void TearDown()
        {
            if (_wasSpectating)
                DmPlayerSpectateLocator.SetSpectating(_previousOwnerId);
            else
                DmPlayerSpectateLocator.Clear();
        }

        [Test]
        public void SetSpectating_TracksOwnerUntilCleared()
        {
            DmPlayerSpectateLocator.SetSpectating(7);

            Assert.IsTrue(DmPlayerSpectateLocator.IsSpectating);
            Assert.AreEqual(7, DmPlayerSpectateLocator.SpectatedOwnerId);

            DmPlayerSpectateLocator.Clear();

            Assert.IsFalse(DmPlayerSpectateLocator.IsSpectating);
            Assert.AreEqual(-1, DmPlayerSpectateLocator.SpectatedOwnerId);
        }
    }

    public class DmPlayerSpectateGatewayTests
    {
        [Test]
        public void CanSpectateOwner_ReturnsFalse_WhenNoProviderRegistered()
        {
            Assert.IsFalse(DmPlayerSpectateGateway.CanSpectateOwner(1));
        }
    }
}

using NUnit.Framework;
using GameCore.Networking;

namespace GameCore.Tests.EditMode
{
    public class NetworkPlayerSpawnPolicyTests
    {
        [Test]
        public void ShouldSpawnPlayerObject_ReturnsFalse_ForHostClient()
        {
            Assert.IsFalse(NetworkPlayerSpawnPolicy.ShouldSpawnPlayerObject(0, 0));
        }

        [Test]
        public void ShouldSpawnPlayerObject_ReturnsTrue_ForNonHostClients()
        {
            Assert.IsTrue(NetworkPlayerSpawnPolicy.ShouldSpawnPlayerObject(1, 0));
            Assert.IsTrue(NetworkPlayerSpawnPolicy.ShouldSpawnPlayerObject(4, 0));
        }
    }
}

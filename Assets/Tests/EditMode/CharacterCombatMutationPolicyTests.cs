using GameCore.Actors;
using GameCore.PlayerData;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class CharacterCombatMutationPolicyTests
    {
        [Test]
        public void CanMutate_AllowsDungeonMaster()
        {
            Assert.IsTrue(CharacterCombatMutationPolicy.CanMutate(isDungeonMaster: true, isLocalOwner: false));
        }

        [Test]
        public void CanMutate_AllowsLocalOwner()
        {
            Assert.IsTrue(CharacterCombatMutationPolicy.CanMutate(isDungeonMaster: false, isLocalOwner: true));
        }

        [Test]
        public void CanMutate_DeniesNonOwnerPlayer()
        {
            Assert.IsFalse(CharacterCombatMutationPolicy.CanMutate(isDungeonMaster: false, isLocalOwner: false));
        }

        [Test]
        public void IsLocalOwner_ReturnsTrue_WhenActorMatchesRegistryLocalActor()
        {
            ActorRegistry.Clear();
            var local = new FakeActor(1, isLocal: true);
            var remote = new FakeActor(2, isLocal: false);
            ActorRegistry.Register(local);

            Assert.IsTrue(CharacterCombatMutationPolicy.IsLocalOwner(local));
            Assert.IsFalse(CharacterCombatMutationPolicy.IsLocalOwner(remote));
            ActorRegistry.Clear();
        }

        [Test]
        public void IsLocalOwner_ReturnsTrue_ForLocalPlayerBeforeRegistryAssignment()
        {
            ActorRegistry.Clear();
            var local = new FakeActor(1, isLocal: true);

            Assert.IsTrue(CharacterCombatMutationPolicy.IsLocalOwner(local));
            ActorRegistry.Clear();
        }

        private sealed class FakeActor : IActor
        {
            public FakeActor(int ownerId, bool isLocal)
            {
                OwnerId = ownerId;
                IsLocalPlayer = isLocal;
            }

            public int OwnerId { get; }
            public bool IsLocalPlayer { get; }
            public string DisplayName => "Test";
            public ICharacterSheet Sheet => null;
            public IPlayerDataService DataService => null;
            public UnityEngine.Transform Transform => null;
        }
    }
}

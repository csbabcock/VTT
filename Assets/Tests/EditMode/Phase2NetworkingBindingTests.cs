using NUnit.Framework;
using GameCore.Actors;
using GameCore.Networking;
using GameCore.PlayerData;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Covers the netcode-agnostic seams added in Phase 2: ownership re-evaluation in
    /// the actor registry, the in-memory data service used for network-received
    /// characters, and the local session role.
    /// </summary>
    public class ActorOwnershipReassignmentTests
    {
        [SetUp]
        public void SetUp() => ActorRegistry.Clear();

        [TearDown]
        public void TearDown() => ActorRegistry.Clear();

        [Test]
        public void NotifyOwnershipChanged_FixesLocalActor_WhenRemoteRegistersFirstAsDefaultLocal()
        {
            // Mirrors networking order: actors register (OnEnable) with the default
            // "local" flag before the spawner assigns real ownership (OnNetworkSpawn).
            var remote = new MutableFakeActor(ownerId: 2, isLocal: true);
            var local = new MutableFakeActor(ownerId: 1, isLocal: true);

            ActorRegistry.Register(remote);
            ActorRegistry.Register(local);

            // The remote actor wrongly held the local slot at registration time.
            Assert.AreSame(remote, ActorRegistry.LocalActor);

            // Ownership is now assigned by the spawner.
            remote.IsLocalPlayer = false;
            ActorRegistry.NotifyOwnershipChanged(remote);

            local.IsLocalPlayer = true;
            ActorRegistry.NotifyOwnershipChanged(local);

            Assert.AreSame(local, ActorRegistry.LocalActor);
        }

        [Test]
        public void NotifyOwnershipChanged_IgnoresUnregisteredActor()
        {
            var stray = new MutableFakeActor(ownerId: 9, isLocal: true);

            ActorRegistry.NotifyOwnershipChanged(stray);

            Assert.IsNull(ActorRegistry.LocalActor);
        }

        private sealed class MutableFakeActor : IActor
        {
            public MutableFakeActor(int ownerId, bool isLocal)
            {
                OwnerId = ownerId;
                IsLocalPlayer = isLocal;
            }

            public int OwnerId { get; }
            public bool IsLocalPlayer { get; set; }
            public string DisplayName => "Fake";
            public ICharacterSheet Sheet => null;
            public IPlayerDataService DataService => null;
            public Transform Transform => null;
        }
    }

    public class InMemoryPlayerDataServiceTests
    {
        [Test]
        public void WrapsProvidedCharacter_AndExposesSheet()
        {
            var data = new DnD5eCharacterData
            {
                characterName = "Gimli",
                level = 3,
                strength = 18,
            };

            var service = new InMemoryPlayerDataService(data);

            ICharacterSheet sheet = service.GetCharacterSheet();
            Assert.IsNotNull(sheet);
            Assert.AreEqual("Gimli", sheet.CharacterName);
            Assert.AreEqual(18, sheet.GetAbilityScore("STR"));
            Assert.AreEqual(18, service.GetPlayerData().Strength);
        }

        [Test]
        public void NullCharacter_FallsBackToDefault()
        {
            var service = new InMemoryPlayerDataService(null);

            Assert.IsNotNull(service.GetCharacterSheet());
            Assert.IsNotNull(service.GetPlayerData());
        }
    }

    public class SessionRoleLocatorTests
    {
        private SessionRole _previous;

        [SetUp]
        public void SetUp() => _previous = SessionRoleLocator.LocalRole;

        [TearDown]
        public void TearDown() => SessionRoleLocator.LocalRole = _previous;

        [Test]
        public void IsDungeonMaster_ReflectsRole()
        {
            SessionRoleLocator.LocalRole = SessionRole.Player;
            Assert.IsFalse(SessionRoleLocator.IsDungeonMaster);

            SessionRoleLocator.LocalRole = SessionRole.DungeonMaster;
            Assert.IsTrue(SessionRoleLocator.IsDungeonMaster);
        }
    }
}

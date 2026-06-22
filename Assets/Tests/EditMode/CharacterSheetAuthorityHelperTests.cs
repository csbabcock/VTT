using GameCore.Actors;
using GameCore.Networking;
using GameCore.PlayerData;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class CharacterSheetAuthorityHelperTests
    {
        private SessionRole _previousRole;

        [SetUp]
        public void SetUp()
        {
            _previousRole = SessionRoleLocator.LocalRole;
            ActorRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SessionRoleLocator.LocalRole = _previousRole;
            ActorRegistry.Clear();
        }

        [Test]
        public void TryGetMutableAuthority_ReturnsAuthority_ForDungeonMasterOnRemoteActor()
        {
            SessionRoleLocator.LocalRole = SessionRole.DungeonMaster;
            var actor = CreateActor(ownerId: 2, isLocal: false);

            var authority = CharacterSheetAuthorityHelper.TryGetMutableAuthority(actor);

            Assert.NotNull(authority);
        }

        [Test]
        public void TryGetMutableAuthority_ReturnsAuthority_ForLocalOwnerPlayer()
        {
            SessionRoleLocator.LocalRole = SessionRole.Player;
            var actor = CreateActor(ownerId: 1, isLocal: true);
            ActorRegistry.Register(actor);

            var authority = CharacterSheetAuthorityHelper.TryGetMutableAuthority(actor);

            Assert.NotNull(authority);
        }

        [Test]
        public void TryGetMutableAuthority_ReturnsNull_ForNonOwnerPlayer()
        {
            SessionRoleLocator.LocalRole = SessionRole.Player;
            var local = CreateActor(ownerId: 1, isLocal: true);
            var remote = CreateActor(ownerId: 2, isLocal: false);
            ActorRegistry.Register(local);

            var authority = CharacterSheetAuthorityHelper.TryGetMutableAuthority(remote);

            Assert.IsNull(authority);
        }

        [Test]
        public void TryGetMutableAuthority_MutationSucceeds_ForPermittedCaller()
        {
            SessionRoleLocator.LocalRole = SessionRole.Player;
            var actor = CreateActor(ownerId: 1, isLocal: true);
            ActorRegistry.Register(actor);

            var authority = CharacterSheetAuthorityHelper.TryGetMutableAuthority(actor);
            authority.RequestAdjustCurrentHitPoints(-2);

            Assert.AreEqual(8, authority.CurrentHitPoints);
        }

        private static IActor CreateActor(int ownerId, bool isLocal)
        {
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 1,
                constitution = 10,
                currentHitPoints = 10,
            };
            var service = new InMemoryPlayerDataService(data);
            return new FakeActor(ownerId, isLocal, service);
        }

        private sealed class FakeActor : IActor
        {
            public FakeActor(int ownerId, bool isLocal, IPlayerDataService dataService)
            {
                OwnerId = ownerId;
                IsLocalPlayer = isLocal;
                DataService = dataService;
            }

            public int OwnerId { get; }
            public bool IsLocalPlayer { get; }
            public string DisplayName => "Test";
            public ICharacterSheet Sheet => DataService?.GetCharacterSheet();
            public IPlayerDataService DataService { get; }
            public UnityEngine.Transform Transform => null;
        }
    }
}

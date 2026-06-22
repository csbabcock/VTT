using NUnit.Framework;
using GameCore.Actors;
using GameCore.Networking;
using GameCore.PlayerData;

namespace GameCore.Tests.EditMode
{
    public class DirectCharacterSheetAuthorityTests
    {
        private SessionRole _previousRole;

        [SetUp]
        public void SetUp() => _previousRole = SessionRoleLocator.LocalRole;

        [TearDown]
        public void TearDown()
        {
            SessionRoleLocator.LocalRole = _previousRole;
            ActorRegistry.Clear();
        }

        [Test]
        public void RequestAdjustCurrentHitPoints_MutatesSheet_WhenDungeonMaster()
        {
            SessionRoleLocator.LocalRole = SessionRole.DungeonMaster;
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 3,
                constitution = 14,
                currentHitPoints = 20,
            };
            var service = new InMemoryPlayerDataService(data);
            var authority = new DirectCharacterSheetAuthority(data, service);

            authority.RequestAdjustCurrentHitPoints(-5);

            Assert.AreEqual(15, data.currentHitPoints);
            Assert.AreEqual(15, authority.CurrentHitPoints);
        }

        [Test]
        public void RequestToggleCondition_UpdatesFlags_WhenDungeonMaster()
        {
            SessionRoleLocator.LocalRole = SessionRole.DungeonMaster;
            var data = new DnD5eCharacterData { characterClass = "Fighter", level = 1, constitution = 10 };
            var service = new InMemoryPlayerDataService(data);
            var authority = new DirectCharacterSheetAuthority(data, service);

            authority.RequestToggleCondition("Poisoned");
            Assert.IsTrue(DnD5eConditions.Has(authority.ConditionFlags, "Poisoned"));

            authority.RequestToggleCondition("Poisoned");
            Assert.IsFalse(DnD5eConditions.Has(authority.ConditionFlags, "Poisoned"));
        }

        [Test]
        public void RequestAdjustCurrentHitPoints_IsIgnored_WhenNotDungeonMasterOrOwner()
        {
            SessionRoleLocator.LocalRole = SessionRole.Player;
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 3,
                constitution = 14,
                currentHitPoints = 20,
            };
            var service = new InMemoryPlayerDataService(data);
            var authority = new DirectCharacterSheetAuthority(data, service);

            authority.RequestAdjustCurrentHitPoints(-5);

            Assert.AreEqual(20, data.currentHitPoints);
        }

        [Test]
        public void RequestAdjustCurrentHitPoints_MutatesSheet_WhenLocalOwner()
        {
            SessionRoleLocator.LocalRole = SessionRole.Player;
            ActorRegistry.Clear();

            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 3,
                constitution = 14,
                currentHitPoints = 20,
            };
            var service = new InMemoryPlayerDataService(data);
            var actor = new FakeActor(1, service);
            ActorRegistry.Register(actor);

            var authority = new DirectCharacterSheetAuthority(data, service, actor);
            authority.RequestAdjustCurrentHitPoints(-5);

            Assert.AreEqual(15, data.currentHitPoints);
            ActorRegistry.Clear();
        }

        private sealed class FakeActor : IActor
        {
            public FakeActor(int ownerId, IPlayerDataService dataService)
            {
                OwnerId = ownerId;
                DataService = dataService;
            }

            public int OwnerId { get; }
            public bool IsLocalPlayer => true;
            public string DisplayName => "Test";
            public ICharacterSheet Sheet => DataService?.GetCharacterSheet();
            public IPlayerDataService DataService { get; }
            public UnityEngine.Transform Transform => null;
        }

        [Test]
        public void RequestSetDeathSaves_ClampsCounts()
        {
            SessionRoleLocator.LocalRole = SessionRole.DungeonMaster;
            var data = new DnD5eCharacterData { characterClass = "Fighter", level = 1, constitution = 10 };
            var service = new InMemoryPlayerDataService(data);
            var authority = new DirectCharacterSheetAuthority(data, service);

            authority.RequestSetDeathSaves(9, -2);

            Assert.AreEqual(3, authority.DeathSaveSuccesses);
            Assert.AreEqual(0, authority.DeathSaveFailures);
        }
    }
}

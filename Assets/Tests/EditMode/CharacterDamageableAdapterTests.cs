using GameCore.Actors;
using GameCore.Combat.Adapters;
using GameCore.Networking;
using GameCore.PlayerData;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class CharacterDamageableAdapterTests
    {
        private SessionRole _previousRole;

        [SetUp]
        public void SetUp() => _previousRole = SessionRoleLocator.LocalRole;

        [TearDown]
        public void TearDown() => SessionRoleLocator.LocalRole = _previousRole;

        [Test]
        public void ApplyDamage_ReducesCurrentHitPoints()
        {
            SessionRoleLocator.LocalRole = SessionRole.DungeonMaster;
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 3,
                constitution = 14,
                currentHitPoints = 20,
                armorClass = 15,
            };
            var service = new InMemoryPlayerDataService(data);
            var authority = new DirectCharacterSheetAuthority(data, service);
            var target = new FakeActor(2, service);
            var attacker = new FakeActor(1, service);
            var damageable = new CharacterDamageableAdapter(
                authority,
                target,
                attacker,
                () => data.armorClass,
                data.characterName);

            damageable.ApplyDamage(5);

            Assert.AreEqual(15, damageable.CurrentHitPoints);
            Assert.AreEqual(15, authority.CurrentHitPoints);
            Assert.AreEqual(15, data.currentHitPoints);
        }

        [Test]
        public void IsDestroyed_WhenHitPointsReachZero()
        {
            SessionRoleLocator.LocalRole = SessionRole.DungeonMaster;
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 1,
                constitution = 10,
                currentHitPoints = 3,
                armorClass = 10,
            };
            var service = new InMemoryPlayerDataService(data);
            var authority = new DirectCharacterSheetAuthority(data, service);
            var target = new FakeActor(2, service);
            var attacker = new FakeActor(1, service);
            var damageable = new CharacterDamageableAdapter(
                authority,
                target,
                attacker,
                () => data.armorClass,
                data.characterName);

            damageable.ApplyDamage(3);

            Assert.IsTrue(damageable.IsDestroyed);
        }

        private sealed class FakeActor : IActor
        {
            public FakeActor(int ownerId, IPlayerDataService dataService)
            {
                OwnerId = ownerId;
                DataService = dataService;
            }

            public int OwnerId { get; }
            public bool IsLocalPlayer => OwnerId == 1;
            public string DisplayName => Sheet?.CharacterName ?? $"Player {OwnerId}";
            public ICharacterSheet Sheet => DataService?.GetCharacterSheet();
            public IPlayerDataService DataService { get; }
            public UnityEngine.Transform Transform => null;
        }
    }
}

using GameCore.Actors;
using GameCore.Combat.Services;
using GameCore.Networking;
using GameCore.PlayerData;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class CombatSheetMutatorTests
    {
        [Test]
        public void TryApplyDamage_ReducesTargetHp_WhenLocalAttackerOffline()
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
            var target = new FakeActor(ownerId: 2, service, isLocal: false);
            var attacker = new FakeActor(ownerId: 1, service, isLocal: true);

            Assert.IsTrue(CombatSheetMutator.TryApplyDamage(target, attacker, 5));
            Assert.AreEqual(15, data.currentHitPoints);
        }

        private sealed class FakeActor : IActor
        {
            public FakeActor(int ownerId, IPlayerDataService service, bool isLocal)
            {
                OwnerId = ownerId;
                DataService = service;
                IsLocalPlayer = isLocal;
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

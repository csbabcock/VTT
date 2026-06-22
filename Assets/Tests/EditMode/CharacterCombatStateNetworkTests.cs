using NUnit.Framework;
using GameCore.Networking;
using GameCore.PlayerData;

namespace GameCore.Tests.EditMode
{
    public class CharacterCombatStateNetworkTests
    {
        [Test]
        public void FromCore_AndToCore_PreservesValues()
        {
            var core = new CharacterCombatState
            {
                CurrentHitPoints = 12,
                TemporaryHitPoints = 4,
                DeathSaveSuccesses = 1,
                DeathSaveFailures = 2,
                ConditionFlags = (uint)DnD5eConditionFlags.Poisoned,
                ExhaustionLevel = 3,
                HasInspiration = true,
            };

            var network = CharacterCombatStateNetwork.FromCore(core);
            CharacterCombatState roundTrip = network.ToCore();

            Assert.AreEqual(core, roundTrip);
        }
    }
}

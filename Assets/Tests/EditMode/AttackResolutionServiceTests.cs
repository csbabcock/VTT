using GameCore.Combat.Services;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class AttackResolutionServiceTests
    {
        private AttackResolutionService _service;

        [SetUp]
        public void SetUp() => _service = new AttackResolutionService();

        [Test]
        public void CalculateFlatDamage_WithStr16_Returns4()
        {
            Assert.AreEqual(4, AttackResolutionService.CalculateFlatDamage(1, 3, isCritical: false));
        }

        [Test]
        public void CalculateFlatDamage_WithStr8_ClampsAtZero()
        {
            Assert.AreEqual(0, AttackResolutionService.CalculateFlatDamage(1, -1, isCritical: false));
        }

        [Test]
        public void CalculateFlatDamage_OnCritical_DoublesFlatBaseOnly()
        {
            Assert.AreEqual(5, AttackResolutionService.CalculateFlatDamage(1, 3, isCritical: true));
        }

        [Test]
        public void Resolve_Natural1_IsMissRegardlessOfTotal()
        {
            var outcome = _service.Resolve(
                attackRollNatural: 1,
                attackRollTotal: 25,
                targetArmorClass: 10,
                flatBaseDamage: 1,
                damageAbilityModifier: 3);

            Assert.IsFalse(outcome.DidHit);
            Assert.AreEqual(0, outcome.DamageAmount);
        }

        [Test]
        public void Resolve_Natural20_IsHitEvenBelowAc()
        {
            var outcome = _service.Resolve(
                attackRollNatural: 20,
                attackRollTotal: 12,
                targetArmorClass: 18,
                flatBaseDamage: 1,
                damageAbilityModifier: 3);

            Assert.IsTrue(outcome.DidHit);
            Assert.IsTrue(outcome.IsCritical);
            Assert.AreEqual(5, outcome.DamageAmount);
        }

        [Test]
        public void Resolve_TotalEqualsAc_Hits()
        {
            var outcome = _service.Resolve(
                attackRollNatural: 15,
                attackRollTotal: 18,
                targetArmorClass: 18,
                flatBaseDamage: 1,
                damageAbilityModifier: 3);

            Assert.IsTrue(outcome.DidHit);
            Assert.AreEqual(4, outcome.DamageAmount);
        }

        [Test]
        public void Resolve_TotalBelowAc_Misses()
        {
            var outcome = _service.Resolve(
                attackRollNatural: 14,
                attackRollTotal: 17,
                targetArmorClass: 18,
                flatBaseDamage: 1,
                damageAbilityModifier: 3);

            Assert.IsFalse(outcome.DidHit);
            Assert.AreEqual(0, outcome.DamageAmount);
        }
    }
}

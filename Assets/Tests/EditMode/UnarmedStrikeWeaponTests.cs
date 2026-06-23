using GameCore.PlayerData.Rulesets;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class UnarmedStrikeWeaponTests
    {
        private DnD5eRulesetCalculator _calculator;

        [SetUp]
        public void SetUp() => _calculator = new DnD5eRulesetCalculator();

        [Test]
        public void GetWeaponProperties_UnarmedStrike_UsesFlatBaseDamage()
        {
            var props = _calculator.GetWeaponProperties("Unarmed Strike");

            Assert.IsTrue(props.HasValue);
            Assert.AreEqual(0, props.Value.DamageDice);
            Assert.AreEqual(1, props.Value.FlatBaseDamage);
            Assert.AreEqual("Bludgeoning", props.Value.DamageType);
            Assert.AreEqual("Unarmed", props.Value.Category);
        }

        [Test]
        public void IsProficientWithWeapon_Unarmed_RequiresExplicitProficiency()
        {
            Assert.IsFalse(_calculator.IsProficientWithWeapon(
                "Unarmed Strike",
                new System.Collections.Generic.List<string> { "Simple" }));

            Assert.IsTrue(_calculator.IsProficientWithWeapon(
                "Unarmed Strike",
                new System.Collections.Generic.List<string> { "Unarmed" }));
        }
    }
}

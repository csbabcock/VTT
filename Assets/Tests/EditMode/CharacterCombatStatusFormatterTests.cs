using GameCore.PlayerData;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class CharacterCombatStatusFormatterTests
    {
        [Test]
        public void FormatSummary_IncludesConditionsAndInspiration()
        {
            var state = new CharacterCombatState
            {
                CurrentHitPoints = 5,
                HasInspiration = true,
                ConditionFlags = DnD5eConditions.ToFlags(new[] { "Poisoned" }),
            };

            string summary = CharacterCombatStatusFormatter.FormatSummary(state, maxHp: 10);

            StringAssert.Contains("Inspired", summary);
            StringAssert.Contains("Poisoned", summary);
        }

        [Test]
        public void FormatSummary_ReturnsEmpty_WhenNoStatus()
        {
            var state = new CharacterCombatState { CurrentHitPoints = 8 };

            Assert.AreEqual(string.Empty, CharacterCombatStatusFormatter.FormatSummary(state, maxHp: 10));
        }
    }
}

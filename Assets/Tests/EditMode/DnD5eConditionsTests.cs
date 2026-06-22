using NUnit.Framework;
using GameCore.PlayerData;

namespace GameCore.Tests.EditMode
{
    public class DnD5eConditionsTests
    {
        [Test]
        public void ToFlags_AndToList_RoundTrip()
        {
            var data = new DnD5eCharacterData
            {
                conditions = { "Poisoned", "Prone", "Blinded" },
            };

            uint flags = DnD5eConditions.ToFlags(data.conditions);
            var list = DnD5eConditions.ToList(flags);

            Assert.AreEqual(3, list.Count);
            CollectionAssert.Contains(list, "Poisoned");
            CollectionAssert.Contains(list, "Prone");
            CollectionAssert.Contains(list, "Blinded");
        }

        [Test]
        public void Toggle_AddsThenRemovesCondition()
        {
            uint flags = 0;
            flags = DnD5eConditions.Toggle(flags, "Stunned");
            Assert.IsTrue(DnD5eConditions.Has(flags, "Stunned"));

            flags = DnD5eConditions.Toggle(flags, "Stunned");
            Assert.IsFalse(DnD5eConditions.Has(flags, "Stunned"));
        }

        [Test]
        public void TryParse_IsCaseInsensitive()
        {
            Assert.IsTrue(DnD5eConditions.TryParse("frightened", out var flag));
            Assert.AreEqual(DnD5eConditionFlags.Frightened, flag);
        }

        [Test]
        public void Count_ReturnsActiveConditionTotal()
        {
            uint flags = DnD5eConditions.ToFlags(new[] { "Blinded", "Charmed" });
            Assert.AreEqual(2, DnD5eConditions.Count(flags));
        }
    }
}

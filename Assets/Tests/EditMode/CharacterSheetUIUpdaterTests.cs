using GameCore.Actors;
using GameCore.Networking;
using GameCore.PlayerData;
using GameCore.UI.InGame;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameCore.Tests.EditMode
{
    public class CharacterSheetUIUpdaterTests
    {
        [Test]
        public void UpdateCharacterSheet_SetsHitPointsAndTemporaryHitPoints()
        {
            var root = BuildMinimalSheetRoot();
            var data = new DnD5eCharacterData
            {
                characterName = "Aldric",
                characterClass = "Fighter",
                level = 3,
                constitution = 14,
                currentHitPoints = 18,
                temporaryHitPoints = 5,
            };

            CharacterSheetUIUpdater.UpdateCharacterSheet(root, data);

            Assert.AreEqual("18 / 28", root.Q<Label>("hp-value").text);
            Assert.AreEqual("+5", root.Q<Label>("temp-hp-value").text);
            Assert.AreEqual("Aldric", root.Q<Label>("character-name").text);
        }

        [Test]
        public void UpdateCharacterSheet_ShowsZeroTemporaryHitPoints_WhenNone()
        {
            var root = BuildMinimalSheetRoot();
            var data = new DnD5eCharacterData
            {
                characterClass = "Fighter",
                level = 1,
                constitution = 10,
                currentHitPoints = 10,
                temporaryHitPoints = 0,
            };

            CharacterSheetUIUpdater.UpdateCharacterSheet(root, data);

            Assert.AreEqual("0", root.Q<Label>("temp-hp-value").text);
        }

        private static VisualElement BuildMinimalSheetRoot()
        {
            var root = new VisualElement { name = "root" };
            root.Add(new Label { name = "character-name" });
            root.Add(new Label { name = "character-details" });
            root.Add(new Label { name = "hp-value" });
            root.Add(new Label { name = "temp-hp-value" });
            root.Add(new Label { name = "ac-value" });
            root.Add(new Label { name = "initiative-value" });
            root.Add(new Label { name = "speed-value" });
            return root;
        }
    }
}

using System.Collections;
using GameCore.PlayerData;
using GameCore.UI.InGame;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameCore.Tests.EditMode
{
    public class CharacterSheetCombatSectionViewTests
    {
        private VisualElement _root;
        private CharacterSheetCombatSectionView _view;
        private UiToolkitTestHostWindow _hostWindow;

        [SetUp]
        public void SetUp()
        {
            _root = BuildCombatSectionRoot();
            _hostWindow = UiToolkitTestHostWindow.Open(_root);

            _view = new CharacterSheetCombatSectionView();
            _view.Initialize(_root);
        }

        [TearDown]
        public void TearDown()
        {
            UiToolkitTestHostWindow.CloseIfOpen(_hostWindow);
            _hostWindow = null;
        }

        [Test]
        public void Initialize_BuildsAllConditionChips()
        {
            var grid = _root.Q<VisualElement>("charsheet-condition-grid");
            Assert.AreEqual(DnD5eConditions.AllConditionIds.Count, grid.childCount);
        }

        [Test]
        public void Bind_UpdatesInspirationAndExhaustionDisplay()
        {
            var state = new CharacterCombatState
            {
                HasInspiration = true,
                ExhaustionLevel = 2,
                TemporaryHitPoints = 3,
            };

            _view.Bind(state, maxHp: 20);

            Assert.IsTrue(_root.Q<Toggle>("charsheet-inspiration-toggle").value);
            Assert.AreEqual("2", _root.Q<Label>("charsheet-exhaustion-value").text);
        }

        [UnityTest]
        public IEnumerator HitPointsAdjusted_FiresWhenButtonClicked()
        {
            yield return null;

            int? captured = null;
            _view.HitPointsAdjusted += delta => captured = delta;

            SimulateClick(_root.Q<Button>("hp-plus-one"));

            Assert.AreEqual(1, captured);
        }

        [UnityTest]
        public IEnumerator ConditionToggled_FiresWhenConditionChipClicked()
        {
            yield return null;

            string captured = null;
            _view.ConditionToggled += id => captured = id;

            var grid = _root.Q<VisualElement>("charsheet-condition-grid");
            SimulateClick(grid[0]);

            Assert.AreEqual(DnD5eConditions.AllConditionIds[0], captured);
        }

        [UnityTest]
        public IEnumerator DeathSavesChanged_FiresWhenSuccessPipClicked()
        {
            yield return null;

            int? successes = null;
            _view.DeathSavesChanged += (s, _) => successes = s;
            _view.Bind(new CharacterCombatState(), maxHp: 10);

            yield return null;

            var successesContainer = _root.Q<VisualElement>("charsheet-death-successes");
            SimulateClick(successesContainer[0]);

            Assert.AreEqual(1, successes);
        }

        private static void SimulateClick(VisualElement element)
        {
            Assert.IsNotNull(element, "Click target was not found.");
            Assert.IsNotNull(element.panel, "Click target must be attached to a panel.");

            using (var clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = element;
                element.SendEvent(clickEvent);
            }
        }

        private static VisualElement BuildCombatSectionRoot()
        {
            var root = new VisualElement { name = "root" };
            root.Add(new Label { name = "temp-hp-value" });
            root.Add(new VisualElement { name = "charsheet-death-successes" });
            root.Add(new VisualElement { name = "charsheet-death-failures" });
            root.Add(new VisualElement { name = "charsheet-condition-grid" });
            root.Add(new Toggle { name = "charsheet-inspiration-toggle" });
            root.Add(new Label { name = "charsheet-exhaustion-value" });
            root.Add(new Button { name = "hp-minus-five" });
            root.Add(new Button { name = "hp-minus-one" });
            root.Add(new Button { name = "hp-plus-one" });
            root.Add(new Button { name = "hp-plus-five" });
            root.Add(new Button { name = "temp-hp-minus-one" });
            root.Add(new Button { name = "temp-hp-plus-one" });
            root.Add(new Button { name = "charsheet-death-reset" });
            root.Add(new Button { name = "charsheet-exhaustion-minus" });
            root.Add(new Button { name = "charsheet-exhaustion-plus" });
            return root;
        }

    }
}

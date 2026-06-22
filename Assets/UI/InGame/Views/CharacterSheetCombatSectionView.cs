using System;
using System.Collections.Generic;
using GameCore.PlayerData;
using UnityEngine.UIElements;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Combat-tracking controls on the shared character sheet (HP header + Overview status).
    /// </summary>
    public class CharacterSheetCombatSectionView
    {
        private VisualElement _root;
        private Label _tempHpValueLabel;
        private VisualElement _deathSaveSuccesses;
        private VisualElement _deathSaveFailures;
        private VisualElement _conditionGrid;
        private Toggle _inspirationToggle;
        private Label _exhaustionValueLabel;
        private readonly List<Button> _conditionButtons = new List<Button>();
        private int _boundDeathSuccesses;
        private int _boundDeathFailures;
        private bool _wired;

        public event Action<int> HitPointsAdjusted;
        public event Action<int> TemporaryHitPointsAdjusted;
        public event Action<int, int> DeathSavesChanged;
        public event Action DeathSavesReset;
        public event Action<string> ConditionToggled;
        public event Action<bool> InspirationChanged;
        public event Action<int> ExhaustionAdjusted;

        public void Reset()
        {
            _wired = false;
            _root = null;
            _tempHpValueLabel = null;
            _deathSaveSuccesses = null;
            _deathSaveFailures = null;
            _conditionGrid = null;
            _inspirationToggle = null;
            _exhaustionValueLabel = null;
            _conditionButtons.Clear();
        }

        public void Initialize(VisualElement root)
        {
            if (root == null)
                return;

            if (_wired && ReferenceEquals(_root, root))
                return;

            Reset();
            _root = root;
            _tempHpValueLabel = root.Q<Label>("temp-hp-value");
            _deathSaveSuccesses = root.Q<VisualElement>("charsheet-death-successes");
            _deathSaveFailures = root.Q<VisualElement>("charsheet-death-failures");
            _conditionGrid = root.Q<VisualElement>("charsheet-condition-grid");
            _inspirationToggle = root.Q<Toggle>("charsheet-inspiration-toggle");
            _exhaustionValueLabel = root.Q<Label>("charsheet-exhaustion-value");

            WireButton(root, "hp-minus-five", () => HitPointsAdjusted?.Invoke(-5));
            WireButton(root, "hp-minus-one", () => HitPointsAdjusted?.Invoke(-1));
            WireButton(root, "hp-plus-one", () => HitPointsAdjusted?.Invoke(1));
            WireButton(root, "hp-plus-five", () => HitPointsAdjusted?.Invoke(5));
            WireButton(root, "temp-hp-minus-one", () => TemporaryHitPointsAdjusted?.Invoke(-1));
            WireButton(root, "temp-hp-plus-one", () => TemporaryHitPointsAdjusted?.Invoke(1));
            WireButton(root, "charsheet-death-reset", () => DeathSavesReset?.Invoke());
            WireButton(root, "charsheet-exhaustion-minus", () => ExhaustionAdjusted?.Invoke(-1));
            WireButton(root, "charsheet-exhaustion-plus", () => ExhaustionAdjusted?.Invoke(1));

            if (_inspirationToggle != null)
                _inspirationToggle.RegisterValueChangedCallback(evt => InspirationChanged?.Invoke(evt.newValue));

            BuildConditionButtons();
            _wired = true;
        }

        public void Bind(CharacterCombatState combat, int maxHp)
        {
            if (!EnsureElementsAlive())
                return;

            if (_tempHpValueLabel != null)
                _tempHpValueLabel.text = combat.TemporaryHitPoints > 0
                    ? $"+{combat.TemporaryHitPoints}"
                    : "0";

            _boundDeathSuccesses = combat.DeathSaveSuccesses;
            _boundDeathFailures = combat.DeathSaveFailures;
            RefreshDeathSavePips(_deathSaveSuccesses, combat.DeathSaveSuccesses, true);
            RefreshDeathSavePips(_deathSaveFailures, combat.DeathSaveFailures, false);
            RefreshConditionButtons(combat.ConditionFlags);

            if (_inspirationToggle != null)
                _inspirationToggle.SetValueWithoutNotify(combat.HasInspiration);

            if (_exhaustionValueLabel != null)
                _exhaustionValueLabel.text = combat.ExhaustionLevel.ToString();
        }

        private bool EnsureElementsAlive()
        {
            if (_root == null)
                return false;

            if (IsElementAlive(_deathSaveSuccesses)
                && IsElementAlive(_deathSaveFailures)
                && IsElementAlive(_conditionGrid))
            {
                return true;
            }

            _tempHpValueLabel = _root.Q<Label>("temp-hp-value");
            _deathSaveSuccesses = _root.Q<VisualElement>("charsheet-death-successes");
            _deathSaveFailures = _root.Q<VisualElement>("charsheet-death-failures");
            _conditionGrid = _root.Q<VisualElement>("charsheet-condition-grid");
            _inspirationToggle = _root.Q<Toggle>("charsheet-inspiration-toggle");
            _exhaustionValueLabel = _root.Q<Label>("charsheet-exhaustion-value");

            if (_conditionGrid != null && _conditionButtons.Count == 0)
                BuildConditionButtons();

            return IsElementAlive(_deathSaveSuccesses)
                   && IsElementAlive(_deathSaveFailures)
                   && IsElementAlive(_conditionGrid);
        }

        private static bool IsElementAlive(VisualElement element) =>
            element != null && element.panel != null;

        private void BuildConditionButtons()
        {
            if (_conditionGrid == null)
                return;

            _conditionGrid.Clear();
            _conditionButtons.Clear();

            IReadOnlyList<string> ids = DnD5eConditions.AllConditionIds;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                var button = new Button { text = id, name = $"charsheet-condition-{id.ToLowerInvariant()}" };
                button.AddToClassList("charsheet-condition-chip");
                button.AddToClassList("diegetic-button");
                button.AddToClassList("diegetic-button-compact");
                button.RegisterCallback<ClickEvent>(_ => ConditionToggled?.Invoke(id));
                _conditionGrid.Add(button);
                _conditionButtons.Add(button);
            }
        }

        private void RefreshConditionButtons(uint flags)
        {
            IReadOnlyList<string> ids = DnD5eConditions.AllConditionIds;
            for (int i = 0; i < _conditionButtons.Count && i < ids.Count; i++)
            {
                bool active = DnD5eConditions.Has(flags, ids[i]);
                if (active)
                    _conditionButtons[i].AddToClassList("charsheet-condition-chip-active");
                else
                    _conditionButtons[i].RemoveFromClassList("charsheet-condition-chip-active");
            }
        }

        private void RefreshDeathSavePips(VisualElement container, int count, bool successes)
        {
            if (!IsElementAlive(container))
                return;

            container.Clear();
            for (int i = 0; i < CharacterCombatStateRules.MaxDeathSaveCount; i++)
            {
                var pip = new VisualElement();
                pip.style.width = 16;
                pip.style.height = 16;
                pip.pickingMode = PickingMode.Position;
                pip.AddToClassList("charsheet-death-save-pip");
                if (i < count)
                    pip.AddToClassList(successes ? "charsheet-death-save-pip-success" : "charsheet-death-save-pip-failure");

                int index = i;
                container.Add(pip);
                pip.RegisterCallback<ClickEvent>(_ =>
                {
                    if (successes)
                    {
                        int next = _boundDeathSuccesses == index + 1 ? index : index + 1;
                        _boundDeathSuccesses = next;
                        DeathSavesChanged?.Invoke(next, _boundDeathFailures);
                    }
                    else
                    {
                        int next = _boundDeathFailures == index + 1 ? index : index + 1;
                        _boundDeathFailures = next;
                        DeathSavesChanged?.Invoke(_boundDeathSuccesses, next);
                    }
                });
            }
        }

        private static void WireButton(VisualElement root, string name, Action onClick)
        {
            var button = root.Q<Button>(name);
            if (button != null)
                button.RegisterCallback<ClickEvent>(_ => onClick());
        }
    }
}

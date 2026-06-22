using System;
using System.Collections.Generic;
using GameCore.PlayerData;
using GameCore.UI.InGame.Models;
using UnityEngine.UIElements;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// DM-focused character inspector for editing combat state on a selected player.
    /// </summary>
    public class DmCharacterInspectorView
    {
        private VisualElement _panel;
        private Label _headerLabel;
        private Label _hpValueLabel;
        private Label _tempHpValueLabel;
        private VisualElement _deathSaveSuccesses;
        private VisualElement _deathSaveFailures;
        private VisualElement _conditionGrid;
        private Toggle _inspirationToggle;
        private Label _exhaustionValueLabel;
        private readonly List<Button> _conditionButtons = new List<Button>();
        private int _boundDeathSuccesses;
        private int _boundDeathFailures;

        public event Action<int> HitPointsAdjusted;
        public event Action<int> TemporaryHitPointsAdjusted;
        public event Action<int, int> DeathSavesChanged;
        public event Action DeathSavesReset;
        public event Action<string> ConditionToggled;
        public event Action<bool> InspirationChanged;
        public event Action<int> ExhaustionAdjusted;

        public void Initialize(VisualElement root)
        {
            if (root == null)
                return;

            _panel = root.Q<VisualElement>("dm-inspector");
            _headerLabel = root.Q<Label>("dm-inspector-header");
            _hpValueLabel = root.Q<Label>("dm-inspector-hp-value");
            _tempHpValueLabel = root.Q<Label>("dm-inspector-temp-hp-value");
            _deathSaveSuccesses = root.Q<VisualElement>("dm-inspector-death-successes");
            _deathSaveFailures = root.Q<VisualElement>("dm-inspector-death-failures");
            _conditionGrid = root.Q<VisualElement>("dm-inspector-condition-grid");
            _inspirationToggle = root.Q<Toggle>("dm-inspector-inspiration-toggle");
            _exhaustionValueLabel = root.Q<Label>("dm-inspector-exhaustion-value");

            if (_panel == null)
            {
                UnityEngine.Debug.LogWarning("DmCharacterInspectorView: dm-inspector not found in UXML.");
                return;
            }

            WireButton(root, "dm-inspector-hp-minus-five", () => HitPointsAdjusted?.Invoke(-5));
            WireButton(root, "dm-inspector-hp-minus-one", () => HitPointsAdjusted?.Invoke(-1));
            WireButton(root, "dm-inspector-hp-plus-one", () => HitPointsAdjusted?.Invoke(1));
            WireButton(root, "dm-inspector-hp-plus-five", () => HitPointsAdjusted?.Invoke(5));
            WireButton(root, "dm-inspector-temp-minus-one", () => TemporaryHitPointsAdjusted?.Invoke(-1));
            WireButton(root, "dm-inspector-temp-plus-one", () => TemporaryHitPointsAdjusted?.Invoke(1));
            WireButton(root, "dm-inspector-death-reset", () => DeathSavesReset?.Invoke());
            WireButton(root, "dm-inspector-exhaustion-minus", () => ExhaustionAdjusted?.Invoke(-1));
            WireButton(root, "dm-inspector-exhaustion-plus", () => ExhaustionAdjusted?.Invoke(1));

            if (_inspirationToggle != null)
                _inspirationToggle.RegisterValueChangedCallback(evt => InspirationChanged?.Invoke(evt.newValue));

            BuildConditionButtons();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_panel == null)
                return;

            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _panel.SetEnabled(visible);
        }

        public void Bind(
            string displayName,
            int currentHp,
            int maxHp,
            int tempHp,
            int deathSuccesses,
            int deathFailures,
            uint conditionFlags,
            bool hasInspiration,
            int exhaustionLevel)
        {
            if (_headerLabel != null)
                _headerLabel.text = string.IsNullOrEmpty(displayName) ? "No player selected" : displayName;

            if (_hpValueLabel != null)
                _hpValueLabel.text = maxHp > 0 ? $"{currentHp} / {maxHp}" : "—";

            if (_tempHpValueLabel != null)
                _tempHpValueLabel.text = tempHp > 0 ? $"+{tempHp}" : "0";

            _boundDeathSuccesses = deathSuccesses;
            _boundDeathFailures = deathFailures;
            RefreshDeathSavePips(_deathSaveSuccesses, deathSuccesses, true);
            RefreshDeathSavePips(_deathSaveFailures, deathFailures, false);

            RefreshConditionButtons(conditionFlags);

            if (_inspirationToggle != null)
                _inspirationToggle.SetValueWithoutNotify(hasInspiration);

            if (_exhaustionValueLabel != null)
                _exhaustionValueLabel.text = exhaustionLevel.ToString();
        }

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
                var button = new Button { text = id, name = $"dm-condition-{id.ToLowerInvariant()}" };
                button.AddToClassList("dm-condition-chip");
                button.clicked += () => ConditionToggled?.Invoke(id);
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
                    _conditionButtons[i].AddToClassList("dm-condition-chip-active");
                else
                    _conditionButtons[i].RemoveFromClassList("dm-condition-chip-active");
            }
        }

        private void RefreshDeathSavePips(VisualElement container, int count, bool successes)
        {
            if (container == null)
                return;

            container.Clear();
            for (int i = 0; i < CharacterCombatStateRules.MaxDeathSaveCount; i++)
            {
                var pip = new VisualElement();
                pip.AddToClassList("dm-death-save-pip");
                if (i < count)
                    pip.AddToClassList(successes ? "dm-death-save-pip-success" : "dm-death-save-pip-failure");

                int index = i;
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
                container.Add(pip);
            }
        }

        private static void WireButton(VisualElement root, string name, Action onClick)
        {
            var button = root.Q<Button>(name);
            if (button != null)
                button.clicked += onClick;
        }
    }
}

using System;
using System.Collections.Generic;
using GameCore.Actors;
using UnityEngine.UIElements;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// DM-only panel listing connected players and exposing HP adjustment controls for the
    /// currently selected actor. Pure UI binding — the presenter supplies data and handles events.
    /// </summary>
    public class DmPanelView
    {
        private VisualElement _panel;
        private VisualElement _playerList;
        private Label _selectedLabel;
        private Label _hpValueLabel;
        private Button _viewSelfButton;
        private readonly List<Button> _playerButtons = new List<Button>();

        public event Action<int> PlayerSelected;
        public event Action ViewSelfClicked;
        public event Action<int> HitPointsAdjusted;
        public event Action StartEncounterClicked;
        public event Action EndEncounterClicked;
        public event Action NextTurnClicked;

        public void Initialize(VisualElement root)
        {
            if (root == null)
                return;

            _panel = root.Q<VisualElement>("dm-panel");
            _playerList = root.Q<VisualElement>("dm-player-list");
            _selectedLabel = root.Q<Label>("dm-selected-label");
            _hpValueLabel = root.Q<Label>("dm-hp-value");
            _viewSelfButton = root.Q<Button>("dm-view-self-button");

            if (_panel == null)
            {
                UnityEngine.Debug.LogWarning("DmPanelView: dm-panel not found in UXML.");
                return;
            }

            _panel.pickingMode = PickingMode.Position;

            WireButton(root, "dm-hp-minus-five", () => HitPointsAdjusted?.Invoke(-5));
            WireButton(root, "dm-hp-minus-one", () => HitPointsAdjusted?.Invoke(-1));
            WireButton(root, "dm-hp-plus-one", () => HitPointsAdjusted?.Invoke(1));
            WireButton(root, "dm-hp-plus-five", () => HitPointsAdjusted?.Invoke(5));
            WireButton(root, "dm-start-encounter-button", () => StartEncounterClicked?.Invoke());
            WireButton(root, "dm-end-encounter-button", () => EndEncounterClicked?.Invoke());
            WireButton(root, "dm-next-turn-button", () => NextTurnClicked?.Invoke());

            if (_viewSelfButton != null)
                _viewSelfButton.clicked += () => ViewSelfClicked?.Invoke();

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_panel == null)
                return;

            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _panel.SetEnabled(visible);
        }

        public void RefreshPlayerList(IReadOnlyList<IActor> actors, int selectedOwnerId)
        {
            if (_playerList == null)
                return;

            ClearPlayerButtons();
            _playerList.Clear();

            if (actors == null || actors.Count == 0)
            {
                _playerList.Add(new Label("No players registered yet."));
                UpdateSelectionDetails(null, selectedOwnerId);
                return;
            }

            foreach (var actor in actors)
            {
                if (actor == null)
                    continue;

                int ownerId = actor.OwnerId;
                var row = new Button
                {
                    text = actor.DisplayName,
                    name = $"dm-player-{ownerId}",
                };
                row.AddToClassList("dm-player-button");
                if (ownerId == selectedOwnerId)
                    row.AddToClassList("dm-player-button-selected");

                row.clicked += () => PlayerSelected?.Invoke(ownerId);
                _playerList.Add(row);
                _playerButtons.Add(row);
            }

            IActor selected = null;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] != null && actors[i].OwnerId == selectedOwnerId)
                {
                    selected = actors[i];
                    break;
                }
            }

            UpdateSelectionDetails(selected, selectedOwnerId);
        }

        public void UpdateSelectionDetails(IActor selected, int currentHp, int maxHp)
        {
            if (_selectedLabel != null)
                _selectedLabel.text = selected != null ? $"Inspecting: {selected.DisplayName}" : "Inspecting: (none)";

            if (_hpValueLabel != null)
                _hpValueLabel.text = selected != null ? $"HP: {currentHp} / {maxHp}" : "HP: —";
        }

        private void UpdateSelectionDetails(IActor selected, int selectedOwnerId)
        {
            if (_selectedLabel != null)
            {
                _selectedLabel.text = selected != null
                    ? $"Inspecting: {selected.DisplayName}"
                    : selectedOwnerId >= 0 ? $"Selected client {selectedOwnerId}" : "Inspecting: (none)";
            }

            if (_hpValueLabel != null)
                _hpValueLabel.text = "HP: —";
        }

        private void ClearPlayerButtons()
        {
            _playerButtons.Clear();
        }

        private static void WireButton(VisualElement root, string name, Action onClick)
        {
            var button = root.Q<Button>(name);
            if (button != null)
                button.clicked += onClick;
        }
    }
}

using System;
using System.Collections.Generic;
using GameCore.UI.InGame.Models;
using UnityEngine.UIElements;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// DM-only panel listing connected players and exposing encounter controls.
    /// </summary>
    public class DmPanelView
    {
        private VisualElement _panel;
        private VisualElement _playerList;
        private Button _startEncounterButton;
        private Button _endEncounterButton;
        private Button _nextTurnButton;
        private readonly List<Button> _playerButtons = new List<Button>();

        public event Action<int> PlayerSelected;
        public event Action StartEncounterClicked;
        public event Action EndEncounterClicked;
        public event Action NextTurnClicked;

        public void Reset()
        {
            _panel = null;
            _playerList = null;
            _startEncounterButton = null;
            _endEncounterButton = null;
            _nextTurnButton = null;
            _playerButtons.Clear();
        }

        public void Initialize(VisualElement root)
        {
            if (root == null)
                return;

            _panel = root.Q<VisualElement>("dm-panel");
            _playerList = root.Q<VisualElement>("dm-player-list");

            if (_playerList != null)
                _playerList.pickingMode = PickingMode.Position;

            var playerListScroll = root.Q<ScrollView>("dm-player-list-scroll");
            if (playerListScroll != null)
                playerListScroll.pickingMode = PickingMode.Position;

            if (_panel == null)
            {
                UnityEngine.Debug.LogWarning("DmPanelView: dm-panel not found in UXML.");
                return;
            }

            _panel.pickingMode = PickingMode.Position;

            _startEncounterButton = WireButton(root, "dm-start-encounter-button", () => StartEncounterClicked?.Invoke());
            _endEncounterButton = WireButton(root, "dm-end-encounter-button", () => EndEncounterClicked?.Invoke());
            _nextTurnButton = WireButton(root, "dm-next-turn-button", () => NextTurnClicked?.Invoke());

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_panel == null)
                return;

            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _panel.SetEnabled(visible);
        }

        public void SetEncounterControls(bool isEncounterActive, bool hasTurnOrder)
        {
            if (_startEncounterButton != null)
                _startEncounterButton.SetEnabled(!isEncounterActive);

            if (_endEncounterButton != null)
                _endEncounterButton.SetEnabled(isEncounterActive);

            if (_nextTurnButton != null)
                _nextTurnButton.SetEnabled(isEncounterActive && hasTurnOrder);
        }

        public void RefreshPlayerList(IReadOnlyList<DmPlayerRowState> rows)
        {
            if (_playerList == null)
                return;

            ClearPlayerButtons();
            _playerList.Clear();

            if (rows == null || rows.Count == 0)
            {
                var emptyLabel = new Label("No players registered yet.");
                emptyLabel.AddToClassList("dm-empty-label");
                _playerList.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var button = BuildPlayerRow(row);
                _playerList.Add(button);
                _playerButtons.Add(button);
            }
        }

        private Button BuildPlayerRow(DmPlayerRowState row)
        {
            int ownerId = row.OwnerId;
            var button = new Button { name = $"dm-player-{ownerId}" };
            button.AddToClassList("dm-player-button");
            button.AddToClassList("diegetic-button");
            button.pickingMode = PickingMode.Position;
            button.focusable = true;
            if (row.IsSelected)
                button.AddToClassList("diegetic-button-selected");

            var rowContainer = new VisualElement();
            rowContainer.AddToClassList("dm-player-row");
            rowContainer.pickingMode = PickingMode.Ignore;

            var nameRow = new VisualElement();
            nameRow.style.flexDirection = FlexDirection.Row;
            nameRow.style.justifyContent = Justify.SpaceBetween;
            nameRow.style.width = Length.Percent(100);
            nameRow.pickingMode = PickingMode.Ignore;

            var nameLabel = new Label(row.DisplayName);
            nameLabel.AddToClassList("dm-player-row-name");
            nameLabel.pickingMode = PickingMode.Ignore;
            nameRow.Add(nameLabel);

            if (row.IsCurrentTurn)
            {
                var turnLabel = new Label("Turn");
                turnLabel.AddToClassList("dm-player-row-status");
                turnLabel.pickingMode = PickingMode.Ignore;
                nameRow.Add(turnLabel);
            }

            rowContainer.Add(nameRow);

            var hpLabel = new Label($"{row.CurrentHp}/{row.MaxHp} HP");
            hpLabel.AddToClassList("dm-player-row-hp");
            hpLabel.pickingMode = PickingMode.Ignore;
            rowContainer.Add(hpLabel);

            if (!string.IsNullOrEmpty(row.StatusSummary))
            {
                var statusLabel = new Label(row.StatusSummary);
                statusLabel.AddToClassList("dm-player-row-status");
                statusLabel.pickingMode = PickingMode.Ignore;
                rowContainer.Add(statusLabel);
            }

            button.Add(rowContainer);
            button.clicked += () => PlayerSelected?.Invoke(ownerId);
            return button;
        }

        private void ClearPlayerButtons()
        {
            _playerButtons.Clear();
        }

        private static Button WireButton(VisualElement root, string name, Action onClick)
        {
            var button = root.Q<Button>(name);
            if (button != null)
                button.clicked += onClick;

            return button;
        }
    }
}

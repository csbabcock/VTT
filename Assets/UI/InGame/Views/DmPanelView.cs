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
        private readonly List<Button> _playerButtons = new List<Button>();

        public event Action<int> PlayerSelected;
        public event Action StartEncounterClicked;
        public event Action EndEncounterClicked;
        public event Action NextTurnClicked;

        public void Reset()
        {
            _panel = null;
            _playerList = null;
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

            WireButton(root, "dm-start-encounter-button", () => StartEncounterClicked?.Invoke());
            WireButton(root, "dm-end-encounter-button", () => EndEncounterClicked?.Invoke());
            WireButton(root, "dm-next-turn-button", () => NextTurnClicked?.Invoke());

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_panel == null)
                return;

            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _panel.SetEnabled(visible);
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
            var button = new Button { name = $"dm-player-{ownerId}", text = row.DisplayName };
            button.AddToClassList("dm-player-button");
            button.AddToClassList("diegetic-button");
            button.pickingMode = PickingMode.Position;
            button.focusable = true;
            if (row.IsSelected)
                button.AddToClassList("diegetic-button-selected");

            button.clicked += () => PlayerSelected?.Invoke(ownerId);
            return button;
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

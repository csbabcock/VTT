using System;
using System.Collections.Generic;
using GameCore.PlayerData;
using GameCore.UI.InGame.Models;
using UnityEngine.UIElements;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// DM-only panel listing connected players and exposing encounter controls.
    /// Character editing lives in <see cref="DmCharacterInspectorView"/>.
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

        public void Initialize(VisualElement root)
        {
            if (root == null)
                return;

            _panel = root.Q<VisualElement>("dm-panel");
            _playerList = root.Q<VisualElement>("dm-player-list");

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
            var button = new Button { name = $"dm-player-{ownerId}" };
            button.AddToClassList("dm-player-button");
            button.AddToClassList("diegetic-button");
            if (row.IsSelected)
                button.AddToClassList("diegetic-button-selected");

            var container = new VisualElement();
            container.AddToClassList("dm-player-row");

            var nameLabel = new Label(row.DisplayName);
            nameLabel.AddToClassList("dm-player-row-name");
            container.Add(nameLabel);

            string hpText = row.MaxHitPoints > 0
                ? $"{row.CurrentHitPoints}/{row.MaxHitPoints}"
                : "—";
            if (row.TemporaryHitPoints > 0)
                hpText += $" (+{row.TemporaryHitPoints})";

            var hpLabel = new Label(hpText);
            hpLabel.AddToClassList("dm-player-row-hp");
            container.Add(hpLabel);

            string status = BuildStatusSummary(row);
            if (!string.IsNullOrEmpty(status))
            {
                var statusLabel = new Label(status);
                statusLabel.AddToClassList("dm-player-row-status");
                container.Add(statusLabel);
            }

            button.Add(container);
            button.clicked += () => PlayerSelected?.Invoke(ownerId);
            return button;
        }

        private static string BuildStatusSummary(DmPlayerRowState row)
        {
            int conditionCount = DnD5eConditions.Count(row.ConditionFlags);
            bool hasDeathSaves = row.CurrentHitPoints <= 0
                                 && (row.DeathSaveSuccesses > 0 || row.DeathSaveFailures > 0);

            if (conditionCount == 0 && !hasDeathSaves)
                return string.Empty;

            var parts = new List<string>();
            if (conditionCount > 0)
                parts.Add($"{conditionCount} cond");

            if (hasDeathSaves)
                parts.Add($"DS {row.DeathSaveSuccesses}/{row.DeathSaveFailures}");

            return string.Join(" · ", parts);
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

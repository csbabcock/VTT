using GameCore.UI.InGame.Models;
using GameCore.UI.InGame.Services;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Handles game log display and management.
    /// Follows Single Responsibility Principle - only handles game log functionality.
    /// </summary>
    public class GameLogView : MonoBehaviour
    {
        #region Constants
        private const int MAX_LOG_ENTRIES = 100;
        private const string GAME_LOG_HEIGHT_PREF = "GameLogHeight";
        private const string GAME_LOG_HEIGHT_VERSION = "GameLogHeightVersion";
        private const int GAME_LOG_HEIGHT_VERSION_NUM = 2;
        private const float DEFAULT_GAME_LOG_HEIGHT = 600f;
        private const float OLD_DEFAULT_GAME_LOG_HEIGHT = 300f;
        private const float SCREEN_EDGE_BUFFER = 5f;
        #endregion

        #region Private Fields
        private VisualElement _root;
        private VisualElement _gameLogPanel;
        private UIAnimationController _animationController;
        private Button _clearLogButton;
        private System.Action _clearLogClickedHandler;
        #endregion

        #region Events
        /// <summary>
        /// Fired when a log entry delete button is clicked.
        /// </summary>
        public event System.Action<VisualElement> LogEntryDeleteClicked;

        /// <summary>
        /// Fired when the clear log button is clicked.
        /// </summary>
        public event System.Action ClearLogClicked;
        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the game log view.
        /// </summary>
        public void Initialize(VisualElement root, UIAnimationController animationController)
        {
            DetachClearButton();

            _root = root;
            _animationController = animationController;
            
            _gameLogPanel = _root.Q<VisualElement>("game-log-panel");
            if (_gameLogPanel == null)
            {
                Debug.LogWarning("GameLogView: Game log panel not found!");
                return;
            }

            // Configure game log panel
            _gameLogPanel.pickingMode = PickingMode.Position;
            
            // Only hide and position off-screen during play mode
            // In editor/preview, panel remains visible with default positioning from USS
            if (Application.isPlaying)
            {
                _gameLogPanel.style.display = DisplayStyle.None;
                _gameLogPanel.SetEnabled(false);
                _gameLogPanel.AddToClassList("runtime-hidden");
            }

            // Load saved height preference
            LoadGameLogHeight();

            // Wire up clear button
            _clearLogButton = _root.Q<Button>("game-log-clear-button");
            if (_clearLogButton != null)
            {
                _clearLogButton.pickingMode = PickingMode.Position;
                _clearLogClickedHandler = () => ClearLogClicked?.Invoke();
                _clearLogButton.clicked += _clearLogClickedHandler;
            }
        }

        private void DetachClearButton()
        {
            if (_clearLogButton != null && _clearLogClickedHandler != null)
            {
                try
                {
                    _clearLogButton.clicked -= _clearLogClickedHandler;
                }
                catch
                {
                    // Button may already be disposed after a panel reload.
                }
            }

            _clearLogButton = null;
            _clearLogClickedHandler = null;
        }

        /// <summary>
        /// Sets the visibility of the game log panel.
        /// </summary>
        public void SetVisible(bool isVisible, float panelOffscreenRight, float panelOnscreenRight)
        {
            if (_gameLogPanel == null)
                return;

            if (isVisible)
            {
                PrepareForAnimation(true, panelOffscreenRight, panelOnscreenRight);
                if (_animationController != null)
                {
                    float startRight = GetPanelRightPosition(panelOffscreenRight);
                    _animationController.AnimateGameLogSlideIn(_gameLogPanel, startRight, panelOnscreenRight);
                }
            }
            else
            {
                Hide(panelOffscreenRight, panelOnscreenRight);
            }
        }

        /// <summary>
        /// Adds a new entry to the game log.
        /// </summary>
        public void AddLogEntry(FormattedLogEntry entry)
        {
            if (!ValidateGameLogPanel())
                return;

            var logEntries = GetLogEntriesContainer();
            if (logEntries == null)
                return;

            var card = CreateLogEntryCard(entry);
            logEntries.Add(card);

            ScrollToBottom();
            EnforceLogEntryLimit(logEntries);
        }

        /// <summary>
        /// Clears all entries from the game log.
        /// </summary>
        public void ClearLog()
        {
            if (_gameLogPanel == null)
            {
                Debug.LogWarning("GameLogView: Game log panel is null!");
                return;
            }

            var logEntries = _root.Q<VisualElement>("game-log-entries");
            if (logEntries != null)
            {
                logEntries.Clear();
            }
        }

        /// <summary>
        /// Removes a specific log entry from the game log.
        /// </summary>
        public void RemoveLogEntry(VisualElement entryCard)
        {
            if (entryCard == null)
                return;

            var logEntries = _root.Q<VisualElement>("game-log-entries");
            if (logEntries != null && entryCard.parent == logEntries)
            {
                logEntries.Remove(entryCard);
            }
        }

        #endregion

        #region Private Methods

        private bool ValidateGameLogPanel()
        {
            if (_gameLogPanel == null)
            {
                Debug.LogWarning("GameLogView: Game log panel is null!");
                return false;
            }
            return true;
        }

        private VisualElement GetLogEntriesContainer()
        {
            var logEntries = _root.Q<VisualElement>("game-log-entries");
            if (logEntries == null)
            {
                Debug.LogWarning("GameLogView: Game log entries container is null!");
            }
            return logEntries;
        }

        private VisualElement CreateLogEntryCard(FormattedLogEntry entry)
        {
            var card = new VisualElement();
            card.AddToClassList("game-log-card");
            card.AddToClassList(entry.CssClass);
            card.pickingMode = PickingMode.Ignore;

            var cardHeader = CreateLogEntryHeader(entry, card);
            var mainContent = CreateLogEntryContent(entry);

            card.Add(cardHeader);
            card.Add(mainContent);

            return card;
        }

        private VisualElement CreateLogEntryHeader(FormattedLogEntry entry, VisualElement card)
        {
            var cardHeader = new VisualElement();
            cardHeader.AddToClassList("game-log-card-header");

            if (!string.IsNullOrEmpty(entry.CharacterName))
            {
                var characterNameLabel = new Label(entry.CharacterName);
                characterNameLabel.AddToClassList("game-log-character-name");
                cardHeader.Add(characterNameLabel);
            }

            var deleteButton = new Button();
            deleteButton.AddToClassList("game-log-delete-button");
            deleteButton.text = "×";
            deleteButton.tooltip = "Delete this entry";
            deleteButton.pickingMode = PickingMode.Position;
            deleteButton.clicked += () => LogEntryDeleteClicked?.Invoke(card);
            cardHeader.Add(deleteButton);

            return cardHeader;
        }

        private VisualElement CreateLogEntryContent(FormattedLogEntry entry)
        {
            var mainContent = new VisualElement();
            mainContent.AddToClassList("game-log-main-content");

            var actionRow = new VisualElement();
            actionRow.AddToClassList("game-log-action-row");

            var actionTypeLabel = new Label(entry.ActionType);
            actionTypeLabel.AddToClassList("game-log-action-type");
            actionRow.Add(actionTypeLabel);

            if (!string.IsNullOrEmpty(entry.SubActionType))
            {
                var subActionLabel = new Label(entry.SubActionType);
                subActionLabel.AddToClassList("game-log-sub-action");
                subActionLabel.AddToClassList($"sub-action-{entry.CssClass.Replace("log-", "")}");
                actionRow.Add(subActionLabel);
            }

            mainContent.Add(actionRow);

            if (!string.IsNullOrEmpty(entry.DiceFormula))
            {
                var formulaLabel = new Label(entry.DiceFormula);
                formulaLabel.AddToClassList("game-log-dice-formula");
                mainContent.Add(formulaLabel);
            }

            if (!string.IsNullOrEmpty(entry.DiceBreakdown))
            {
                var diceBreakdownLabel = new Label(entry.DiceBreakdown);
                diceBreakdownLabel.AddToClassList("game-log-dice-breakdown");
                mainContent.Add(diceBreakdownLabel);
            }

            if (entry.Result.HasValue)
            {
                var resultLabel = new Label(entry.Result.Value.ToString());
                resultLabel.AddToClassList("game-log-result");
                mainContent.Add(resultLabel);
            }

            var timestamp = System.DateTime.Now.ToString("h:mm tt");
            var timestampLabel = new Label(timestamp);
            timestampLabel.AddToClassList("game-log-timestamp");
            mainContent.Add(timestampLabel);

            return mainContent;
        }

        private void ScrollToBottom()
        {
            var scrollView = _root.Q<ScrollView>("game-log-content");
            if (scrollView == null)
                return;

            var contentContainer = scrollView.contentContainer;

            void ScrollToBottomCallback(GeometryChangedEvent evt)
            {
                contentContainer.UnregisterCallback<GeometryChangedEvent>(ScrollToBottomCallback);

                float contentHeight = contentContainer.layout.height;
                float viewportHeight = scrollView.contentViewport.layout.height;
                float maxScroll = contentHeight - viewportHeight;

                if (maxScroll > 0)
                {
                    scrollView.scrollOffset = new Vector2(0, maxScroll);
                }
            }

            contentContainer.RegisterCallback<GeometryChangedEvent>(ScrollToBottomCallback);
            contentContainer.MarkDirtyRepaint();
        }

        private void EnforceLogEntryLimit(VisualElement logEntries)
        {
            while (logEntries.childCount > MAX_LOG_ENTRIES)
            {
                var firstChild = logEntries[0];
                logEntries.Remove(firstChild);
            }
        }

        private void LoadGameLogHeight()
        {
            if (_gameLogPanel == null)
                return;

            int savedVersion = UnityEngine.PlayerPrefs.GetInt(GAME_LOG_HEIGHT_VERSION, 0);
            float savedHeight;
            
            if (savedVersion < GAME_LOG_HEIGHT_VERSION_NUM)
            {
                if (UnityEngine.PlayerPrefs.HasKey(GAME_LOG_HEIGHT_PREF))
                {
                    float oldHeight = UnityEngine.PlayerPrefs.GetFloat(GAME_LOG_HEIGHT_PREF);
                    if (Mathf.Abs(oldHeight - OLD_DEFAULT_GAME_LOG_HEIGHT) < 50f)
                    {
                        savedHeight = DEFAULT_GAME_LOG_HEIGHT;
                    }
                    else
                    {
                        savedHeight = oldHeight;
                    }
                }
                else
                {
                    savedHeight = DEFAULT_GAME_LOG_HEIGHT;
                }
                
                UnityEngine.PlayerPrefs.SetFloat(GAME_LOG_HEIGHT_PREF, savedHeight);
                UnityEngine.PlayerPrefs.SetInt(GAME_LOG_HEIGHT_VERSION, GAME_LOG_HEIGHT_VERSION_NUM);
                UnityEngine.PlayerPrefs.Save();
            }
            else
            {
                savedHeight = UnityEngine.PlayerPrefs.GetFloat(GAME_LOG_HEIGHT_PREF, DEFAULT_GAME_LOG_HEIGHT);
            }
            
            savedHeight = ClampGameLogHeightToScreen(savedHeight);
            _gameLogPanel.style.height = savedHeight;
        }

        private float ClampGameLogHeightToScreen(float height)
        {
            if (_gameLogPanel == null || _root == null)
                return height;

            float screenHeight = Screen.height;
            float panelTop = _gameLogPanel.resolvedStyle.top;
            
            if (float.IsNaN(panelTop) || panelTop <= 0)
            {
                panelTop = 815f; // Fallback
            }

            float maxHeightFromScreen = screenHeight - panelTop - SCREEN_EDGE_BUFFER;
            return Mathf.Min(height, maxHeightFromScreen);
        }

        private void PrepareForAnimation(bool isVisible, float panelOffscreenRight, float panelOnscreenRight)
        {
            if (_gameLogPanel == null)
                return;

            if (_animationController != null)
            {
                _animationController.StopGameLogAnimation();
            }

            float gameLogCurrentRight = GetPanelRightPosition(panelOffscreenRight);
            if (float.IsNaN(gameLogCurrentRight))
            {
                gameLogCurrentRight = isVisible ? panelOffscreenRight : panelOnscreenRight;
            }

            // Remove runtime-hidden class when showing
            if (isVisible)
            {
                _gameLogPanel.RemoveFromClassList("runtime-hidden");
            }

            _gameLogPanel.style.display = DisplayStyle.Flex;
            _gameLogPanel.SetEnabled(true);
            _gameLogPanel.pickingMode = PickingMode.Position;
            _gameLogPanel.style.right = gameLogCurrentRight;

            if (isVisible)
            {
                float currentHeight = _gameLogPanel.resolvedStyle.height;
                float clampedHeight = ClampGameLogHeightToScreen(currentHeight);
                if (clampedHeight != currentHeight)
                {
                    _gameLogPanel.style.height = clampedHeight;
                }
            }

            _gameLogPanel.MarkDirtyRepaint();
        }

        private void Hide(float panelOffscreenRight, float panelOnscreenRight)
        {
            if (_gameLogPanel == null)
                return;

            if (_animationController != null)
            {
                _animationController.StopGameLogAnimation();
            }

            float gameLogCurrentRight = GetPanelRightPosition(panelOnscreenRight);
            if (float.IsNaN(gameLogCurrentRight))
            {
                gameLogCurrentRight = panelOnscreenRight;
            }

            float gameLogDistanceToOffScreen = Mathf.Abs(gameLogCurrentRight - panelOffscreenRight);
            const float INSTANT_CLOSE_DISTANCE_THRESHOLD = 50f;
            
            if (gameLogDistanceToOffScreen > INSTANT_CLOSE_DISTANCE_THRESHOLD && _animationController != null)
            {
                _animationController.AnimateGameLogSlideOut(_gameLogPanel, gameLogCurrentRight, panelOffscreenRight, () =>
                {
                    _gameLogPanel.AddToClassList("runtime-hidden");
                    _gameLogPanel.style.display = DisplayStyle.None;
                    _gameLogPanel.SetEnabled(false);
                    _gameLogPanel.pickingMode = PickingMode.Ignore;
                });
            }
            else
            {
                _gameLogPanel.AddToClassList("runtime-hidden");
                _gameLogPanel.style.right = panelOffscreenRight;
                _gameLogPanel.style.display = DisplayStyle.None;
                _gameLogPanel.SetEnabled(false);
                _gameLogPanel.pickingMode = PickingMode.Ignore;
            }
        }

        private float GetPanelRightPosition(float defaultValue)
        {
            if (_gameLogPanel == null)
                return defaultValue;

            float position = _gameLogPanel.resolvedStyle.right;
            if (float.IsNaN(position))
            {
                position = _gameLogPanel.style.right.value.value;
            }
            if (float.IsNaN(position))
            {
                return defaultValue;
            }
            return position;
        }

        #endregion
    }
}


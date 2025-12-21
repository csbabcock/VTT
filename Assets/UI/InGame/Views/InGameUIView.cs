using GameCore.UI;
using GameCore.UI.InGame.Services;
using GameCore.UI.InGame.Models;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Base view for in-game UI using UI Toolkit.
    /// Intended for diegetic-style HUD elements anchored in the world or screen.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class InGameUIView : MonoBehaviour, IUIView<InGameUIState>
    {
        #region Constants
        private const int TOTAL_TABS = 7;
        private const int VISIBLE_TAB_COUNT = 4;
        private const float DRAG_THRESHOLD = 10f;
        private const float TAB_WIDTH = 100f;
        private const float ANIMATION_DURATION = 0.3f;
        private const float PANEL_OFFSCREEN_RIGHT = -568f;
        private const float PANEL_ONSCREEN_RIGHT = 48f;
        private const int DRAG_CLICK_DELAY_MS = 50;
        private const float VISIBILITY_DISTANCE_THRESHOLD = 100f; // Distance from on-screen position to consider visible
        private const float INSTANT_CLOSE_DISTANCE_THRESHOLD = 50f; // Distance from off-screen to skip animation
        private const float MIN_VISIBILITY_CHANGE_INTERVAL = 0.1f; // Minimum seconds between visibility changes
        #endregion

        #region Serialized Fields
        [Header("Assets")]
        [Tooltip("USS stylesheet for this view. Drag InGameUI.uss here.")]
        [SerializeField] private StyleSheet _inGameStyleSheet;
        #endregion

        #region Private Fields

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _characterSheetPanel;
        private VisualElement _gameLogPanel;
        private ScrollView _characterSheetScrollView;
        private VisualElement[] _characterSheetTabs;
        private Button[] _tabButtons;
        private System.Action[] _tabButtonHandlers;
        private VisualElement _tabsContainer;
        private VisualElement _tabsWrapper;
        private Button _tabNavLeft;
        private Button _tabNavRight;
        private int _currentTabOffset = 0;
        
        // Drag functionality for tabs
        private bool _isDragging = false;
        private float _dragStartX = 0f;
        private float _dragStartOffset = 0f;
        
        // Game log height preference (for saved height, no interactive resizing)
        private const string GAME_LOG_HEIGHT_PREF = "GameLogHeight";
        private const string GAME_LOG_HEIGHT_VERSION = "GameLogHeightVersion";
        private const int GAME_LOG_HEIGHT_VERSION_NUM = 2; // Increment when default changes
        private const float DEFAULT_GAME_LOG_HEIGHT = 600f;
        private const float OLD_DEFAULT_GAME_LOG_HEIGHT = 300f;
        private const float SCREEN_EDGE_BUFFER = 5f; // Buffer from screen edges
        
        // Animation state
        private Coroutine _currentAnimation = null;
        private bool? _targetVisibilityState = null; // Track what state we're animating to (null = not animating)
        private float _lastVisibilityChangeTime = 0f; // Track when we last changed visibility to prevent rapid toggling
        private Coroutine _gameLogAnimation = null;
        #endregion

        #region Public Properties

        public VisualElement Root => _root;

        /// <summary>
        /// Checks if the character sheet is currently open/visible.
        /// </summary>
        public bool IsCharacterSheetOpen()
        {
            if (_characterSheetPanel == null)
                return false;

            return _characterSheetPanel.style.display == DisplayStyle.Flex;
        }

        /// <summary>
        /// Checks if the mouse is currently over the character sheet panel.
        /// Uses multiple detection methods for reliability.
        /// </summary>
        public bool IsMouseOverCharacterSheet()
        {
            // First check if character sheet is open - if not, mouse can't be over it
            if (!IsCharacterSheetOpen())
                return false;

            if (_uiDocument == null || _characterSheetPanel == null)
                return false;

            // Get mouse position
            Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null)
                return false;
            mousePosition = mouse.position.ReadValue();
#else
            mousePosition = Input.mousePosition;
#endif

            // Get the panel
            var panel = _uiDocument.rootVisualElement.panel;
            if (panel == null)
                return false;

            // Method 1: Use Panel.Pick to check if mouse is over the character sheet
            // Panel.Pick uses screen coordinates directly
            var pickedElement = panel.Pick(mousePosition);
            if (pickedElement != null)
            {
                // Check if the picked element is within the character sheet panel hierarchy
                // Also verify the element can receive pointer events
                VisualElement current = pickedElement;
                while (current != null)
                {
                    if (current == _characterSheetPanel)
                    {
                        // Verify the panel can actually receive events
                        if (current.pickingMode != PickingMode.Ignore && 
                            current.enabledInHierarchy &&
                            current.resolvedStyle.display == DisplayStyle.Flex)
                        {
                            return true;
                        }
                    }
                    current = current.parent;
                }
            }

            // Method 2: Check if mouse position is within the character sheet panel's world bounds
            // UI Toolkit worldBound is in panel space (top-left origin)
            Rect panelRect = _characterSheetPanel.worldBound;
            
            // Convert screen coordinates to panel space
            // Panel space uses top-left origin, screen uses bottom-left
            float screenHeight = Screen.height;
            Vector2 panelSpacePos = new Vector2(
                mousePosition.x,
                screenHeight - mousePosition.y
            );

            if (panelRect.Contains(panelSpacePos))
            {
                // Additional check: verify the element is actually visible and enabled
                if (_characterSheetPanel.resolvedStyle.display == DisplayStyle.Flex &&
                    _characterSheetPanel.enabledInHierarchy &&
                    _characterSheetPanel.pickingMode != PickingMode.Ignore)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Events
        /// <summary>
        /// Fired when a tab button is clicked. Parameter is the tab index.
        /// </summary>
        public event System.Action<int> TabClicked;

        /// <summary>
        /// Fired when an ability score button is clicked. Parameter is the ability name (e.g., "STR", "DEX").
        /// </summary>
        public event System.Action<string> AbilityScoreClicked;

        /// <summary>
        /// Fired when a skill button is clicked. Parameter is the skill name (e.g., "Acrobatics", "Athletics").
        /// </summary>
        public event System.Action<string> SkillClicked;

        /// <summary>
        /// Fired when an action button is clicked. Parameter is the action name (e.g., "Attack", "Dash").
        /// </summary>
        public event System.Action<string> ActionClicked;

        /// <summary>
        /// Fired when an attack button is clicked. Parameter is the attack name (e.g., "Longsword", "Shortbow").
        /// </summary>
        public event System.Action<string> AttackClicked;

        /// <summary>
        /// Fired when a feature button is clicked. Parameter is the feature name.
        /// </summary>
        public event System.Action<string> FeatureClicked;

        /// <summary>
        /// Fired when a rest button is clicked. Parameter is the rest type ("Short Rest" or "Long Rest").
        /// </summary>
        public event System.Action<string> RestClicked;

        /// <summary>
        /// Fired when the clear log button is clicked.
        /// </summary>
        public event System.Action ClearLogClicked;

        /// <summary>
        /// Fired when a log entry delete button is clicked. Parameter is the log entry card element.
        /// </summary>
        public event System.Action<VisualElement> LogEntryDeleteClicked;
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_root == null)
            {
                Initialize();
            }

            Show();
        }

        private void OnDisable()
        {
            // Unsubscribe from tab buttons
            if (_tabButtons != null && _tabButtonHandlers != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    if (_tabButtons[i] != null && _tabButtonHandlers[i] != null)
                    {
                        _tabButtons[i].clicked -= _tabButtonHandlers[i];
                    }
                }
            }

            // Unsubscribe from carousel navigation
            if (_tabNavLeft != null)
            {
                _tabNavLeft.clicked -= OnTabNavLeftClicked;
            }

            if (_tabNavRight != null)
            {
                _tabNavRight.clicked -= OnTabNavRightClicked;
            }

            // Unsubscribe from drag events
            if (_tabsWrapper != null)
            {
                _tabsWrapper.UnregisterCallback<PointerDownEvent>(OnTabsPointerDown);
                _tabsWrapper.UnregisterCallback<PointerMoveEvent>(OnTabsPointerMove);
                _tabsWrapper.UnregisterCallback<PointerUpEvent>(OnTabsPointerUp);
                _tabsWrapper.UnregisterCallback<PointerLeaveEvent>(OnTabsPointerLeave);
            }

            Hide();
        }

        public void Initialize()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            _root = _uiDocument.rootVisualElement;

            if (_root == null)
            {
                Debug.LogError("InGameUIView: UIDocument has no rootVisualElement. Make sure the UXML is assigned.");
                return;
            }

            // Configure root element for input
            _root.pickingMode = PickingMode.Position;
            _root.focusable = true;
            
            if (_root.resolvedStyle.display == DisplayStyle.None)
            {
                Debug.LogWarning("InGameUIView: Root element has display: none! UI won't be visible or interactive.");
            }

            // Add style sheet if assigned
            if (_inGameStyleSheet != null && !_root.styleSheets.Contains(_inGameStyleSheet))
            {
                _root.styleSheets.Add(_inGameStyleSheet);
            }

            _characterSheetPanel = _root.Q<VisualElement>("character-sheet-panel");

            // Ensure character sheet panel can receive pointer events and starts hidden
            if (_characterSheetPanel != null)
            {
                _characterSheetPanel.pickingMode = PickingMode.Position;
                // Start hidden and positioned off-screen
                _characterSheetPanel.style.display = DisplayStyle.None;
                _characterSheetPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                _characterSheetPanel.SetEnabled(false);
            }

            // Get the ScrollView for tab content
            _characterSheetScrollView = _root.Q<ScrollView>("charsheet-tab-content");

            _gameLogPanel = _root.Q<VisualElement>("game-log-panel");

            // Ensure game log panel can receive pointer events and starts hidden
            if (_gameLogPanel != null)
            {
                _gameLogPanel.pickingMode = PickingMode.Position;
                // Start hidden and positioned off-screen
                _gameLogPanel.style.display = DisplayStyle.None;
                _gameLogPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                _gameLogPanel.SetEnabled(false);
                
                // Load saved height preference
                // Check version to see if we need to migrate from old default
                int savedVersion = UnityEngine.PlayerPrefs.GetInt(GAME_LOG_HEIGHT_VERSION, 0);
                float savedHeight;
                
                if (savedVersion < GAME_LOG_HEIGHT_VERSION_NUM)
                {
                    // Version mismatch - migrate to new default
                    // If saved value is the old default or close to it (within 50px), update to new default
                    if (UnityEngine.PlayerPrefs.HasKey(GAME_LOG_HEIGHT_PREF))
                    {
                        float oldHeight = UnityEngine.PlayerPrefs.GetFloat(GAME_LOG_HEIGHT_PREF);
                        if (Mathf.Abs(oldHeight - OLD_DEFAULT_GAME_LOG_HEIGHT) < 50f)
                        {
                            // Was using old default, migrate to new default
                            savedHeight = DEFAULT_GAME_LOG_HEIGHT;
                        }
                        else
                        {
                            // User had custom height, keep it
                            savedHeight = oldHeight;
                        }
                    }
                    else
                    {
                        // No saved preference, use new default
                        savedHeight = DEFAULT_GAME_LOG_HEIGHT;
                    }
                    
                    // Save migrated values
                    UnityEngine.PlayerPrefs.SetFloat(GAME_LOG_HEIGHT_PREF, savedHeight);
                    UnityEngine.PlayerPrefs.SetInt(GAME_LOG_HEIGHT_VERSION, GAME_LOG_HEIGHT_VERSION_NUM);
                    UnityEngine.PlayerPrefs.Save();
                }
                else
                {
                    // Version is current, just load the saved height
                    savedHeight = UnityEngine.PlayerPrefs.GetFloat(GAME_LOG_HEIGHT_PREF, DEFAULT_GAME_LOG_HEIGHT);
                }
                
                // Clamp to screen bounds
                savedHeight = ClampGameLogHeightToScreen(savedHeight);
                _gameLogPanel.style.height = savedHeight;
            }

            // Find all character sheet tabs
            _characterSheetTabs = new VisualElement[TOTAL_TABS]
            {
                _root.Q<VisualElement>("tab-overview-content"),
                _root.Q<VisualElement>("tab-skills-content"),
                _root.Q<VisualElement>("tab-actions-content"),
                _root.Q<VisualElement>("tab-spells-content"),
                _root.Q<VisualElement>("tab-inventory-content"),
                _root.Q<VisualElement>("tab-features-content"),
                _root.Q<VisualElement>("tab-rest-content")
            };

            // Wire up tab buttons
            _tabButtons = new Button[TOTAL_TABS]
            {
                _root.Q<Button>("tab-overview"),
                _root.Q<Button>("tab-skills"),
                _root.Q<Button>("tab-actions"),
                _root.Q<Button>("tab-spells"),
                _root.Q<Button>("tab-inventory"),
                _root.Q<Button>("tab-features"),
                _root.Q<Button>("tab-rest")
            };

            _tabButtonHandlers = new System.Action[_tabButtons.Length];
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].pickingMode = PickingMode.Position; // Ensure tab buttons receive pointer events
                    _tabButtonHandlers[i] = () => 
                    {
                        // Only trigger tab click if we're not dragging
                        if (!_isDragging)
                        {
                            TabClicked?.Invoke(tabIndex);
                        }
                    };
                    _tabButtons[i].clicked += _tabButtonHandlers[i];
                }
            }

            // Wire up ability score buttons
            WireAbilityButton("ability-str", "STR");
            WireAbilityButton("ability-dex", "DEX");
            WireAbilityButton("ability-con", "CON");
            WireAbilityButton("ability-int", "INT");
            WireAbilityButton("ability-wis", "WIS");
            WireAbilityButton("ability-cha", "CHA");

            // Wire up skill buttons
            WireSkillButton("skill-acrobatics", "Acrobatics");
            WireSkillButton("skill-animal-handling", "Animal Handling");
            WireSkillButton("skill-arcana", "Arcana");
            WireSkillButton("skill-athletics", "Athletics");
            WireSkillButton("skill-deception", "Deception");
            WireSkillButton("skill-history", "History");
            WireSkillButton("skill-insight", "Insight");
            WireSkillButton("skill-intimidation", "Intimidation");
            WireSkillButton("skill-investigation", "Investigation");
            WireSkillButton("skill-medicine", "Medicine");
            WireSkillButton("skill-nature", "Nature");
            WireSkillButton("skill-perception", "Perception");
            WireSkillButton("skill-performance", "Performance");
            WireSkillButton("skill-persuasion", "Persuasion");
            WireSkillButton("skill-religion", "Religion");
            WireSkillButton("skill-sleight-of-hand", "Sleight of Hand");
            WireSkillButton("skill-stealth", "Stealth");
            WireSkillButton("skill-survival", "Survival");

            // Wire up action buttons
            WireActionButton("action-attack", "Attack");
            WireActionButton("action-dash", "Dash");
            WireActionButton("action-disengage", "Disengage");
            WireActionButton("action-dodge", "Dodge");
            WireActionButton("action-help", "Help");
            WireActionButton("action-hide", "Hide");
            WireActionButton("action-ready", "Ready");
            WireActionButton("action-search", "Search");
            WireActionButton("action-use-object", "Use Object");

            // Wire up attack buttons
            WireAttackButton("attack-longsword", "Longsword");
            WireAttackButton("attack-shortbow", "Shortbow");

            // Wire up feature buttons
            WireFeatureButton("feature-fighting-style", "Fighting Style: Defense");
            WireFeatureButton("feature-second-wind", "Second Wind");
            WireFeatureButton("feature-action-surge", "Action Surge");

            // Wire up rest buttons
            WireRestButton("short-rest-button", "Short Rest");
            WireRestButton("long-rest-button", "Long Rest");

            // Wire up game log clear button
            var clearLogButton = _root.Q<Button>("game-log-clear-button");
            if (clearLogButton != null)
            {
                clearLogButton.pickingMode = PickingMode.Position;
                clearLogButton.clicked += () => ClearLogClicked?.Invoke();
            }

            // Wire up carousel navigation
            _tabsContainer = _root.Q<VisualElement>("charsheet-tabs-container");
            _tabsWrapper = _root.Q<VisualElement>("charsheet-tabs-wrapper");
            _tabNavLeft = _root.Q<Button>("tab-nav-left");
            _tabNavRight = _root.Q<Button>("tab-nav-right");

            if (_tabNavLeft != null)
            {
                _tabNavLeft.clicked += OnTabNavLeftClicked;
                _tabNavLeft.pickingMode = PickingMode.Position;
            }

            if (_tabNavRight != null)
            {
                _tabNavRight.clicked += OnTabNavRightClicked;
                _tabNavRight.pickingMode = PickingMode.Position;
            }

            // Wire up drag functionality for carousel
            if (_tabsWrapper != null)
            {
                _tabsWrapper.RegisterCallback<PointerDownEvent>(OnTabsPointerDown);
                _tabsWrapper.RegisterCallback<PointerMoveEvent>(OnTabsPointerMove);
                _tabsWrapper.RegisterCallback<PointerUpEvent>(OnTabsPointerUp);
                _tabsWrapper.RegisterCallback<PointerLeaveEvent>(OnTabsPointerLeave);
                _tabsWrapper.pickingMode = PickingMode.Position;
            }

            UpdateTabCarousel();

            // Ensure all buttons in the character sheet can receive pointer events
            EnablePickingOnAllButtons(_root);

            // Start with character sheet hidden by default and positioned off-screen
            if (_characterSheetPanel != null)
            {
                _characterSheetPanel.style.display = DisplayStyle.None;
                _characterSheetPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                _characterSheetPanel.SetEnabled(false);
                _characterSheetPanel.pickingMode = PickingMode.Ignore;
            }

            // Start with game log hidden by default and positioned off-screen
            if (_gameLogPanel != null)
            {
                _gameLogPanel.style.display = DisplayStyle.None;
                _gameLogPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                _gameLogPanel.SetEnabled(false);
                _gameLogPanel.pickingMode = PickingMode.Ignore;
            }
        }
        #endregion

        #region IUIView Implementation

        /// <summary>
        /// Recursively enables picking mode on all interactive elements.
        /// </summary>
        private static void EnablePickingOnAllButtons(VisualElement element)
        {
            if (element == null)
                return;

            // Enable picking for interactive elements
            if (IsInteractiveElement(element))
            {
                element.pickingMode = PickingMode.Position;
            }

            // Recursively process children
            foreach (var child in element.Children())
            {
                EnablePickingOnAllButtons(child);
            }
        }

        /// <summary>
        /// Checks if an element is an interactive UI element that should receive pointer events.
        /// </summary>
        private static bool IsInteractiveElement(VisualElement element)
        {
            return element is Button 
                || element is Toggle 
                || element is TextField 
                || element is Slider
                || element is ScrollView;
        }

        public void Show()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                _root.SetEnabled(true);
                _root.pickingMode = PickingMode.Position;
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                _root.SetEnabled(false);
            }
        }
        #endregion

        #region View Updates

        /// <summary>
        /// Update the view based on the latest model state.
        /// </summary>
        /// <param name="state">Snapshot of the in-game UI state.</param>
        public void UpdateView(InGameUIState state)
        {
            // If we're already animating to the target state, don't re-trigger
            // This prevents closing during opening animation
            if (_targetVisibilityState.HasValue && _targetVisibilityState.Value == state.IsCharacterSheetOpen && _currentAnimation != null)
            {
                // Already animating to this state, just update tab
                SetCharacterSheetTab(state.CharacterSheetTabIndex);
                return;
            }
            
            // If we're animating toward visible, consider it visible to prevent auto-closing
            bool currentlyVisible = false;
            if (_characterSheetPanel != null)
            {
                // If animating toward visible, treat as visible
                if (_targetVisibilityState == true && _currentAnimation != null)
                {
                    currentlyVisible = true;
                }
                else
                {
                    bool isDisplayed = _characterSheetPanel.resolvedStyle.display == DisplayStyle.Flex;
                    if (isDisplayed)
                    {
                        // Check if panel is actually on-screen (within reasonable distance of on-screen position)
                        float currentRight = _characterSheetPanel.resolvedStyle.right;
                        if (!float.IsNaN(currentRight))
                        {
                            // Consider visible if within 100px of on-screen position (accounts for animation)
                            float distanceFromOnScreen = Mathf.Abs(currentRight - PANEL_ONSCREEN_RIGHT);
                            currentlyVisible = distanceFromOnScreen < 100f;
                        }
                        else
                        {
                            // If position is invalid but displayed, check style property as fallback
                            float styleRight = _characterSheetPanel.style.right.value.value;
                            if (!float.IsNaN(styleRight))
                            {
                                float distanceFromOnScreen = Mathf.Abs(styleRight - PANEL_ONSCREEN_RIGHT);
                                currentlyVisible = distanceFromOnScreen < 100f;
                            }
                            else
                            {
                                // If both are invalid but displayed, assume it's visible (might be initializing)
                                currentlyVisible = true;
                            }
                        }
                    }
                }
            }
            
            // Only update visibility if the state actually changed
            // AND we're not already animating to that state
            // AND enough time has passed since last change (prevents rapid toggling)
            if (state.IsCharacterSheetOpen != currentlyVisible)
            {
                float timeSinceLastChange = Time.time - _lastVisibilityChangeTime;
                // Don't close if we're currently animating toward visible
                // Also prevent rapid toggling (wait at least 0.1 seconds between changes)
                bool shouldUpdate = !(state.IsCharacterSheetOpen == false && _targetVisibilityState == true && _currentAnimation != null);
                shouldUpdate = shouldUpdate && (timeSinceLastChange > 0.1f || _targetVisibilityState == null);
                
                if (shouldUpdate)
                {
                    SetCharacterSheetVisible(state.IsCharacterSheetOpen);
                    _lastVisibilityChangeTime = Time.time;
                }
            }
            
            // Always update tab (no animation needed)
            SetCharacterSheetTab(state.CharacterSheetTabIndex);
        }

        public void SetCharacterSheetVisible(bool isVisible)
        {
            if (_characterSheetPanel == null)
            {
                Debug.LogWarning("InGameUIView: Character sheet panel is null!");
                return;
            }

            // Stop any ongoing animation and capture current position
            float currentRight = GetCurrentPanelPosition(_characterSheetPanel, isVisible);
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
                _targetVisibilityState = null; // Clear animation target when stopping
                // Capture current position before starting new animation
                currentRight = GetPanelRightPosition(_characterSheetPanel);
                if (float.IsNaN(currentRight))
                {
                    currentRight = isVisible ? PANEL_OFFSCREEN_RIGHT : PANEL_ONSCREEN_RIGHT;
                }
            }

            if (isVisible)
            {
                // Mark that we're animating toward visible state
                _targetVisibilityState = true;
                
                // Make it visible first
                _characterSheetPanel.style.display = DisplayStyle.Flex;
                _characterSheetPanel.SetEnabled(true);
                _characterSheetPanel.pickingMode = PickingMode.Position;
                
                // Set starting position (use current if animating, otherwise start off-screen)
                _characterSheetPanel.style.right = currentRight;
                _characterSheetPanel.MarkDirtyRepaint();
                
                EnablePickingOnAllButtons(_characterSheetPanel);
                
                // Ensure character sheet fits on screen
                ClampCharacterSheetToScreen();

                // Prepare and show game log panel
                PrepareGameLogPanelForAnimation(true);
                
                // Start both animations simultaneously to keep them in sync
                _currentAnimation = StartCoroutine(AnimateSlideInCoroutine(_characterSheetPanel, currentRight, PANEL_ONSCREEN_RIGHT));
                StartGameLogAnimation(true);
            }
            else
            {
                // Mark that we're animating toward hidden state
                _targetVisibilityState = false;
                
                // Clear keyboard selection when closing
                if (_characterSheetTabs != null)
                {
                    for (int i = 0; i < _characterSheetTabs.Length; i++)
                    {
                        ClearButtonSelection(i);
                    }
                }
                
                // Check if panel is already mostly off-screen - if so, skip animation for instant close
                float distanceToOffScreen = Mathf.Abs(currentRight - PANEL_OFFSCREEN_RIGHT);
                bool shouldAnimate = distanceToOffScreen > INSTANT_CLOSE_DISTANCE_THRESHOLD;
                
                if (shouldAnimate)
                {
                    // Animate slide out to right from current position
                    _currentAnimation = StartCoroutine(AnimateSlideOutCoroutine(_characterSheetPanel, currentRight, PANEL_OFFSCREEN_RIGHT, () =>
                    {
                        _characterSheetPanel.style.display = DisplayStyle.None;
                        _characterSheetPanel.SetEnabled(false);
                        _characterSheetPanel.pickingMode = PickingMode.Ignore;
                        _currentAnimation = null;
                    }));
                }
                else
                {
                    // Already mostly off-screen, close instantly
                    _characterSheetPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                    _characterSheetPanel.style.display = DisplayStyle.None;
                    _characterSheetPanel.SetEnabled(false);
                    _characterSheetPanel.pickingMode = PickingMode.Ignore;
                    _currentAnimation = null;
                }

                // Hide game log panel
                HideGameLogPanel();
            }
        }

        /// <summary>
        /// Gets the right position of a panel, with fallback to style property if resolvedStyle is invalid.
        /// </summary>
        private float GetPanelRightPosition(VisualElement panel)
        {
            float position = panel.resolvedStyle.right;
            if (float.IsNaN(position))
            {
                position = panel.style.right.value.value;
            }
            return position;
        }

        /// <summary>
        /// Gets the current panel position, with appropriate defaults based on visibility state.
        /// </summary>
        private float GetCurrentPanelPosition(VisualElement panel, bool targetVisible)
        {
            if (panel.resolvedStyle.display == DisplayStyle.Flex)
            {
                float position = GetPanelRightPosition(panel);
                if (!float.IsNaN(position))
                {
                    return position;
                }
            }
            // Default based on target state
            return targetVisible ? PANEL_OFFSCREEN_RIGHT : PANEL_ONSCREEN_RIGHT;
        }

        /// <summary>
        /// Prepares the game log panel for animation (showing or hiding).
        /// </summary>
        private void PrepareGameLogPanelForAnimation(bool isVisible)
        {
            if (_gameLogPanel == null)
                return;

            // Stop any ongoing game log animation
            if (_gameLogAnimation != null)
            {
                StopCoroutine(_gameLogAnimation);
                _gameLogAnimation = null;
            }

            float gameLogCurrentRight = GetPanelRightPosition(_gameLogPanel);
            if (float.IsNaN(gameLogCurrentRight))
            {
                gameLogCurrentRight = isVisible ? PANEL_OFFSCREEN_RIGHT : PANEL_ONSCREEN_RIGHT;
            }

            _gameLogPanel.style.display = DisplayStyle.Flex;
            _gameLogPanel.SetEnabled(true);
            _gameLogPanel.pickingMode = PickingMode.Position;
            _gameLogPanel.style.right = gameLogCurrentRight;

            if (isVisible)
            {
                // Ensure game log height fits on screen
                float currentHeight = _gameLogPanel.resolvedStyle.height;
                float clampedHeight = ClampGameLogHeightToScreen(currentHeight);
                if (clampedHeight != currentHeight)
                {
                    _gameLogPanel.style.height = clampedHeight;
                }

                // Re-enable picking on buttons after showing
                EnablePickingOnAllButtons(_gameLogPanel);
            }

            _gameLogPanel.MarkDirtyRepaint();
        }

        /// <summary>
        /// Starts the game log panel animation (slide in or out).
        /// </summary>
        private void StartGameLogAnimation(bool slideIn)
        {
            if (_gameLogPanel == null)
                return;

            float gameLogStartRight = GetPanelRightPosition(_gameLogPanel);
            if (float.IsNaN(gameLogStartRight))
            {
                gameLogStartRight = slideIn ? PANEL_OFFSCREEN_RIGHT : PANEL_ONSCREEN_RIGHT;
            }

            float targetRight = slideIn ? PANEL_ONSCREEN_RIGHT : PANEL_OFFSCREEN_RIGHT;
            _gameLogAnimation = StartCoroutine(
                slideIn 
                    ? AnimateSlideInCoroutine(_gameLogPanel, gameLogStartRight, targetRight)
                    : AnimateSlideOutCoroutine(_gameLogPanel, gameLogStartRight, targetRight, () =>
                    {
                        _gameLogPanel.style.display = DisplayStyle.None;
                        _gameLogPanel.SetEnabled(false);
                        _gameLogPanel.pickingMode = PickingMode.Ignore;
                        _gameLogAnimation = null;
                    })
            );
        }

        /// <summary>
        /// Hides the game log panel, with instant close if already mostly off-screen.
        /// </summary>
        private void HideGameLogPanel()
        {
            if (_gameLogPanel == null)
                return;

            // Stop any ongoing game log animation
            if (_gameLogAnimation != null)
            {
                StopCoroutine(_gameLogAnimation);
                _gameLogAnimation = null;
            }

            float gameLogCurrentRight = GetPanelRightPosition(_gameLogPanel);
            if (float.IsNaN(gameLogCurrentRight))
            {
                gameLogCurrentRight = PANEL_ONSCREEN_RIGHT;
            }

            float gameLogDistanceToOffScreen = Mathf.Abs(gameLogCurrentRight - PANEL_OFFSCREEN_RIGHT);
            if (gameLogDistanceToOffScreen > INSTANT_CLOSE_DISTANCE_THRESHOLD)
            {
                StartGameLogAnimation(false);
            }
            else
            {
                // Close instantly
                _gameLogPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                _gameLogPanel.style.display = DisplayStyle.None;
                _gameLogPanel.SetEnabled(false);
                _gameLogPanel.pickingMode = PickingMode.Ignore;
                _gameLogAnimation = null;
            }
        }

        public void SetCharacterSheetTab(int tabIndex)
        {
            if (_characterSheetTabs == null || _tabButtons == null)
                return;

            // Clear keyboard selection when changing tabs
            for (int i = 0; i < _characterSheetTabs.Length; i++)
            {
                if (i != tabIndex && _characterSheetTabs[i] != null)
                {
                    ClearButtonSelection(i);
                }
            }

            // Show/hide tab content
            for (int i = 0; i < _characterSheetTabs.Length; i++)
            {
                if (_characterSheetTabs[i] != null)
                {
                    bool isActive = (i == tabIndex);
                    _characterSheetTabs[i].style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                    _characterSheetTabs[i].pickingMode = isActive ? PickingMode.Position : PickingMode.Ignore;
                    
                    // Update tab button active state
                    if (_tabButtons[i] != null)
                    {
                        if (isActive)
                        {
                            _tabButtons[i].AddToClassList("active");
                        }
                        else
                        {
                            _tabButtons[i].RemoveFromClassList("active");
                        }
                    }
                }
            }

            // Ensure active tab is visible in carousel
            EnsureTabVisible(tabIndex);
        }
        #endregion

        #region Carousel Navigation

        /// <summary>
        /// Ensures the specified tab is visible in the carousel by adjusting the offset.
        /// </summary>
        private void EnsureTabVisible(int tabIndex)
        {
            if (tabIndex < _currentTabOffset)
            {
                // Tab is to the left of visible area, scroll left
                _currentTabOffset = tabIndex;
            }
            else if (tabIndex >= _currentTabOffset + VISIBLE_TAB_COUNT)
            {
                // Tab is to the right of visible area, scroll right
                _currentTabOffset = Mathf.Max(0, tabIndex - VISIBLE_TAB_COUNT + 1);
            }

            UpdateTabCarousel();
        }

        /// <summary>
        /// Updates the carousel display and navigation button states.
        /// </summary>
        private void UpdateTabCarousel()
        {
            if (_tabButtons == null || _tabNavLeft == null || _tabNavRight == null)
                return;

            int totalTabs = _tabButtons.Length;
            
            // If we have fewer tabs than visible, show all and disable navigation
            if (totalTabs <= VISIBLE_TAB_COUNT)
            {
                _tabNavLeft.SetEnabled(false);
                _tabNavRight.SetEnabled(false);
                
                // Show all tabs
                for (int i = 0; i < totalTabs; i++)
                {
                    if (_tabButtons[i] != null)
                    {
                        _tabButtons[i].style.display = DisplayStyle.Flex;
                    }
                }
                return;
            }

            // Update navigation button states (wrap around)
            _tabNavLeft.SetEnabled(true); // Always enabled for wrap-around
            _tabNavRight.SetEnabled(true); // Always enabled for wrap-around

            // Show visible tabs - wrap around if needed
            for (int i = 0; i < totalTabs; i++)
            {
                if (_tabButtons[i] != null)
                {
                    // Calculate if this tab should be visible (with wrap-around)
                    bool isVisible = false;
                    
                    if (_currentTabOffset + VISIBLE_TAB_COUNT <= totalTabs)
                    {
                        // Normal case - no wrap needed
                        isVisible = i >= _currentTabOffset && i < _currentTabOffset + VISIBLE_TAB_COUNT;
                    }
                    else
                    {
                        // Wrap around case
                        int overflow = (_currentTabOffset + VISIBLE_TAB_COUNT) - totalTabs;
                        isVisible = (i >= _currentTabOffset) || (i < overflow);
                    }
                    
                    _tabButtons[i].style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        #endregion

        #region Carousel Event Handlers
        /// <summary>
        /// Handles left navigation button click.
        /// </summary>
        private void OnTabNavLeftClicked()
        {
            if (_tabButtons == null || _tabButtons.Length == 0)
                return;
                
            _currentTabOffset--;
            
            // Wrap around
            int maxOffset = Mathf.Max(0, _tabButtons.Length - VISIBLE_TAB_COUNT);
            if (_currentTabOffset < 0)
            {
                _currentTabOffset = maxOffset;
            }
            
            UpdateTabCarousel();
        }

        /// <summary>
        /// Handles right navigation button click.
        /// </summary>
        private void OnTabNavRightClicked()
        {
            if (_tabButtons == null || _tabButtons.Length == 0)
                return;
                
            _currentTabOffset++;
            
            // Wrap around
            int maxOffset = Mathf.Max(0, _tabButtons.Length - VISIBLE_TAB_COUNT);
            if (_currentTabOffset > maxOffset)
            {
                _currentTabOffset = 0;
            }
            
            UpdateTabCarousel();
        }
        #endregion

        #region Drag Functionality
        /// <summary>
        /// Handles pointer down event for drag functionality.
        /// </summary>
        private void OnTabsPointerDown(PointerDownEvent evt)
        {
            _isDragging = true;
            _dragStartX = evt.position.x;
            _dragStartOffset = _currentTabOffset;
            _tabsWrapper.CapturePointer(evt.pointerId);
        }

        /// <summary>
        /// Handles pointer move event for drag functionality.
        /// </summary>
        private void OnTabsPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging)
                return;

            float deltaX = evt.position.x - _dragStartX;
            float dragDistance = Mathf.Abs(deltaX);

            // Only start dragging if we've moved past the threshold
            if (dragDistance > DRAG_THRESHOLD)
            {
                // Calculate how many tabs to scroll based on drag distance
                int tabsToScroll = Mathf.RoundToInt(deltaX / TAB_WIDTH);

                int newOffset = Mathf.RoundToInt(_dragStartOffset - tabsToScroll);
                newOffset = Mathf.Clamp(newOffset, 0, Mathf.Max(0, _tabButtons.Length - VISIBLE_TAB_COUNT));

                if (newOffset != _currentTabOffset)
                {
                    _currentTabOffset = newOffset;
                    UpdateTabCarousel();
                }
            }
        }

        /// <summary>
        /// Handles pointer up event for drag functionality.
        /// </summary>
        private void OnTabsPointerUp(PointerUpEvent evt)
        {
            if (_isDragging)
            {
                // Small delay before allowing clicks again to prevent accidental tab selection
                _tabsWrapper.schedule.Execute(() =>
                {
                    _isDragging = false;
                }).ExecuteLater(DRAG_CLICK_DELAY_MS);
                _tabsWrapper.ReleasePointer(evt.pointerId);
            }
        }
        #endregion

        #region Animations

        /// <summary>
        /// Handles pointer leave event to stop dragging.
        /// </summary>
        private void OnTabsPointerLeave(PointerLeaveEvent evt)
        {
            if (_isDragging)
            {
                _isDragging = false;
            }
        }

        /// <summary>
        /// Coroutine to animate the panel sliding in.
        /// </summary>
        private System.Collections.IEnumerator AnimateSlideInCoroutine(VisualElement element, float startRight, float endRight)
        {
            float elapsed = 0f;
            bool isCharacterSheet = (element == _characterSheetPanel);

            while (elapsed < ANIMATION_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ANIMATION_DURATION);
                
                // Ease out cubic for smooth animation
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                float currentRight = Mathf.Lerp(startRight, endRight, t);
                element.style.right = currentRight;
                
                // Only mark dirty every few frames to reduce stutter
                if (elapsed % 0.016f < Time.deltaTime) // ~60fps updates
                {
                    element.MarkDirtyRepaint();
                }

                yield return null;
            }

            // Ensure final position is set
            element.style.right = endRight;
            element.MarkDirtyRepaint();
            
            if (isCharacterSheet)
            {
                _currentAnimation = null;
                _targetVisibilityState = null; // Clear animation target when complete
            }
            else
            {
                _gameLogAnimation = null;
            }
        }

        /// <summary>
        /// Coroutine to animate the panel sliding out.
        /// </summary>
        private System.Collections.IEnumerator AnimateSlideOutCoroutine(VisualElement element, float startRight, float endRight, System.Action onComplete)
        {
            float elapsed = 0f;
            bool isCharacterSheet = (element == _characterSheetPanel);

            while (elapsed < ANIMATION_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ANIMATION_DURATION);
                
                // Ease in cubic for smooth animation
                t = t * t * t;
                
                float currentRight = Mathf.Lerp(startRight, endRight, t);
                element.style.right = currentRight;
                
                // Mark dirty every frame for smooth animation
                element.MarkDirtyRepaint();

                yield return null;
            }

            // Ensure final position is set
            element.style.right = endRight;
            element.MarkDirtyRepaint();
            
            // Call completion callback
            if (isCharacterSheet)
            {
                _currentAnimation = null;
            }
            else
            {
                _gameLogAnimation = null;
            }
            onComplete?.Invoke();
        }
        #endregion

        #region Button Wiring

        /// <summary>
        /// Wires up all buttons with the given name across all tabs.
        /// Follows DRY principle by centralizing the common wiring pattern.
        /// </summary>
        /// <param name="buttonName">The name of the buttons to wire up.</param>
        /// <param name="onClick">The action to invoke when any of the buttons is clicked.</param>
        private void WireButtons(string buttonName, System.Action onClick)
        {
            var buttons = _root.Query<Button>(buttonName).ToList();
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.pickingMode = PickingMode.Position;
                    button.clicked += onClick;
                }
            }
        }

        /// <summary>
        /// Wires up an ability score button click event.
        /// Wires ALL buttons with this name across all tabs (not just the first one found).
        /// </summary>
        private void WireAbilityButton(string buttonName, string abilityName)
        {
            WireButtons(buttonName, () => AbilityScoreClicked?.Invoke(abilityName));
        }

        /// <summary>
        /// Wires up a skill button click event.
        /// Wires ALL buttons with this name across all tabs (not just the first one found).
        /// </summary>
        private void WireSkillButton(string buttonName, string skillName)
        {
            WireButtons(buttonName, () => SkillClicked?.Invoke(skillName));
        }

        /// <summary>
        /// Wires up an action button click event.
        /// Wires ALL buttons with this name across all tabs (not just the first one found).
        /// </summary>
        private void WireActionButton(string buttonName, string actionName)
        {
            WireButtons(buttonName, () => ActionClicked?.Invoke(actionName));
        }

        /// <summary>
        /// Wires up an attack button click event.
        /// Wires ALL buttons with this name across all tabs (not just the first one found).
        /// </summary>
        private void WireAttackButton(string buttonName, string attackName)
        {
            WireButtons(buttonName, () => AttackClicked?.Invoke(attackName));
        }

        /// <summary>
        /// Wires up a feature button click event.
        /// Wires ALL buttons with this name across all tabs (not just the first one found).
        /// </summary>
        private void WireFeatureButton(string buttonName, string featureName)
        {
            WireButtons(buttonName, () => FeatureClicked?.Invoke(featureName));
        }

        /// <summary>
        /// Wires up a rest button click event.
        /// Wires ALL buttons with this name across all tabs (not just the first one found).
        /// </summary>
        private void WireRestButton(string buttonName, string restType)
        {
            WireButtons(buttonName, () => RestClicked?.Invoke(restType));
        }
        #endregion

        #region Game Log Methods

        #region Constants
        private const int MAX_LOG_ENTRIES = 100;
        #endregion

        /// <summary>
        /// Adds a new entry to the game log using structured data.
        /// Follows Single Responsibility Principle by delegating to focused helper methods.
        /// </summary>
        /// <param name="entry">The formatted log entry data.</param>
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
        /// Validates that the game log panel exists.
        /// </summary>
        private bool ValidateGameLogPanel()
        {
            if (_gameLogPanel == null)
            {
                Debug.LogWarning("InGameUIView: Game log panel is null!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the log entries container, validating it exists.
        /// </summary>
        private VisualElement GetLogEntriesContainer()
        {
            var logEntries = _root.Q<VisualElement>("game-log-entries");
            if (logEntries == null)
            {
                Debug.LogWarning("InGameUIView: Game log entries container is null!");
            }
            return logEntries;
        }

        /// <summary>
        /// Creates a complete log entry card with header and content.
        /// Follows Single Responsibility Principle by delegating to focused methods.
        /// </summary>
        private VisualElement CreateLogEntryCard(FormattedLogEntry entry)
        {
            var card = CreateCardContainer(entry);
            var cardHeader = CreateLogEntryHeader(entry, card);
            var mainContent = CreateLogEntryContent(entry);

            card.Add(cardHeader);
            card.Add(mainContent);

            return card;
        }

        /// <summary>
        /// Creates the base card container element.
        /// </summary>
        private VisualElement CreateCardContainer(FormattedLogEntry entry)
        {
            var card = new VisualElement();
            card.AddToClassList("game-log-card");
            card.AddToClassList(entry.CssClass);
            card.pickingMode = PickingMode.Ignore;
            return card;
        }

        /// <summary>
        /// Creates the log entry header with character name and delete button.
        /// </summary>
        private VisualElement CreateLogEntryHeader(FormattedLogEntry entry, VisualElement card)
        {
            var cardHeader = new VisualElement();
            cardHeader.AddToClassList("game-log-card-header");

            if (!string.IsNullOrEmpty(entry.CharacterName))
            {
                var characterNameLabel = CreateCharacterNameLabel(entry.CharacterName);
                cardHeader.Add(characterNameLabel);
            }

            var deleteButton = CreateDeleteButton(card);
            cardHeader.Add(deleteButton);

            return cardHeader;
        }

        /// <summary>
        /// Creates a character name label for the log entry header.
        /// </summary>
        private Label CreateCharacterNameLabel(string characterName)
        {
            var label = new Label(characterName);
            label.AddToClassList("game-log-character-name");
            return label;
        }

        /// <summary>
        /// Creates a delete button for a log entry card.
        /// </summary>
        private Button CreateDeleteButton(VisualElement card)
        {
            var deleteButton = new Button();
            deleteButton.AddToClassList("game-log-delete-button");
            deleteButton.text = "×";
            deleteButton.tooltip = "Delete this entry";
            deleteButton.pickingMode = PickingMode.Position;
            deleteButton.clicked += () => LogEntryDeleteClicked?.Invoke(card);
            return deleteButton;
        }

        /// <summary>
        /// Creates the main content area of a log entry.
        /// </summary>
        private VisualElement CreateLogEntryContent(FormattedLogEntry entry)
        {
            var mainContent = new VisualElement();
            mainContent.AddToClassList("game-log-main-content");

            var actionRow = CreateActionRow(entry);
            mainContent.Add(actionRow);

            if (!string.IsNullOrEmpty(entry.DiceFormula))
            {
                var formulaLabel = CreateFormulaLabel(entry.DiceFormula);
                mainContent.Add(formulaLabel);
            }

            if (!string.IsNullOrEmpty(entry.DiceBreakdown))
            {
                var diceBreakdownLabel = CreateDiceBreakdownLabel(entry.DiceBreakdown);
                mainContent.Add(diceBreakdownLabel);
            }

            if (entry.Result.HasValue)
            {
                var resultLabel = CreateResultLabel(entry.Result.Value);
                mainContent.Add(resultLabel);
            }

            var timestampLabel = CreateTimestampLabel();
            mainContent.Add(timestampLabel);

            return mainContent;
        }

        /// <summary>
        /// Creates the action row with action type and sub-action.
        /// </summary>
        private VisualElement CreateActionRow(FormattedLogEntry entry)
        {
            var actionRow = new VisualElement();
            actionRow.AddToClassList("game-log-action-row");

            var actionTypeLabel = new Label(entry.ActionType);
            actionTypeLabel.AddToClassList("game-log-action-type");
            actionRow.Add(actionTypeLabel);

            if (!string.IsNullOrEmpty(entry.SubActionType))
            {
                var subActionLabel = CreateSubActionLabel(entry.SubActionType, entry.CssClass);
                actionRow.Add(subActionLabel);
            }

            return actionRow;
        }

        /// <summary>
        /// Creates a sub-action label with appropriate styling.
        /// </summary>
        private Label CreateSubActionLabel(string subActionType, string cssClass)
        {
            var subActionLabel = new Label(subActionType);
            subActionLabel.AddToClassList("game-log-sub-action");
            subActionLabel.AddToClassList($"sub-action-{cssClass.Replace("log-", "")}");
            return subActionLabel;
        }

        /// <summary>
        /// Creates a dice formula label.
        /// </summary>
        private Label CreateFormulaLabel(string diceFormula)
        {
            var formulaLabel = new Label(diceFormula);
            formulaLabel.AddToClassList("game-log-dice-formula");
            return formulaLabel;
        }

        /// <summary>
        /// Creates a dice breakdown label.
        /// </summary>
        private Label CreateDiceBreakdownLabel(string diceBreakdown)
        {
            var diceBreakdownLabel = new Label(diceBreakdown);
            diceBreakdownLabel.AddToClassList("game-log-dice-breakdown");
            return diceBreakdownLabel;
        }

        /// <summary>
        /// Creates a result label with the roll result.
        /// </summary>
        private Label CreateResultLabel(int result)
        {
            var resultLabel = new Label(result.ToString());
            resultLabel.AddToClassList("game-log-result");
            return resultLabel;
        }

        /// <summary>
        /// Creates a timestamp label with current time.
        /// </summary>
        private Label CreateTimestampLabel()
        {
            var timestamp = System.DateTime.Now.ToString("h:mm tt");
            var timestampLabel = new Label(timestamp);
            timestampLabel.AddToClassList("game-log-timestamp");
            return timestampLabel;
        }

        /// <summary>
        /// Scrolls the game log to the bottom to show the newest entry.
        /// </summary>
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

        /// <summary>
        /// Enforces the maximum number of log entries to prevent performance issues.
        /// </summary>
        private void EnforceLogEntryLimit(VisualElement logEntries)
        {
            while (logEntries.childCount > MAX_LOG_ENTRIES)
            {
                var firstChild = logEntries[0];
                logEntries.Remove(firstChild);
            }
        }

        /// <summary>
        /// Legacy method for simple text entries (for system messages, etc.).
        /// </summary>
        public void AddLogEntry(string message, string cssClass = "game-log-entry")
        {
            var entry = new FormattedLogEntry
            {
                CharacterName = "System",
                ActionType = "",
                SubActionType = "",
                DiceFormula = "",
                DiceBreakdown = "",
                Result = null,
                CssClass = cssClass,
                FullMessage = message
            };
            AddLogEntry(entry);
        }

        /// <summary>
        /// Clears all entries from the game log.
        /// </summary>
        public void ClearLog()
        {
            if (_gameLogPanel == null)
            {
                Debug.LogWarning("InGameUIView: Game log panel is null!");
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
        /// <param name="entryCard">The log entry card element to remove.</param>
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

        #region Button Navigation

        /// <summary>
        /// Gets all focusable buttons in the specified tab content.
        /// </summary>
        public List<Button> GetButtonsInTab(int tabIndex)
        {
            var buttons = new List<Button>();
            
            if (_characterSheetTabs == null)
                return buttons;

            if (tabIndex < 0 || tabIndex >= _characterSheetTabs.Length)
                return buttons;

            var currentTab = _characterSheetTabs[tabIndex];
            if (currentTab == null)
                return buttons;

            // Recursively find all buttons in the current tab
            FindButtonsRecursive(currentTab, buttons);
            
            return buttons;
        }

        /// <summary>
        /// Gets the index of the button currently under the mouse cursor.
        /// Returns -1 if no button is hovered.
        /// </summary>
        public int GetHoveredButtonIndex(int tabIndex)
        {
            if (!IsCharacterSheetOpen() || _uiDocument == null)
                return -1;

            var buttons = GetButtonsInTab(tabIndex);
            if (buttons.Count == 0)
                return -1;

            // Get mouse position
            Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null)
                return -1;
            mousePosition = mouse.position.ReadValue();
#else
            mousePosition = Input.mousePosition;
#endif

            // Get the panel
            var panel = _uiDocument.rootVisualElement.panel;
            if (panel == null)
                return -1;

            // Convert screen coordinates to panel space
            float screenHeight = Screen.height;
            Vector2 panelSpacePos = new Vector2(
                mousePosition.x,
                screenHeight - mousePosition.y
            );

            // Check each button to see if mouse is over it
            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || !button.enabledInHierarchy)
                    continue;

                // Check if button is visible
                if (button.resolvedStyle.display == DisplayStyle.None ||
                    button.resolvedStyle.visibility == Visibility.Hidden)
                    continue;

                // Check if mouse is within button bounds
                Rect buttonRect = button.worldBound;
                if (buttonRect.Contains(panelSpacePos))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Recursively finds all buttons in a visual element tree.
        /// </summary>
        private void FindButtonsRecursive(VisualElement element, List<Button> buttons)
        {
            if (element == null)
                return;

            // Skip elements that are not visible or enabled
            if (element.resolvedStyle.display == DisplayStyle.None || 
                !element.enabledInHierarchy ||
                element.resolvedStyle.visibility == Visibility.Hidden)
            {
                return;
            }

            // Check if this element is a button
            if (element is Button button)
            {
                // Exclude tab navigation buttons and tab buttons themselves
                if (button != _tabNavLeft && button != _tabNavRight && 
                    !IsTabButton(button) &&
                    button.enabledInHierarchy &&
                    element.resolvedStyle.display == DisplayStyle.Flex)
                {
                    buttons.Add(button);
                }
            }

            // Recursively search children
            foreach (var child in element.Children())
            {
                FindButtonsRecursive(child, buttons);
            }
        }

        /// <summary>
        /// Checks if a button is one of the tab buttons.
        /// </summary>
        private bool IsTabButton(Button button)
        {
            if (_tabButtons == null)
                return false;

            foreach (var tabButton in _tabButtons)
            {
                if (tabButton == button)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the selected button index and updates visual feedback.
        /// </summary>
        public void SetSelectedButtonIndex(int tabIndex, int buttonIndex)
        {
            var buttons = GetButtonsInTab(tabIndex);
            if (buttons.Count == 0)
                return;

            // Clamp index to valid range
            buttonIndex = Mathf.Clamp(buttonIndex, 0, buttons.Count - 1);

            // Remove highlight from all buttons in this tab
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.RemoveFromClassList("keyboard-selected");
                }
            }

            // Add highlight to selected button
            if (buttonIndex >= 0 && buttonIndex < buttons.Count && buttons[buttonIndex] != null)
            {
                buttons[buttonIndex].AddToClassList("keyboard-selected");
                
                // Scroll the button into view if needed
                ScrollButtonIntoView(buttons[buttonIndex]);
            }
        }

        /// <summary>
        /// Scrolls a button into view within the scroll view.
        /// </summary>
        private void ScrollButtonIntoView(Button button)
        {
            if (_characterSheetScrollView == null || button == null)
                return;

            // Get the button's world position
            Rect buttonRect = button.worldBound;
            Rect scrollViewRect = _characterSheetScrollView.worldBound;

            // Check if button is outside the visible area
            if (buttonRect.yMin < scrollViewRect.yMin)
            {
                // Button is above visible area, scroll up
                float scrollAmount = scrollViewRect.yMin - buttonRect.yMin + 10f; // 10px padding
                ScrollTabContent(-scrollAmount);
            }
            else if (buttonRect.yMax > scrollViewRect.yMax)
            {
                // Button is below visible area, scroll down
                float scrollAmount = buttonRect.yMax - scrollViewRect.yMax + 10f; // 10px padding
                ScrollTabContent(scrollAmount);
            }
        }

        /// <summary>
        /// Gets the currently selected button and triggers its click event.
        /// </summary>
        public bool ActivateSelectedButton(int tabIndex, int buttonIndex)
        {
            var buttons = GetButtonsInTab(tabIndex);
            if (buttonIndex < 0 || buttonIndex >= buttons.Count)
                return false;

            var button = buttons[buttonIndex];
            if (button != null && button.enabledInHierarchy)
            {
                // Ensure button is focusable for keyboard navigation
                if (!button.focusable)
                {
                    button.focusable = true;
                }
                
                // Focus the button first (required for UI Toolkit keyboard navigation)
                button.Focus();
                
                // Send a NavigationSubmitEvent which is the standard way to trigger button clicks via keyboard in UI Toolkit
                using (var submitEvent = NavigationSubmitEvent.GetPooled())
                {
                    submitEvent.target = button;
                    button.SendEvent(submitEvent);
                }
                
                // Also try sending a ClickEvent as a fallback
                // This simulates an actual mouse click
                using (var clickEvent = ClickEvent.GetPooled())
                {
                    clickEvent.target = button;
                    button.SendEvent(clickEvent);
                }
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears the keyboard selection highlight from all buttons in the specified tab.
        /// </summary>
        public void ClearButtonSelection(int tabIndex)
        {
            var buttons = GetButtonsInTab(tabIndex);
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.RemoveFromClassList("keyboard-selected");
                }
            }
        }

        #endregion

        #region Screen Bounds Helpers

        /// <summary>
        /// Clamps the game log height to ensure it doesn't go off-screen.
        /// </summary>
        private float ClampGameLogHeightToScreen(float height)
        {
            if (_gameLogPanel == null || _root == null)
                return height;

            // Get screen resolution
            float screenHeight = Screen.height;

            // Get panel position (top) - game log is positioned dynamically based on character sheet
            float panelTop = _gameLogPanel.resolvedStyle.top;
            if (float.IsNaN(panelTop) || panelTop <= 0)
            {
                // Calculate based on character sheet position if not set
                if (_characterSheetPanel != null)
                {
                    float charSheetTop = _characterSheetPanel.resolvedStyle.top;
                    float charSheetHeight = _characterSheetPanel.resolvedStyle.height;
                    if (!float.IsNaN(charSheetTop) && !float.IsNaN(charSheetHeight))
                    {
                        panelTop = charSheetTop + charSheetHeight + 10f; // 10px spacing
                    }
                    else
                    {
                        panelTop = 815f; // Fallback (5px + 800px + 10px)
                    }
                }
                else
                {
                    panelTop = 815f; // Fallback
                }
            }

            // Calculate maximum height based on screen bounds
            // Panel top + height should not exceed screen height - buffer
            float maxHeightFromScreen = screenHeight - panelTop - SCREEN_EDGE_BUFFER;
            
            // Return the minimum of requested height and screen-constrained height
            return Mathf.Min(height, maxHeightFromScreen);
        }

        /// <summary>
        /// Ensures the character sheet panel doesn't go off-screen vertically.
        /// </summary>
        private void ClampCharacterSheetToScreen()
        {
            if (_characterSheetPanel == null)
                return;

            float screenHeight = Screen.height;
            float panelTop = SCREEN_EDGE_BUFFER; // Character sheet top position (5px from top)
            float currentHeight = _characterSheetPanel.resolvedStyle.height;
            
            // If panel height would go off-screen, clamp it
            float maxHeight = screenHeight - panelTop - SCREEN_EDGE_BUFFER;
            if (currentHeight > maxHeight)
            {
                _characterSheetPanel.style.height = maxHeight;
            }
        }

        /// <summary>
        /// Scrolls the character sheet tab content up or down.
        /// </summary>
        /// <param name="scrollAmount">Positive value scrolls down, negative scrolls up.</param>
        public void ScrollTabContent(float scrollAmount)
        {
            if (_characterSheetScrollView == null)
                return;

            Vector2 currentOffset = _characterSheetScrollView.scrollOffset;
            float newY = currentOffset.y + scrollAmount;
            
            // Clamp to valid scroll range
            float maxScroll = _characterSheetScrollView.contentContainer.layout.height - _characterSheetScrollView.contentViewport.layout.height;
            newY = Mathf.Clamp(newY, 0f, Mathf.Max(0f, maxScroll));
            
            _characterSheetScrollView.scrollOffset = new Vector2(currentOffset.x, newY);
        }

        #endregion
    }
}

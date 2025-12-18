using GameCore.UI;
using GameCore.UI.InGame.Services;
using GameCore.UI.InGame.Models;
using UnityEngine;
using UnityEngine.UIElements;

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
        #endregion

        #region Public Properties

        public VisualElement Root => _root;
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
            // Only update visibility if it changed
            bool currentlyVisible = _characterSheetPanel != null && 
                                   _characterSheetPanel.style.display == DisplayStyle.Flex;
            
            if (state.IsCharacterSheetOpen != currentlyVisible)
            {
                SetCharacterSheetVisible(state.IsCharacterSheetOpen);
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

            // Stop any ongoing animation
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            if (isVisible)
            {
                // Make it visible first
                _characterSheetPanel.style.display = DisplayStyle.Flex;
                _characterSheetPanel.SetEnabled(true);
                _characterSheetPanel.pickingMode = PickingMode.Position;
                
                // Ensure it starts off-screen
                _characterSheetPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                _characterSheetPanel.MarkDirtyRepaint();
                
                // Start animation immediately
                _currentAnimation = StartCoroutine(AnimateSlideInCoroutine(_characterSheetPanel, PANEL_OFFSCREEN_RIGHT, PANEL_ONSCREEN_RIGHT));
                
                EnablePickingOnAllButtons(_characterSheetPanel);
                
                // Ensure character sheet fits on screen
                ClampCharacterSheetToScreen();

                // Also show game log panel
                if (_gameLogPanel != null)
                {
                    _gameLogPanel.style.display = DisplayStyle.Flex;
                    _gameLogPanel.SetEnabled(true);
                    _gameLogPanel.pickingMode = PickingMode.Position;
                    _gameLogPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                    
                    // Ensure game log height fits on screen
                    float currentHeight = _gameLogPanel.resolvedStyle.height;
                    float clampedHeight = ClampGameLogHeightToScreen(currentHeight);
                    if (clampedHeight != currentHeight)
                    {
                        _gameLogPanel.style.height = clampedHeight;
                    }
                    
                    // Re-enable picking on buttons after showing
                    EnablePickingOnAllButtons(_gameLogPanel);
                    
                    _gameLogPanel.MarkDirtyRepaint();
                    StartCoroutine(AnimateSlideInCoroutine(_gameLogPanel, PANEL_OFFSCREEN_RIGHT, PANEL_ONSCREEN_RIGHT));
                }
            }
            else
            {
                // Animate slide out to right
                float currentRight = _characterSheetPanel.resolvedStyle.right;
                _currentAnimation = StartCoroutine(AnimateSlideOutCoroutine(_characterSheetPanel, currentRight, PANEL_OFFSCREEN_RIGHT, () =>
                {
                    _characterSheetPanel.style.display = DisplayStyle.None;
                    _characterSheetPanel.SetEnabled(false);
                    _characterSheetPanel.pickingMode = PickingMode.Ignore;
                    _currentAnimation = null;
                }));

                // Also hide game log panel
                if (_gameLogPanel != null)
                {
                    float gameLogCurrentRight = _gameLogPanel.resolvedStyle.right;
                    StartCoroutine(AnimateSlideOutCoroutine(_gameLogPanel, gameLogCurrentRight, PANEL_OFFSCREEN_RIGHT, () =>
                    {
                        _gameLogPanel.style.display = DisplayStyle.None;
                        _gameLogPanel.SetEnabled(false);
                        _gameLogPanel.pickingMode = PickingMode.Ignore;
                    }));
                }
            }
        }

        public void SetCharacterSheetTab(int tabIndex)
        {
            if (_characterSheetTabs == null || _tabButtons == null)
                return;

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
            _currentAnimation = null;
        }

        /// <summary>
        /// Coroutine to animate the panel sliding out.
        /// </summary>
        private System.Collections.IEnumerator AnimateSlideOutCoroutine(VisualElement element, float startRight, float endRight, System.Action onComplete)
        {
            float elapsed = 0f;

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
            _currentAnimation = null;
            onComplete?.Invoke();
        }
        #endregion

        #region Button Wiring
        /// <summary>
        /// Wires up an ability score button click event.
        /// </summary>
        private void WireAbilityButton(string buttonName, string abilityName)
        {
            var button = _root.Q<Button>(buttonName);
            if (button != null)
            {
                button.pickingMode = PickingMode.Position;
                button.clicked += () => AbilityScoreClicked?.Invoke(abilityName);
            }
        }

        /// <summary>
        /// Wires up a skill button click event.
        /// </summary>
        private void WireSkillButton(string buttonName, string skillName)
        {
            var button = _root.Q<Button>(buttonName);
            if (button != null)
            {
                button.pickingMode = PickingMode.Position;
                button.clicked += () => SkillClicked?.Invoke(skillName);
            }
        }

        /// <summary>
        /// Wires up an action button click event.
        /// </summary>
        private void WireActionButton(string buttonName, string actionName)
        {
            var button = _root.Q<Button>(buttonName);
            if (button != null)
            {
                button.pickingMode = PickingMode.Position;
                button.clicked += () => ActionClicked?.Invoke(actionName);
            }
        }

        /// <summary>
        /// Wires up an attack button click event.
        /// </summary>
        private void WireAttackButton(string buttonName, string attackName)
        {
            var button = _root.Q<Button>(buttonName);
            if (button != null)
            {
                button.pickingMode = PickingMode.Position;
                button.clicked += () => AttackClicked?.Invoke(attackName);
            }
        }

        /// <summary>
        /// Wires up a feature button click event.
        /// </summary>
        private void WireFeatureButton(string buttonName, string featureName)
        {
            var button = _root.Q<Button>(buttonName);
            if (button != null)
            {
                button.pickingMode = PickingMode.Position;
                button.clicked += () => FeatureClicked?.Invoke(featureName);
            }
        }

        /// <summary>
        /// Wires up a rest button click event.
        /// </summary>
        private void WireRestButton(string buttonName, string restType)
        {
            var button = _root.Q<Button>(buttonName);
            if (button != null)
            {
                button.pickingMode = PickingMode.Position;
                button.clicked += () => RestClicked?.Invoke(restType);
            }
        }
        #endregion

        #region Game Log Methods

        /// <summary>
        /// Adds a new entry to the game log using structured data.
        /// </summary>
        /// <param name="entry">The formatted log entry data.</param>
        public void AddLogEntry(FormattedLogEntry entry)
        {
            if (_gameLogPanel == null)
            {
                Debug.LogWarning("InGameUIView: Game log panel is null!");
                return;
            }

            var logEntries = _root.Q<VisualElement>("game-log-entries");
            if (logEntries == null)
            {
                Debug.LogWarning("InGameUIView: Game log entries container is null!");
                return;
            }

            // Create card container
            var card = new VisualElement();
            card.AddToClassList("game-log-card");
            card.AddToClassList(entry.CssClass);
            card.pickingMode = PickingMode.Ignore;

            // Main content area
            var mainContent = new VisualElement();
            mainContent.AddToClassList("game-log-main-content");

            // Character name
            if (!string.IsNullOrEmpty(entry.CharacterName))
            {
                var characterNameLabel = new Label(entry.CharacterName);
                characterNameLabel.AddToClassList("game-log-character-name");
                mainContent.Add(characterNameLabel);
            }

            // Action type and sub-action on one line
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

            // Formula on its own line (if available)
            if (!string.IsNullOrEmpty(entry.DiceFormula))
            {
                var formulaLabel = new Label(entry.DiceFormula);
                formulaLabel.AddToClassList("game-log-dice-formula");
                mainContent.Add(formulaLabel);
            }

            // Dice breakdown on its own line (if available)
            if (!string.IsNullOrEmpty(entry.DiceBreakdown))
            {
                var diceBreakdownLabel = new Label(entry.DiceBreakdown);
                diceBreakdownLabel.AddToClassList("game-log-dice-breakdown");
                mainContent.Add(diceBreakdownLabel);
            }

            // Result (large number) on its own line
            if (entry.Result.HasValue)
            {
                var resultLabel = new Label(entry.Result.Value.ToString());
                resultLabel.AddToClassList("game-log-result");
                mainContent.Add(resultLabel);
            }

            // Timestamp
            var timestamp = System.DateTime.Now.ToString("h:mm tt");
            var timestampLabel = new Label(timestamp);
            timestampLabel.AddToClassList("game-log-timestamp");
            mainContent.Add(timestampLabel);

            card.Add(mainContent);
            logEntries.Add(card);

            // Auto-scroll to bottom to show new entry
            var scrollView = _root.Q<ScrollView>("game-log-content");
            if (scrollView != null)
            {
                scrollView.schedule.Execute(() =>
                {
                    float maxScroll = scrollView.contentContainer.layout.height - scrollView.contentViewport.layout.height;
                    if (maxScroll > 0)
                    {
                        scrollView.scrollOffset = new Vector2(0, maxScroll);
                    }
                    else
                    {
                        scrollView.scrollOffset = new Vector2(0, 0);
                    }
                }).ExecuteLater(1);
            }

            // Limit log entries to prevent performance issues (keep last 100 entries)
            const int maxEntries = 100;
            while (logEntries.childCount > maxEntries)
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

        #endregion
    }
}

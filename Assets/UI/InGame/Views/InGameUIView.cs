using GameCore.UI;
using GameCore.UI.InGame.Services;
using GameCore.UI.InGame.Models;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
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
    [RequireComponent(typeof(PanelRenderer))]
    public class InGameUIView : MonoBehaviour, IUIView<InGameUIState>
    {
        #region Constants
        private const int TOTAL_TABS = 7;
        private const float PANEL_OFFSCREEN_RIGHT = -568f;
        private const float PANEL_ONSCREEN_RIGHT = 48f;
        private const float VISIBILITY_DISTANCE_THRESHOLD = 100f; // Distance from on-screen position to consider visible
        private const float INSTANT_CLOSE_DISTANCE_THRESHOLD = 50f; // Distance from off-screen to skip animation
        private const float MIN_VISIBILITY_CHANGE_INTERVAL = 0.1f; // Minimum seconds between visibility changes
        private const float SCREEN_EDGE_BUFFER = 5f; // Buffer from screen edges
        #endregion

        #region Serialized Fields
        [Header("Assets")]
        [Tooltip("USS stylesheet for this view. Drag InGameUI.uss here.")]
        [SerializeField] private StyleSheet _inGameStyleSheet;
        #endregion

        #region Private Fields

        private PanelRenderer _panelRenderer;
        private bool _panelReloadRegistered;
        private Coroutine _deferredBindCoroutine;
        private bool _visualTreeBound;
        private VisualElement _root;
        private VisualElement _characterSheetPanel;
        private ScrollView _characterSheetScrollView;
        private VisualElement[] _characterSheetTabs;
        private Button[] _tabButtons;
        private System.Action[] _tabButtonHandlers;
        
        // Component references (SOLID - Single Responsibility)
        private UIAnimationController _animationController;
        private GameLogView _gameLogView;
        private TabCarouselView _tabCarouselView;
        private ButtonNavigationView _buttonNavigationView;
        
        // Animation state
        private bool? _targetVisibilityState = null; // Track what state we're animating to (null = not animating)
        private float _lastVisibilityChangeTime = 0f; // Track when we last changed visibility to prevent rapid toggling

        private Button _moveButton;
        private System.Action _moveButtonClickedHandler;
        private DmPanelView _dmPanelView = new DmPanelView();
        private DmCharacterInspectorView _dmInspectorView = new DmCharacterInspectorView();
        private VisualElement _controlsCheatsheetPanel;
        private bool _playerHudVisible = true;
        #endregion

        #region Public Properties

        public VisualElement Root => _root;

        /// <summary>DM-only player list and HP controls.</summary>
        public DmPanelView DmPanel => _dmPanelView;

        /// <summary>DM-only character inspector for editing player combat state.</summary>
        public DmCharacterInspectorView DmInspector => _dmInspectorView;

        /// <summary>
        /// Shows or hides player-only HUD (controls cheatsheet, character sheet, game log).
        /// DM clients use the dm-panel and dm-inspector instead.
        /// </summary>
        public void SetPlayerHudVisible(bool visible)
        {
            _playerHudVisible = visible;
            SetControlsCheatsheetVisible(visible);

            if (!visible)
            {
                SetCharacterSheetVisible(false);
                _gameLogView?.SetVisible(false, PANEL_OFFSCREEN_RIGHT, PANEL_ONSCREEN_RIGHT);
            }
        }

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

            if (_root == null || _characterSheetPanel == null)
                return false;

            // Get mouse position in screen coordinates (needed for Panel.Pick)
            Vector2? screenPosition = GetMouseScreenPosition();
            if (!screenPosition.HasValue)
                return false;

            // Get the panel
            var panel = _root.panel;
            if (panel == null)
                return false;

            // Method 1: Use Panel.Pick to check if mouse is over the character sheet
            // Panel.Pick uses screen coordinates directly
            if (IsMouseOverPanelUsingPick(panel, screenPosition.Value))
                return true;

            // Method 2: Check if mouse position is within the character sheet panel's world bounds
            // UI Toolkit worldBound is in panel space (top-left origin)
            return IsMouseOverPanelUsingBounds(panelSpacePos: GetMousePositionInPanelSpace(screenPosition.Value));
        }

        /// <summary>
        /// Gets the mouse position in panel space coordinates.
        /// </summary>
        private Vector2? GetMousePositionInPanelSpace(Vector2 screenPosition)
        {
            // Convert screen coordinates to panel space
            // Panel space uses top-left origin, screen uses bottom-left
            float screenHeight = Screen.height;
            return new Vector2(
                screenPosition.x,
                screenHeight - screenPosition.y
            );
        }

        /// <summary>
        /// Gets the current mouse position in screen coordinates.
        /// </summary>
        private Vector2? GetMouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null)
                return null;
            return mouse.position.ReadValue();
#else
            return Input.mousePosition;
#endif
        }

        /// <summary>
        /// Checks if mouse is over the character sheet panel using Panel.Pick method.
        /// </summary>
        private bool IsMouseOverPanelUsingPick(IPanel panel, Vector2 screenPosition)
        {
            var pickedElement = panel.Pick(screenPosition);
            if (pickedElement == null)
                return false;

            // Check if the picked element is within the character sheet panel hierarchy
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

            return false;
        }

        /// <summary>
        /// Checks if mouse is over the character sheet panel using bounds checking.
        /// </summary>
        private bool IsMouseOverPanelUsingBounds(Vector2? panelSpacePos)
        {
            if (!panelSpacePos.HasValue || _characterSheetPanel == null)
                return false;

            Rect panelRect = _characterSheetPanel.worldBound;
            
            if (panelRect.Contains(panelSpacePos.Value))
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

        /// <summary>
        /// Fired when the Move button is clicked.
        /// </summary>
        public event System.Action MoveButtonClicked;

        /// <summary>Fired after the UXML visual tree is bound (including deferred PanelRenderer loads).</summary>
        public event System.Action VisualTreeBound;
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            if (_panelRenderer == null)
            {
                _panelRenderer = GetComponent<PanelRenderer>();
            }

            EnsurePanelReloadSubscription();
            if (_panelRenderer != null)
            {
                ((IPanelComponent)_panelRenderer).PerformUpdate();
            }

            TrySyncRootFromPanel();
            TryBindVisualTree();
            ScheduleDeferredBindIfNeeded();
            Show();
        }

        private void OnDisable()
        {
            if (_deferredBindCoroutine != null)
            {
                StopCoroutine(_deferredBindCoroutine);
                _deferredBindCoroutine = null;
            }

            ReleasePanelReloadSubscription();

            TeardownBoundVisualTree();
            _root = null;
            Hide();
        }

        private void OnPanelUiReload(PanelRenderer _, VisualElement root)
        {
            // PanelRenderer replaces the visual tree; cached elements become invalid but
            // TryBindVisualTree() no-ops while _visualTreeBound is true — tear down first.
            TeardownBoundVisualTree();
            _root = root;
            TryBindVisualTree();
            if (isActiveAndEnabled)
            {
                Show();
                ScheduleDeferredBindIfNeeded();
            }
        }

        private void EnsurePanelReloadSubscription()
        {
            if (_panelRenderer == null || _panelReloadRegistered)
            {
                return;
            }

            _panelRenderer.RegisterUIReloadCallback(OnPanelUiReload);
            _panelReloadRegistered = true;
        }

        private void ReleasePanelReloadSubscription()
        {
            if (_panelRenderer == null || !_panelReloadRegistered)
            {
                return;
            }

            _panelRenderer.UnregisterUIReloadCallback(OnPanelUiReload);
            _panelReloadRegistered = false;
        }

        /// <summary>
        /// Clears Toolkit subscriptions and element caches so <see cref="TryBindVisualTree"/> can run again.
        /// Uses try/catch around unsubscriptions because elements may already be disposed after a panel reload.
        /// </summary>
        private void TeardownBoundVisualTree()
        {
            DetachMoveButton();

            if (_tabButtons != null && _tabButtonHandlers != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    if (_tabButtons[i] != null && _tabButtonHandlers[i] != null)
                    {
                        try
                        {
                            _tabButtons[i].clicked -= _tabButtonHandlers[i];
                        }
                        catch
                        {
                            // Disposed or invalid after PanelRenderer tree swap.
                        }
                    }
                }
            }

            try
            {
                _tabCarouselView?.Cleanup();
            }
            catch
            {
            }

            _tabCarouselView = null;
            _buttonNavigationView = null;
            _characterSheetPanel = null;
            _characterSheetScrollView = null;
            _characterSheetTabs = null;
            _tabButtons = null;
            _tabButtonHandlers = null;
            _controlsCheatsheetPanel = null;
            _targetVisibilityState = null;
            _visualTreeBound = false;
        }

        private void DetachMoveButton()
        {
            if (_moveButton != null && _moveButtonClickedHandler != null)
            {
                try
                {
                    _moveButton.clicked -= _moveButtonClickedHandler;
                }
                catch
                {
                }
            }

            _moveButton = null;
            _moveButtonClickedHandler = null;
        }

        private static bool IsElementUnderRoot(VisualElement element, VisualElement root)
        {
            if (element == null || root == null)
                return false;

            try
            {
                for (VisualElement p = element; p != null; p = p.hierarchy.parent)
                {
                    if (p == root)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ensures cached elements match the current <see cref="_root"/>; rebinds after a tree reload or missed callback.
        /// </summary>
        private bool EnsureVisualTreeReadyForUpdate()
        {
            TrySyncRootFromPanel();
            if (_root == null && _panelRenderer != null)
            {
                _root = PanelRendererUtility.TryGetRootVisualElement(_panelRenderer);
            }

            bool treeSwapped = _visualTreeBound
                && _root != null
                && _characterSheetPanel != null
                && !IsElementUnderRoot(_characterSheetPanel, _root);

            if (treeSwapped)
            {
                TeardownBoundVisualTree();
                _root = _panelRenderer != null
                    ? PanelRendererUtility.TryGetRootVisualElement(_panelRenderer)
                    : null;
            }

            if (!_visualTreeBound && _root != null)
            {
                TryBindVisualTree();
            }

            if (!_visualTreeBound)
            {
                ScheduleDeferredBindIfNeeded();
                return false;
            }

            return _characterSheetPanel != null;
        }

        private void TrySyncRootFromPanel()
        {
            if (_root != null || _panelRenderer == null)
            {
                return;
            }

            _root = PanelRendererUtility.TryGetRootVisualElement(_panelRenderer);
        }

        private void ScheduleDeferredBindIfNeeded()
        {
            if (_visualTreeBound || !isActiveAndEnabled)
            {
                return;
            }

            if (_deferredBindCoroutine != null)
            {
                StopCoroutine(_deferredBindCoroutine);
            }

            _deferredBindCoroutine = StartCoroutine(CoDeferredBindPanelTree());
        }

        private IEnumerator CoDeferredBindPanelTree()
        {
            try
            {
                for (int i = 0; i < 24; i++)
                {
                    if (_visualTreeBound)
                    {
                        yield break;
                    }

                    TrySyncRootFromPanel();
                    TryBindVisualTree();
                    if (_visualTreeBound)
                    {
                        yield break;
                    }

                    yield return null;
                }
            }
            finally
            {
                _deferredBindCoroutine = null;
            }
        }

        private void TryBindVisualTree()
        {
            if (_visualTreeBound || _root == null)
            {
                return;
            }

            _visualTreeBound = true;

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

            _dmPanelView.Initialize(_root);
            _dmInspectorView.Initialize(_root);

            _controlsCheatsheetPanel = _root.Q<VisualElement>("controls-cheatsheet-panel");
            if (!_playerHudVisible)
            {
                SetControlsCheatsheetVisible(false);
            }

            _characterSheetPanel = _root.Q<VisualElement>("character-sheet-panel");

            // Ensure character sheet panel can receive pointer events
            // Only apply runtime positioning/hiding when in play mode (not in editor preview)
            if (_characterSheetPanel != null)
            {
                _characterSheetPanel.pickingMode = PickingMode.Position;
                
                // Only hide and position off-screen during play mode
                // In editor/preview, panels remain visible with default positioning from USS
                if (Application.isPlaying)
                {
                    _characterSheetPanel.style.display = DisplayStyle.None;
                    _characterSheetPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                    _characterSheetPanel.SetEnabled(false);
                    _characterSheetPanel.AddToClassList("runtime-hidden");
                }
            }

            // Get the ScrollView for tab content
            _characterSheetScrollView = _root.Q<ScrollView>("charsheet-tab-content");

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
                        if (_tabCarouselView == null || !_tabCarouselView.IsDragging)
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

            // Game log view handles its own clear button wiring

            // Wire up Move button
            DetachMoveButton();
            _moveButton = _root.Q<Button>("move-button");
            if (_moveButton != null)
            {
                _moveButton.pickingMode = PickingMode.Position;
                _moveButtonClickedHandler = () => MoveButtonClicked?.Invoke();
                _moveButton.clicked += _moveButtonClickedHandler;
            }

            // Initialize component references (SOLID - Single Responsibility)
            // Must be called after tabs and buttons are initialized
            InitializeComponents();

            // Initialize tab carousel view
            _tabCarouselView = new TabCarouselView();
            _tabCarouselView.Initialize(_root, _tabButtons);
            _tabCarouselView.TabClicked += (tabIndex) => 
            {
                // Only trigger tab click if we're not dragging
                if (!_tabCarouselView.IsDragging)
                {
                    TabClicked?.Invoke(tabIndex);
                }
            };

            // Ensure all buttons in the character sheet can receive pointer events
            EnablePickingOnAllButtons(_root);

            // Start with character sheet hidden by default and positioned off-screen (only in play mode)
            if (_characterSheetPanel != null && Application.isPlaying)
            {
                _characterSheetPanel.style.display = DisplayStyle.None;
                _characterSheetPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                _characterSheetPanel.SetEnabled(false);
                _characterSheetPanel.pickingMode = PickingMode.Ignore;
                _characterSheetPanel.AddToClassList("runtime-hidden");
            }

            VisualTreeBound?.Invoke();
        }

        /// <summary>
        /// Initializes component references following SOLID principles.
        /// </summary>
        private void InitializeComponents()
        {
            // Initialize animation controller
                _animationController = GetComponent<UIAnimationController>();
                if (_animationController == null)
                {
                    _animationController = gameObject.AddComponent<UIAnimationController>();
                }

            // Initialize game log view
            _gameLogView = GetComponent<GameLogView>();
            if (_gameLogView == null)
            {
                _gameLogView = gameObject.AddComponent<GameLogView>();
            }
            _gameLogView.Initialize(_root, _animationController);
            _gameLogView.LogEntryDeleteClicked += (entryCard) => LogEntryDeleteClicked?.Invoke(entryCard);
            _gameLogView.ClearLogClicked += () => ClearLogClicked?.Invoke();

            // Initialize button navigation view
            _buttonNavigationView = new ButtonNavigationView();
            _buttonNavigationView.Initialize(_root, _characterSheetTabs, _characterSheetScrollView, _tabButtons);
        }
        #endregion

        public void Initialize()
        {
            if (_panelRenderer == null)
            {
                _panelRenderer = GetComponent<PanelRenderer>();
            }

            if (_panelRenderer == null)
            {
                Debug.LogError("InGameUIView: PanelRenderer is missing.");
                return;
            }

            EnsurePanelReloadSubscription();
            ((IPanelComponent)_panelRenderer).PerformUpdate();
            TrySyncRootFromPanel();
            TryBindVisualTree();
            ScheduleDeferredBindIfNeeded();
        }

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
            if (!EnsureVisualTreeReadyForUpdate())
            {
                return;
            }

            if (!_playerHudVisible)
                return;

            // If we're already animating to the target state, don't re-trigger
            // This prevents closing during opening animation
            if (_targetVisibilityState.HasValue && _targetVisibilityState.Value == state.IsCharacterSheetOpen)
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
                if (_targetVisibilityState == true)
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
                bool shouldUpdate = !(state.IsCharacterSheetOpen == false && _targetVisibilityState == true);
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
            if (_animationController != null)
            {
                _animationController.StopCurrentAnimation();
                _targetVisibilityState = null;
                currentRight = GetPanelRightPosition(_characterSheetPanel);
                if (float.IsNaN(currentRight))
                {
                    currentRight = isVisible ? PANEL_OFFSCREEN_RIGHT : PANEL_ONSCREEN_RIGHT;
                }
            }

            if (isVisible)
            {
                _targetVisibilityState = true;
                
                // Remove runtime-hidden class and show panel
                _characterSheetPanel.RemoveFromClassList("runtime-hidden");
                _characterSheetPanel.style.display = DisplayStyle.Flex;
                _characterSheetPanel.SetEnabled(true);
                _characterSheetPanel.pickingMode = PickingMode.Position;
                _characterSheetPanel.style.right = currentRight;
                _characterSheetPanel.MarkDirtyRepaint();
                
                EnablePickingOnAllButtons(_characterSheetPanel);
                ClampCharacterSheetToScreen();

                // Show game log panel using GameLogView
                _gameLogView?.SetVisible(true, PANEL_OFFSCREEN_RIGHT, PANEL_ONSCREEN_RIGHT);
                
                // Animate character sheet slide in
                if (_animationController != null)
                {
                    _animationController.AnimateSlideIn(_characterSheetPanel, currentRight, PANEL_ONSCREEN_RIGHT, () =>
                    {
                        _targetVisibilityState = null;
                    });
                }
            }
            else
            {
                _targetVisibilityState = false;
                
                // Clear keyboard selection when closing
                if (_characterSheetTabs != null)
                {
                    for (int i = 0; i < _characterSheetTabs.Length; i++)
                    {
                        ClearButtonSelection(i);
                    }
                }
                
                // Hide game log panel
                _gameLogView?.SetVisible(false, PANEL_OFFSCREEN_RIGHT, PANEL_ONSCREEN_RIGHT);
                
                float distanceToOffScreen = Mathf.Abs(currentRight - PANEL_OFFSCREEN_RIGHT);
                bool shouldAnimate = distanceToOffScreen > INSTANT_CLOSE_DISTANCE_THRESHOLD;
                
                if (shouldAnimate && _animationController != null)
                {
                    _animationController.AnimateSlideOut(_characterSheetPanel, currentRight, PANEL_OFFSCREEN_RIGHT, () =>
                    {
                        _characterSheetPanel.AddToClassList("runtime-hidden");
                        _characterSheetPanel.style.display = DisplayStyle.None;
                        _characterSheetPanel.SetEnabled(false);
                        _characterSheetPanel.pickingMode = PickingMode.Ignore;
                        _targetVisibilityState = null;
                    });
                }
                else
                {
                    _characterSheetPanel.AddToClassList("runtime-hidden");
                    _characterSheetPanel.style.right = PANEL_OFFSCREEN_RIGHT;
                    _characterSheetPanel.style.display = DisplayStyle.None;
                    _characterSheetPanel.SetEnabled(false);
                    _characterSheetPanel.pickingMode = PickingMode.Ignore;
                    _targetVisibilityState = null;
                }
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
                    
                    // Use class-based approach for better editor preview support
                    if (isActive)
                    {
                        _characterSheetTabs[i].AddToClassList("active");
                        _characterSheetTabs[i].style.display = DisplayStyle.Flex;
                        _characterSheetTabs[i].pickingMode = PickingMode.Position;
                    }
                    else
                    {
                        _characterSheetTabs[i].RemoveFromClassList("active");
                        _characterSheetTabs[i].style.display = DisplayStyle.None;
                        _characterSheetTabs[i].pickingMode = PickingMode.Ignore;
                    }
                    
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
            _tabCarouselView?.EnsureTabVisible(tabIndex);
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
        /// <summary>
        /// Adds a new entry to the game log. Delegates to GameLogView component.
        /// </summary>
        public void AddLogEntry(FormattedLogEntry entry)
        {
            _gameLogView?.AddLogEntry(entry);
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
        /// Clears all entries from the game log. Delegates to GameLogView component.
        /// </summary>
        public void ClearLog()
        {
            _gameLogView?.ClearLog();
        }

        /// <summary>
        /// Removes a specific log entry from the game log. Delegates to GameLogView component.
        /// </summary>
        public void RemoveLogEntry(VisualElement entryCard)
        {
            _gameLogView?.RemoveLogEntry(entryCard);
        }

        #endregion

        #region Button Navigation

        /// <summary>
        /// Gets all focusable buttons in the specified tab content. Delegates to ButtonNavigationView component.
        /// </summary>
        public List<Button> GetButtonsInTab(int tabIndex)
        {
            return _buttonNavigationView?.GetButtonsInTab(tabIndex) ?? new List<Button>();
        }

        /// <summary>
        /// Gets the index of the button currently under the mouse cursor. Delegates to ButtonNavigationView component.
        /// Returns -1 if no button is hovered.
        /// </summary>
        public int GetHoveredButtonIndex(int tabIndex)
        {
            if (!IsCharacterSheetOpen())
                return -1;
            return _buttonNavigationView?.GetHoveredButtonIndex(tabIndex) ?? -1;
        }

        /// <summary>
        /// Sets the selected button index and updates visual feedback. Delegates to ButtonNavigationView component.
        /// </summary>
        public void SetSelectedButtonIndex(int tabIndex, int buttonIndex)
        {
            _buttonNavigationView?.SetSelectedButtonIndex(tabIndex, buttonIndex);
        }

        /// <summary>
        /// Gets the currently selected button and triggers its click event. Delegates to ButtonNavigationView component.
        /// </summary>
        public bool ActivateSelectedButton(int tabIndex, int buttonIndex)
        {
            return _buttonNavigationView?.ActivateSelectedButton(tabIndex, buttonIndex) ?? false;
        }

        /// <summary>
        /// Clears the keyboard selection highlight from all buttons in the specified tab. Delegates to ButtonNavigationView component.
        /// </summary>
        public void ClearButtonSelection(int tabIndex)
        {
            _buttonNavigationView?.ClearButtonSelection(tabIndex);
        }

        /// <summary>
        /// Updates the speed display to show remaining movement.
        /// </summary>
        /// <param name="remainingFeet">Remaining movement in feet</param>
        /// <param name="maxFeet">Maximum movement in feet</param>
        public void UpdateSpeedDisplay(int remainingFeet, int maxFeet)
        {
            var speedLabel = _root?.Q<Label>("speed-value");
            if (speedLabel != null)
            {
                speedLabel.text = $"{remainingFeet} / {maxFeet} ft";
                
                // Change color if movement is exhausted
                if (remainingFeet <= 0)
                {
                    speedLabel.style.color = new StyleColor(new Color(0.8f, 0.2f, 0.2f)); // Red when exhausted
                }
                else
                {
                    speedLabel.style.color = StyleKeyword.Null; // Reset to default
                }
            }
        }

        /// <summary>
        /// Updates the movement button state (Move/Cancel) and adds visual indicator.
        /// </summary>
        /// <param name="isMovementModeActive">Whether movement mode is currently active</param>
        public void UpdateMovementButtonState(bool isMovementModeActive)
        {
            var moveButton = _root?.Q<Button>("move-button");
            if (moveButton != null)
            {
                if (isMovementModeActive)
                {
                    moveButton.text = "Cancel";
                    moveButton.AddToClassList("move-button-active");
                }
                else
                {
                    moveButton.text = "Move";
                    moveButton.RemoveFromClassList("move-button-active");
                }
            }

            // Add visual indicator to speed container when in movement mode
            var speedContainer = _root?.Q<VisualElement>("speed-container");
            if (speedContainer != null)
            {
                if (isMovementModeActive)
                {
                    speedContainer.AddToClassList("movement-mode-active");
                }
                else
                {
                    speedContainer.RemoveFromClassList("movement-mode-active");
                }
            }
        }

        /// <summary>
        /// Shows or hides the encounter turn banner at the top of the HUD.
        /// </summary>
        public void UpdateEncounterTurnIndicator(string text, bool visible)
        {
            var label = _root?.Q<Label>("encounter-turn-label");
            if (label == null)
                return;

            label.text = text ?? string.Empty;
            label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion

        #region Screen Bounds Helpers

        private void SetControlsCheatsheetVisible(bool visible)
        {
            if (_controlsCheatsheetPanel == null)
                return;

            _controlsCheatsheetPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _controlsCheatsheetPanel.SetEnabled(visible);
            _controlsCheatsheetPanel.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
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
        /// Scrolls the character sheet tab content up or down. Delegates to ButtonNavigationView component.
        /// </summary>
        /// <param name="scrollAmount">Positive value scrolls down, negative scrolls up.</param>
        public void ScrollTabContent(float scrollAmount)
        {
            _buttonNavigationView?.ScrollTabContent(scrollAmount);
        }

        #endregion
    }
}

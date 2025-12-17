using GameCore.UI;
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
        [Header("Assets")]
        [Tooltip("USS stylesheet for this view. Drag InGameUI.uss here.")]
        [SerializeField] private StyleSheet _inGameStyleSheet;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _characterSheetPanel;
        private VisualElement[] _characterSheetTabs;
        private Button[] _tabButtons;
        private System.Action[] _tabButtonHandlers;
        private VisualElement _tabsContainer;
        private VisualElement _tabsWrapper;
        private Button _tabNavLeft;
        private Button _tabNavRight;
        private int _visibleTabCount = 4; // Number of tabs visible at once
        private int _currentTabOffset = 0; // Current scroll offset
        
        // Drag functionality
        private bool _isDragging = false;
        private float _dragStartX = 0f;
        private float _dragStartOffset = 0f;
        private float _dragThreshold = 10f; // Minimum drag distance to trigger
        
        // Animation state
        private Coroutine _currentAnimation = null;

        public VisualElement Root => _root;

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
                _characterSheetPanel.style.right = -568f; // -520 (width) - 48 (offset)
                _characterSheetPanel.SetEnabled(false);
            }

            // Find all character sheet tabs (0 = Overview, 1 = Skills, 2 = Actions, 3 = Spells, 4 = Inventory, 5 = Features, 6 = Rest)
            _characterSheetTabs = new VisualElement[]
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
            _tabButtons = new Button[]
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
                _characterSheetPanel.style.right = -520f - 48f; // Position off-screen to the right
                _characterSheetPanel.SetEnabled(false);
                _characterSheetPanel.pickingMode = PickingMode.Ignore;
            }
        }

        /// <summary>
        /// Recursively enables picking mode on all interactive elements.
        /// </summary>
        private void EnablePickingOnAllButtons(VisualElement element)
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
                _root.pickingMode = PickingMode.Position; // Enable pointer events
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
                _characterSheetPanel.style.right = -568f;
                _characterSheetPanel.MarkDirtyRepaint();
                
                // Start animation immediately
                _currentAnimation = StartCoroutine(AnimateSlideInCoroutine(_characterSheetPanel, -568f, 48f));
                
                EnablePickingOnAllButtons(_characterSheetPanel);
            }
            else
            {
                // Animate slide out to right
                float currentRight = _characterSheetPanel.resolvedStyle.right;
                _currentAnimation = StartCoroutine(AnimateSlideOutCoroutine(_characterSheetPanel, currentRight, -568f, () =>
                {
                    _characterSheetPanel.style.display = DisplayStyle.None;
                    _characterSheetPanel.SetEnabled(false);
                    _characterSheetPanel.pickingMode = PickingMode.Ignore;
                    _currentAnimation = null;
                }));
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
            else if (tabIndex >= _currentTabOffset + _visibleTabCount)
            {
                // Tab is to the right of visible area, scroll right
                _currentTabOffset = Mathf.Max(0, tabIndex - _visibleTabCount + 1);
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
            if (totalTabs <= _visibleTabCount)
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
                    
                    if (_currentTabOffset + _visibleTabCount <= totalTabs)
                    {
                        // Normal case - no wrap needed
                        isVisible = i >= _currentTabOffset && i < _currentTabOffset + _visibleTabCount;
                    }
                    else
                    {
                        // Wrap around case
                        int overflow = (_currentTabOffset + _visibleTabCount) - totalTabs;
                        isVisible = (i >= _currentTabOffset) || (i < overflow);
                    }
                    
                    _tabButtons[i].style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// Handles left navigation button click.
        /// </summary>
        private void OnTabNavLeftClicked()
        {
            if (_tabButtons == null || _tabButtons.Length == 0)
                return;
                
            _currentTabOffset--;
            
            // Wrap around
            int maxOffset = Mathf.Max(0, _tabButtons.Length - _visibleTabCount);
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
            int maxOffset = Mathf.Max(0, _tabButtons.Length - _visibleTabCount);
            if (_currentTabOffset > maxOffset)
            {
                _currentTabOffset = 0;
            }
            
            UpdateTabCarousel();
        }

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
            if (dragDistance > _dragThreshold)
            {
                // Calculate how many tabs to scroll based on drag distance
                // Assuming roughly 100px per tab
                float tabWidth = 100f;
                int tabsToScroll = Mathf.RoundToInt(deltaX / tabWidth);

                int newOffset = Mathf.RoundToInt(_dragStartOffset - tabsToScroll);
                newOffset = Mathf.Clamp(newOffset, 0, Mathf.Max(0, _tabButtons.Length - _visibleTabCount));

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
                }).ExecuteLater(50); // 50ms delay
                _tabsWrapper.ReleasePointer(evt.pointerId);
            }
        }

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
            float duration = 0.3f; // 300ms
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
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
            float duration = 0.3f; // 300ms
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
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
    }
}


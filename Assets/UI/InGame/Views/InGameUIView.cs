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
        private Button _characterSheetButton;
        private VisualElement[] _characterSheetTabs;
        private Button[] _tabButtons;
        private System.Action[] _tabButtonHandlers;

        public VisualElement Root => _root;

        /// <summary>
        /// Fired when the player requests to toggle the character sheet
        /// via the on-screen button.
        /// </summary>
        public event System.Action CharacterSheetToggleRequested;

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
            if (_characterSheetButton != null)
            {
                _characterSheetButton.clicked -= OnCharacterSheetButtonClicked;
            }

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
            _characterSheetButton = _root.Q<Button>("character-sheet-button");

            // Ensure character sheet panel can receive pointer events
            if (_characterSheetPanel != null)
            {
                _characterSheetPanel.pickingMode = PickingMode.Position;
            }

            if (_characterSheetButton != null)
            {
                _characterSheetButton.clicked += OnCharacterSheetButtonClicked;
                _characterSheetButton.pickingMode = PickingMode.Position;
                _characterSheetButton.focusable = true;
            }
            else
            {
                Debug.LogWarning("InGameUIView: Character sheet button not found in UXML.");
            }

            // Find all character sheet tabs (0 = Overview, 1 = Skills, 2 = Actions, 3 = Spells, 4 = Inventory, 5 = Features)
            _characterSheetTabs = new VisualElement[]
            {
                _root.Q<VisualElement>("tab-overview-content"),
                _root.Q<VisualElement>("tab-skills-content"),
                _root.Q<VisualElement>("tab-actions-content"),
                _root.Q<VisualElement>("tab-spells-content"),
                _root.Q<VisualElement>("tab-inventory-content"),
                _root.Q<VisualElement>("tab-features-content")
            };

            // Wire up tab buttons
            _tabButtons = new Button[]
            {
                _root.Q<Button>("tab-overview"),
                _root.Q<Button>("tab-skills"),
                _root.Q<Button>("tab-actions"),
                _root.Q<Button>("tab-spells"),
                _root.Q<Button>("tab-inventory"),
                _root.Q<Button>("tab-features")
            };

            _tabButtonHandlers = new System.Action[_tabButtons.Length];
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].pickingMode = PickingMode.Position; // Ensure tab buttons receive pointer events
                    _tabButtonHandlers[i] = () => TabClicked?.Invoke(tabIndex);
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

            // Ensure all buttons in the character sheet can receive pointer events
            EnablePickingOnAllButtons(_root);

            // Start with character sheet hidden by default
            SetCharacterSheetVisible(false);
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
            SetCharacterSheetVisible(state.IsCharacterSheetOpen);
            SetCharacterSheetTab(state.CharacterSheetTabIndex);
        }

        public void SetCharacterSheetVisible(bool isVisible)
        {
            if (_characterSheetPanel == null)
            {
                Debug.LogWarning("InGameUIView: Character sheet panel is null!");
                return;
            }

            _characterSheetPanel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _characterSheetPanel.SetEnabled(isVisible);
            _characterSheetPanel.pickingMode = isVisible ? PickingMode.Position : PickingMode.Ignore;

            if (isVisible)
            {
                EnablePickingOnAllButtons(_characterSheetPanel);
                _characterSheetPanel.MarkDirtyRepaint();
                _root?.MarkDirtyRepaint();
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

        private void OnCharacterSheetButtonClicked()
        {
            CharacterSheetToggleRequested?.Invoke();
        }
    }
}


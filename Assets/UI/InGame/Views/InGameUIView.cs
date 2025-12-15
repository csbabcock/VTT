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
        private VisualElement[] _characterSheetPages;

        public VisualElement Root => _root;

        /// <summary>
        /// Fired when the player requests to toggle the character sheet
        /// via the on-screen button.
        /// </summary>
        public event System.Action CharacterSheetToggleRequested;

        /// <summary>
        /// Fired when the player requests to navigate to the next page.
        /// </summary>
        public event System.Action NextPageRequested;

        /// <summary>
        /// Fired when the player requests to navigate to the previous page.
        /// </summary>
        public event System.Action PreviousPageRequested;

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
                return;

            // Add style sheet if assigned
            if (_inGameStyleSheet != null && !_root.styleSheets.Contains(_inGameStyleSheet))
            {
                _root.styleSheets.Add(_inGameStyleSheet);
            }

            _characterSheetPanel = _root.Q<VisualElement>("character-sheet-panel");
            _characterSheetButton = _root.Q<Button>("character-sheet-button");

            if (_characterSheetButton != null)
            {
                _characterSheetButton.clicked += OnCharacterSheetButtonClicked;
            }

            // Find all character sheet pages (0 = character info, 1 = abilities, 2 = skills, 3 = actions)
            _characterSheetPages = new VisualElement[]
            {
                _root.Q<VisualElement>("charsheet-page-0"),
                _root.Q<VisualElement>("charsheet-page-1"),
                _root.Q<VisualElement>("charsheet-page-2"),
                _root.Q<VisualElement>("charsheet-page-3")
            };

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

            // Start with character sheet hidden by default.
            SetCharacterSheetVisible(false);
        }

        public void Show()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                _root.SetEnabled(true);
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
            SetCharacterSheetPage(state.CharacterSheetPageIndex);
        }

        public void SetCharacterSheetVisible(bool isVisible)
        {
            if (_characterSheetPanel == null)
                return;

            _characterSheetPanel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetCharacterSheetPage(int pageIndex)
        {
            if (_characterSheetPages == null)
                return;

            for (int i = 0; i < _characterSheetPages.Length; i++)
            {
                if (_characterSheetPages[i] != null)
                {
                    _characterSheetPages[i].style.display = (i == pageIndex) ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void WireAbilityButton(string buttonName, string abilityName)
        {
            var button = _root.Q<Button>(buttonName);
            if (button != null)
            {
                button.clicked += () => AbilityScoreClicked?.Invoke(abilityName);
            }
        }

        private void WireSkillButton(string buttonName, string skillName)
        {
            var button = _root.Q<Button>(buttonName);
            if (button != null)
            {
                button.clicked += () => SkillClicked?.Invoke(skillName);
            }
        }

        private void OnCharacterSheetButtonClicked()
        {
            CharacterSheetToggleRequested?.Invoke();
        }
    }
}


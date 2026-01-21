using GameCore.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// View for character creation UI.
    /// Follows MVP pattern - only handles UI display and user input, delegates logic to Presenter.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CharacterCreationView : MonoBehaviour, IUIView<CharacterCreationState>
    {
        [Header("Assets")]
        [Tooltip("Optional: USS stylesheet for this view. If not assigned, it will still work if referenced from the UXML.")]
        [SerializeField] private StyleSheet _characterCreationStyleSheet;

        private UIDocument _uiDocument;
        private VisualElement _root;

        // Option button containers
        private VisualElement _classButtonsContainer;
        private VisualElement _raceButtonsContainer;
        private VisualElement _backgroundButtonsContainer;

        // Ability score inputs
        private IntegerField[] _abilityInputs;

        // Detail panel
        private Label _detailName;
        private Label _detailType;
        private Label _detailContent;
        private VisualElement _featuresSection;

        // Stats panel
        private VisualElement _abilityScoresGrid;
        private VisualElement _characterStatsGrid;
        private VisualElement _spellcastingStatsGrid;
        private VisualElement _physicalTraitsGrid;

        // Action buttons
        private Button _cancelButton;
        private Button _createButton;
        private Button _rollButton;

        // Events - View only raises events, doesn't handle business logic
        public event System.Action<string> ClassSelected;
        public event System.Action<string> RaceSelected;
        public event System.Action<string> BackgroundSelected;
        public event System.Action<int, int> AbilityScoreChanged; // index, value
        public event System.Action RollAbilitiesClicked;
        public event System.Action CancelClicked;
        public event System.Action CreateCharacterClicked;

        public VisualElement Root => _root;

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
                Debug.LogError("CharacterCreationView: UIDocument has no rootVisualElement.");
                return;
            }

            // Add stylesheet if assigned
            if (_characterCreationStyleSheet != null && !_root.styleSheets.Contains(_characterCreationStyleSheet))
            {
                _root.styleSheets.Add(_characterCreationStyleSheet);
            }

            QueryUIElements();
            SetupEventHandlers();
            InitializeUIElements();
        }

        private void QueryUIElements()
        {
            // Option button containers
            _classButtonsContainer = _root.Q<VisualElement>("class-buttons-container");
            _raceButtonsContainer = _root.Q<VisualElement>("race-buttons-container");
            _backgroundButtonsContainer = _root.Q<VisualElement>("background-buttons-container");

            // Ability inputs
            _abilityInputs = new IntegerField[6]
            {
                _root.Q<IntegerField>("ability-str-input"),
                _root.Q<IntegerField>("ability-dex-input"),
                _root.Q<IntegerField>("ability-con-input"),
                _root.Q<IntegerField>("ability-int-input"),
                _root.Q<IntegerField>("ability-wis-input"),
                _root.Q<IntegerField>("ability-cha-input")
            };

            // Detail panel
            _detailName = _root.Q<Label>("detail-name");
            _detailType = _root.Q<Label>("detail-type");
            _detailContent = _root.Q<Label>("detail-content");
            _featuresSection = _root.Q<VisualElement>("features-section");

            // Stats panel
            _abilityScoresGrid = _root.Q<VisualElement>("ability-scores-grid");
            _characterStatsGrid = _root.Q<VisualElement>("character-stats-grid");
            _spellcastingStatsGrid = _root.Q<VisualElement>("spellcasting-stats-grid");
            _physicalTraitsGrid = _root.Q<VisualElement>("physical-traits-grid");

            // Action buttons
            _cancelButton = _root.Q<Button>("cancel-button");
            _createButton = _root.Q<Button>("create-button");
            _rollButton = _root.Q<Button>("roll-abilities-button");
        }

        private void SetupEventHandlers()
        {
            // Ability score inputs
            for (int i = 0; i < _abilityInputs.Length; i++)
            {
                int index = i; // Capture for closure
                if (_abilityInputs[i] != null)
                {
                    _abilityInputs[i].RegisterValueChangedCallback(evt =>
                    {
                        AbilityScoreChanged?.Invoke(index, evt.newValue);
                    });
                }
            }

            // Action buttons
            if (_cancelButton != null)
                _cancelButton.clicked += () => CancelClicked?.Invoke();

            if (_createButton != null)
                _createButton.clicked += () => CreateCharacterClicked?.Invoke();

            if (_rollButton != null)
                _rollButton.clicked += () => RollAbilitiesClicked?.Invoke();
        }

        private void InitializeUIElements()
        {
            // Initialize option buttons from data service
            InitializeOptionButtons(_classButtonsContainer, CharacterCreationDataService.AvailableClasses, 
                (name) => ClassSelected?.Invoke(name));
            InitializeOptionButtons(_raceButtonsContainer, CharacterCreationDataService.AvailableRaces, 
                (name) => RaceSelected?.Invoke(name));
            InitializeOptionButtons(_backgroundButtonsContainer, CharacterCreationDataService.AvailableBackgrounds, 
                (name) => BackgroundSelected?.Invoke(name));

            // Initialize stat display rows (created in UXML, just need to query labels)
            InitializeAbilityStatRows();
            InitializeCharacterStatItems();
        }

        private void InitializeOptionButtons(VisualElement container, string[] options, System.Action<string> onClick)
        {
            if (container == null) return;

            foreach (string optionName in options)
            {
                CreateOptionButton(container, optionName, () => onClick(optionName));
            }
        }

        private void CreateOptionButton(VisualElement parent, string name, System.Action onClick)
        {
            if (parent == null) return;

            Button button = new Button();
            button.AddToClassList("character-creation-option-button");
            button.name = $"option-{name.ToLower().Replace(" ", "-")}";
            button.text = name;
            button.clicked += () => onClick?.Invoke();

            parent.Add(button);
        }

        private void InitializeAbilityStatRows()
        {
            if (_abilityScoresGrid == null) return;

            // Create ability stat rows if they don't exist
            if (_abilityScoresGrid.childCount == 0)
            {
                string[] abilityNames = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
                foreach (string abilityName in abilityNames)
                {
                    VisualElement row = CreateAbilityStatRow(abilityName);
                    _abilityScoresGrid.Add(row);
                }
            }
        }

        private VisualElement CreateAbilityStatRow(string abilityName)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("character-creation-ability-stat-row");
            row.name = $"ability-stat-{abilityName.ToLower()}";

            Label nameLabel = new Label(abilityName);
            nameLabel.AddToClassList("character-creation-ability-stat-name");
            row.Add(nameLabel);

            VisualElement values = new VisualElement();
            values.AddToClassList("character-creation-ability-stat-values");

            // Score column
            VisualElement scoreColumn = new VisualElement();
            scoreColumn.AddToClassList("character-creation-ability-stat-column");
            Label scoreLabel = new Label("Score");
            scoreLabel.AddToClassList("character-creation-ability-stat-label");
            Label scoreValue = new Label("10");
            scoreValue.AddToClassList("character-creation-ability-score-value");
            scoreValue.name = $"ability-score-{abilityName.ToLower()}";
            scoreColumn.Add(scoreLabel);
            scoreColumn.Add(scoreValue);
            values.Add(scoreColumn);

            // Modifier column
            VisualElement modColumn = new VisualElement();
            modColumn.AddToClassList("character-creation-ability-stat-column");
            Label modLabel = new Label("Mod");
            modLabel.AddToClassList("character-creation-ability-stat-label");
            Label modValue = new Label("+0");
            modValue.AddToClassList("character-creation-ability-modifier-value");
            modValue.name = $"ability-mod-{abilityName.ToLower()}";
            modColumn.Add(modLabel);
            modColumn.Add(modValue);
            values.Add(modColumn);

            row.Add(values);
            return row;
        }

        private void InitializeCharacterStatItems()
        {
            // Create character stat items if they don't exist
            if (_characterStatsGrid != null && _characterStatsGrid.childCount == 0)
            {
                CreateCharacterStatItem(_characterStatsGrid, "Hit Points", "10", "hp-value");
                CreateCharacterStatItem(_characterStatsGrid, "Armor Class", "11", "ac-value");
                CreateCharacterStatItem(_characterStatsGrid, "Initiative", "+1", "initiative-value");
                CreateCharacterStatItem(_characterStatsGrid, "Proficiency", "+2", "proficiency-value");
            }

            if (_spellcastingStatsGrid != null && _spellcastingStatsGrid.childCount == 0)
            {
                CreateCharacterStatItem(_spellcastingStatsGrid, "Spell Save DC", "13", "spell-save-dc-value");
                CreateCharacterStatItem(_spellcastingStatsGrid, "Spell Attack", "+5", "spell-attack-value");
            }

            if (_physicalTraitsGrid != null && _physicalTraitsGrid.childCount == 0)
            {
                CreateCharacterStatItem(_physicalTraitsGrid, "Size", "Medium", "size-value");
                CreateCharacterStatItem(_physicalTraitsGrid, "Speed", "25 ft", "speed-value");
                CreateCharacterStatItem(_physicalTraitsGrid, "Darkvision", "60 ft", "darkvision-value");
            }
        }

        private void CreateCharacterStatItem(VisualElement parent, string label, string value, string valueName)
        {
            if (parent == null) return;

            VisualElement item = new VisualElement();
            item.AddToClassList("character-creation-char-stat-item");

            Label labelElement = new Label(label);
            labelElement.AddToClassList("character-creation-char-stat-label");
            item.Add(labelElement);

            Label valueElement = new Label(value);
            valueElement.AddToClassList("character-creation-char-stat-value");
            valueElement.name = valueName;
            item.Add(valueElement);

            parent.Add(item);
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

        public void UpdateView(CharacterCreationState state)
        {
            if (_root == null) return;

            // Update visibility
            if (state.IsVisible)
                Show();
            else
                Hide();

            // Update selected options
            UpdateOptionSelection(_classButtonsContainer, state.SelectedClass);
            UpdateOptionSelection(_raceButtonsContainer, state.SelectedRace);
            UpdateOptionSelection(_backgroundButtonsContainer, state.SelectedBackground);

            // Update ability scores
            if (state.AbilityScores != null && state.AbilityScores.Length == 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (_abilityInputs[i] != null)
                    {
                        _abilityInputs[i].value = state.AbilityScores[i];
                    }
                }
            }
        }

        private void UpdateOptionSelection(VisualElement container, string selectedName)
        {
            if (container == null || string.IsNullOrEmpty(selectedName)) return;

            foreach (VisualElement element in container.Children())
            {
                if (element is Button button)
                {
                    string normalizedName = selectedName.ToLower().Replace(" ", "-");
                    if (button.name.Contains(normalizedName))
                    {
                        button.AddToClassList("selected");
                    }
                    else
                    {
                        button.RemoveFromClassList("selected");
                    }
                }
            }
        }

        /// <summary>
        /// Updates the detail panel with provided information.
        /// Called by Presenter with calculated data.
        /// </summary>
        public void UpdateDetailPanel(string name, string type, string description, System.Collections.Generic.List<FeatureData> features)
        {
            if (_detailName != null) _detailName.text = name ?? string.Empty;
            if (_detailType != null) _detailType.text = type ?? string.Empty;
            if (_detailContent != null) _detailContent.text = description ?? string.Empty;

            ClearFeatures();
            if (features != null)
            {
                foreach (var feature in features)
                {
                    AddFeature(feature.Name, feature.Description);
                }
            }
        }

        /// <summary>
        /// Updates ability score displays with calculated values.
        /// Called by Presenter with calculated data.
        /// </summary>
        public void UpdateAbilityScoreDisplay(int index, int score, int modifier)
        {
            string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
            if (index < 0 || index >= abilityNames.Length) return;

            string abilityName = abilityNames[index];

            // Update score display
            Label scoreLabel = _root.Q<Label>($"ability-score-{abilityName}");
            if (scoreLabel != null)
            {
                scoreLabel.text = score.ToString();
                scoreLabel.RemoveFromClassList("increased");
                scoreLabel.RemoveFromClassList("decreased");
                scoreLabel.AddToClassList("neutral");
            }

            // Update modifier display
            Label modLabel = _root.Q<Label>($"ability-mod-{abilityName}");
            if (modLabel != null)
            {
                modLabel.text = modifier >= 0 ? $"+{modifier}" : modifier.ToString();
                if (modifier < 0)
                {
                    modLabel.AddToClassList("negative");
                }
                else
                {
                    modLabel.RemoveFromClassList("negative");
                }
            }
        }

        /// <summary>
        /// Updates derived character stats display.
        /// Called by Presenter with calculated data.
        /// </summary>
        public void UpdateDerivedStats(int hitPoints, int armorClass, int initiative, int proficiencyBonus, 
            int? spellSaveDC = null, int? spellAttack = null)
        {
            UpdateStatLabel("hp-value", hitPoints.ToString());
            UpdateStatLabel("ac-value", armorClass.ToString());
            UpdateStatLabel("initiative-value", initiative >= 0 ? $"+{initiative}" : initiative.ToString());
            UpdateStatLabel("proficiency-value", proficiencyBonus >= 0 ? $"+{proficiencyBonus}" : proficiencyBonus.ToString());

            if (spellSaveDC.HasValue)
            {
                UpdateStatLabel("spell-save-dc-value", spellSaveDC.Value.ToString());
            }

            if (spellAttack.HasValue)
            {
                UpdateStatLabel("spell-attack-value", spellAttack.Value >= 0 ? $"+{spellAttack.Value}" : spellAttack.Value.ToString());
            }
        }

        private void UpdateStatLabel(string labelName, string value)
        {
            Label label = _root.Q<Label>(labelName);
            if (label != null)
            {
                label.text = value;
            }
        }

        private void AddFeature(string name, string description)
        {
            if (_featuresSection == null) return;

            VisualElement feature = new VisualElement();
            feature.AddToClassList("character-creation-feature-item");

            Label nameLabel = new Label(name);
            nameLabel.AddToClassList("character-creation-feature-name");
            feature.Add(nameLabel);

            Label descLabel = new Label(description);
            descLabel.AddToClassList("character-creation-feature-description");
            feature.Add(descLabel);

            _featuresSection.Add(feature);
        }

        private void ClearFeatures()
        {
            if (_featuresSection == null) return;
            _featuresSection.Clear();
        }
    }
}

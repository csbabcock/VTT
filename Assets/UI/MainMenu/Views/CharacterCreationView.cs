using GameCore.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// View for character creation UI.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CharacterCreationView : MonoBehaviour, IUIView<CharacterCreationState>
    {
        [Header("Assets")]
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
        private VisualElement _detailPanel;
        private Label _detailName;
        private Label _detailType;
        private Label _detailContent;
        private VisualElement _featuresSection;

        // Stats panel
        private VisualElement _abilityScoresGrid;
        private VisualElement _characterStatsGrid;
        private VisualElement _spellcastingStatsGrid;
        private VisualElement _physicalTraitsGrid;

        // Proficiency panel
        private VisualElement _proficiencyPanel;

        // Action buttons
        private Button _cancelButton;
        private Button _createButton;
        private Button _rollButton;

        // Events
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

            // Add stylesheet if assigned - add to both UIDocument and root
            if (_characterCreationStyleSheet != null)
            {
                // Add to UIDocument stylesheets
                if (_uiDocument != null && !_uiDocument.rootVisualElement.styleSheets.Contains(_characterCreationStyleSheet))
                {
                    _uiDocument.rootVisualElement.styleSheets.Add(_characterCreationStyleSheet);
                }
                
                // Also add to root element
                if (!_root.styleSheets.Contains(_characterCreationStyleSheet))
                {
                    _root.styleSheets.Add(_characterCreationStyleSheet);
                }
            }
            else
            {
                Debug.LogWarning("CharacterCreationView: Stylesheet not assigned! Please assign CharacterCreationView.uss to the _characterCreationStyleSheet field in the inspector.");
            }

            QueryUIElements();
            SetupEventHandlers();
            InitializeOptionCards();
            InitializeAbilityStatRows();
            InitializeCharacterStatItems();
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
            _detailPanel = _root.Q<VisualElement>("detail-panel");
            _detailName = _root.Q<Label>("detail-name");
            _detailType = _root.Q<Label>("detail-type");
            _detailContent = _root.Q<Label>("detail-content");
            _featuresSection = _root.Q<VisualElement>("features-section");

            // Stats panel
            _abilityScoresGrid = _root.Q<VisualElement>("ability-scores-grid");
            _characterStatsGrid = _root.Q<VisualElement>("character-stats-grid");
            _spellcastingStatsGrid = _root.Q<VisualElement>("spellcasting-stats-grid");
            _physicalTraitsGrid = _root.Q<VisualElement>("physical-traits-grid");

            // Proficiency panel
            _proficiencyPanel = _root.Q<VisualElement>("proficiency-panel");

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

        private void InitializeOptionCards()
        {
            // Initialize class buttons
            string[] classes = { "Cleric", "Fighter", "Wizard", "Rogue", "Barbarian", "Ranger", "Bard", "Paladin", "Druid" };
            foreach (string className in classes)
            {
                CreateOptionButton(_classButtonsContainer, className, () => ClassSelected?.Invoke(className));
            }

            // Initialize race buttons
            string[] races = { "Hill Dwarf", "High Elf", "Human", "Dragonborn", "Half-Orc", "Tiefling", "Halfling", "Gnome", "Half-Elf" };
            foreach (string raceName in races)
            {
                CreateOptionButton(_raceButtonsContainer, raceName, () => RaceSelected?.Invoke(raceName));
            }

            // Initialize background buttons
            string[] backgrounds = { "Acolyte", "Soldier", "Criminal", "Folk Hero", "Noble", "Sage" };
            foreach (string bgName in backgrounds)
            {
                CreateOptionButton(_backgroundButtonsContainer, bgName, () => BackgroundSelected?.Invoke(bgName));
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

            string[] abilityNames = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
            foreach (string abilityName in abilityNames)
            {
                VisualElement row = CreateAbilityStatRow(abilityName);
                _abilityScoresGrid.Add(row);
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
            // Combat & Defense stats
            CreateCharacterStatItem(_characterStatsGrid, "Hit Points", "10", "hp-value");
            CreateCharacterStatItem(_characterStatsGrid, "Armor Class", "11", "ac-value");
            CreateCharacterStatItem(_characterStatsGrid, "Initiative", "+1", "initiative-value");
            CreateCharacterStatItem(_characterStatsGrid, "Proficiency", "+2", "proficiency-value");

            // Spellcasting stats
            CreateCharacterStatItem(_spellcastingStatsGrid, "Spell Save DC", "13", "spell-save-dc-value");
            CreateCharacterStatItem(_spellcastingStatsGrid, "Spell Attack", "+5", "spell-attack-value");

            // Physical traits
            CreateCharacterStatItem(_physicalTraitsGrid, "Size", "Medium", "size-value");
            CreateCharacterStatItem(_physicalTraitsGrid, "Speed", "25 ft", "speed-value");
            CreateCharacterStatItem(_physicalTraitsGrid, "Darkvision", "60 ft", "darkvision-value");
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

            // Update visibility - always respect the state
            if (state.IsVisible)
            {
                Show();
            }
            else
            {
                Hide();
            }

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

            // Update detail panel based on selection
            UpdateDetailPanel(state);

            // Update stats based on selections
            UpdateCharacterStats(state);
        }

        private void UpdateOptionSelection(VisualElement container, string selectedName)
        {
            if (container == null) return;

            foreach (VisualElement element in container.Children())
            {
                if (element is Button button)
                {
                    if (button.name.Contains(selectedName?.ToLower().Replace(" ", "-") ?? ""))
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

        private void UpdateDetailPanel(CharacterCreationState state)
        {
            // Update detail panel based on what's selected (race takes priority, then class)
            if (!string.IsNullOrEmpty(state.SelectedRace))
            {
                UpdateDetailPanelForRace(state.SelectedRace);
            }
            else if (!string.IsNullOrEmpty(state.SelectedClass))
            {
                UpdateDetailPanelForClass(state.SelectedClass);
            }
        }

        private void UpdateDetailPanelForRace(string raceName)
        {
            if (_detailName != null) _detailName.text = raceName;
            if (_detailType != null) _detailType.text = "Race";
            if (_detailContent != null)
            {
                _detailContent.text = GetRaceDescription(raceName);
            }
            UpdateFeaturesForRace(raceName);
        }

        private void UpdateDetailPanelForClass(string className)
        {
            if (_detailName != null) _detailName.text = className;
            if (_detailType != null) _detailType.text = "Class";
            if (_detailContent != null)
            {
                _detailContent.text = GetClassDescription(className);
            }
            ClearFeatures();
        }

        private string GetRaceDescription(string raceName)
        {
            // Simplified descriptions - in a real implementation, this would come from a data source
            switch (raceName)
            {
                case "Hill Dwarf":
                    return "Hill dwarves are known for their keen senses, deep intuition, and remarkable resilience. Hardy and dependable, they have adapted to life in rugged mountainous terrain, developing exceptional fortitude and wisdom through generations of living in harmony with stone and earth.";
                default:
                    return $"Description for {raceName}.";
            }
        }

        private string GetClassDescription(string className)
        {
            return $"Description for {className}.";
        }

        private void UpdateFeaturesForRace(string raceName)
        {
            if (_featuresSection == null) return;

            ClearFeatures();

            // Add features based on race
            if (raceName == "Hill Dwarf")
            {
                AddFeature("Dwarven Resilience", "You have advantage on saving throws against poison, and you have resistance against poison damage.");
                AddFeature("Dwarven Toughness", "Your hit point maximum increases by 1, and it increases by 1 every time you gain a level.");
                AddFeature("Stonecunning", "Whenever you make an Intelligence (History) check related to the origin of stonework, you are considered proficient in the History skill and add double your proficiency bonus to the check.");
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

        private void UpdateCharacterStats(CharacterCreationState state)
        {
            if (state.AbilityScores == null || state.AbilityScores.Length != 6) return;

            string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
            for (int i = 0; i < 6; i++)
            {
                int score = state.AbilityScores[i];
                int modifier = CalculateModifier(score);

                // Update score display
                Label scoreLabel = _root.Q<Label>($"ability-score-{abilityNames[i]}");
                if (scoreLabel != null)
                {
                    scoreLabel.text = score.ToString();
                    scoreLabel.RemoveFromClassList("increased");
                    scoreLabel.RemoveFromClassList("decreased");
                    scoreLabel.AddToClassList("neutral");
                }

                // Update modifier display
                Label modLabel = _root.Q<Label>($"ability-mod-{abilityNames[i]}");
                if (modLabel != null)
                {
                    modLabel.text = modifier >= 0 ? $"+{modifier}" : modifier.ToString();
                    // Add/remove negative class for styling
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

            // Update derived stats (simplified - would need class/race bonuses in real implementation)
            UpdateDerivedStats(state);
        }

        private int CalculateModifier(int score)
        {
            return (score - 10) / 2;
        }

        private void UpdateDerivedStats(CharacterCreationState state)
        {
            // Simplified calculations - in a real implementation, these would consider class, race, and level
            int conMod = CalculateModifier(state.AbilityScores[2]); // CON
            int dexMod = CalculateModifier(state.AbilityScores[1]); // DEX
            int wisMod = CalculateModifier(state.AbilityScores[4]); // WIS

            // HP (simplified - would use class hit die)
            Label hpLabel = _root.Q<Label>("hp-value");
            if (hpLabel != null)
            {
                int hp = 8 + conMod; // Base 8 for cleric
                hpLabel.text = hp.ToString();
            }

            // AC (simplified)
            Label acLabel = _root.Q<Label>("ac-value");
            if (acLabel != null)
            {
                int ac = 10 + dexMod;
                acLabel.text = ac.ToString();
            }

            // Initiative
            Label initLabel = _root.Q<Label>("initiative-value");
            if (initLabel != null)
            {
                initLabel.text = dexMod >= 0 ? $"+{dexMod}" : dexMod.ToString();
            }

            // Spell Save DC (if spellcaster)
            if (!string.IsNullOrEmpty(state.SelectedClass) && (state.SelectedClass == "Cleric" || state.SelectedClass == "Wizard"))
            {
                Label spellDCLabel = _root.Q<Label>("spell-save-dc-value");
                if (spellDCLabel != null)
                {
                    int spellDC = 8 + 2 + wisMod; // 8 + proficiency + casting modifier
                    spellDCLabel.text = spellDC.ToString();
                }

                Label spellAttackLabel = _root.Q<Label>("spell-attack-value");
                if (spellAttackLabel != null)
                {
                    int spellAttack = 2 + wisMod; // proficiency + casting modifier
                    spellAttackLabel.text = spellAttack >= 0 ? $"+{spellAttack}" : spellAttack.ToString();
                }
            }
        }
    }
}

using System;
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

        // Tab buttons
        private Button[] _tabButtons;
        private VisualElement[] _tabContents;

        // Option button containers
        private VisualElement _classButtonsContainer;
        private VisualElement _raceButtonsContainer;

        // Ability score inputs (using Labels for display since they're read-only)
        private Label[] _abilityScoreLabels;

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
        private VisualElement _rolledScoresPool;
        private VisualElement _rolledScoresContainer;

        // Action buttons
        private Button _cancelButton;
        private Button _createButton;
        private Button _rollButton;

        // Drag and drop visual state (UI only - no business logic)
        private VisualElement _dragPreview; // Visual preview of dragged score

        // Events - View only raises events, delegates all logic to Presenter
        public event System.Action<string> ClassSelected;
        public event System.Action<string> RaceSelected;
        public event System.Action RollAbilitiesClicked;
        public event System.Action<int, int> DragStartedFromRolledScore; // rolledScoreIndex, scoreValue
        public event System.Action<int> DragStartedFromAbility; // abilityIndex
        public event System.Action<Vector2> DropOccurred; // position
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
            // Tab buttons and content
            _tabButtons = new Button[2]
            {
                _root.Q<Button>("tab-class"),
                _root.Q<Button>("tab-race")
            };

            _tabContents = new VisualElement[2]
            {
                _root.Q<VisualElement>("tab-class-content"),
                _root.Q<VisualElement>("tab-race-content")
            };

            // Option button containers
            _classButtonsContainer = _root.Q<VisualElement>("class-buttons-container");
            _raceButtonsContainer = _root.Q<VisualElement>("race-buttons-container");

            // Ability score labels will be queried after they are created in InitializeAbilityStatRows
            _abilityScoreLabels = new Label[6];

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
            _rolledScoresPool = _root.Q<VisualElement>("rolled-scores-pool");
            _rolledScoresContainer = _root.Q<VisualElement>("rolled-scores-container");

            // Action buttons
            _cancelButton = _root.Q<Button>("cancel-button");
            _createButton = _root.Q<Button>("create-button");
            _rollButton = _root.Q<Button>("roll-abilities-button");
        }

        private void SetupEventHandlers()
        {
            // Tab buttons
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].clicked += () => SwitchTab(tabIndex);
                }
            }

            // Ability score input event handlers are set up in InitializeAbilityStatRows
            // after the inputs are dynamically created

            // Action buttons
            if (_cancelButton != null)
                _cancelButton.clicked += () => CancelClicked?.Invoke();

            if (_createButton != null)
                _createButton.clicked += () => CreateCharacterClicked?.Invoke();

            if (_rollButton != null)
                _rollButton.clicked += () => RollAbilitiesClicked?.Invoke();
        }

        private void SwitchTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabButtons.Length || tabIndex >= _tabContents.Length)
                return;

            // Update tab buttons
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                {
                    if (i == tabIndex)
                        _tabButtons[i].AddToClassList("active");
                    else
                        _tabButtons[i].RemoveFromClassList("active");
                }
            }

            // Update tab content
            for (int i = 0; i < _tabContents.Length; i++)
            {
                if (_tabContents[i] != null)
                {
                    if (i == tabIndex)
                    {
                        _tabContents[i].AddToClassList("active");
                        _tabContents[i].style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        _tabContents[i].RemoveFromClassList("active");
                        _tabContents[i].style.display = DisplayStyle.None;
                    }
                }
            }
        }

        private void InitializeUIElements()
        {
            // Initialize option buttons from data service
            InitializeOptionButtons(_classButtonsContainer, CharacterCreationDataService.AvailableClasses, 
                (name) => ClassSelected?.Invoke(name));
            InitializeOptionButtons(_raceButtonsContainer, CharacterCreationDataService.AvailableRaces, 
                (name) => RaceSelected?.Invoke(name));

            // Initialize stat display rows (created in UXML, just need to query labels)
            InitializeAbilityStatRows();
            InitializeCharacterStatItems();
            SetupDragAndDrop();
            
            // Hide rolled scores pool by default
            if (_rolledScoresPool != null)
            {
                _rolledScoresPool.style.display = DisplayStyle.None;
            }
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

            // Query ability score labels after they are created
            string[] inputNames = { "str", "dex", "con", "int", "wis", "cha" };
            for (int i = 0; i < inputNames.Length; i++)
            {
                _abilityScoreLabels[i] = _root.Q<Label>($"ability-{inputNames[i]}-score-label");
            }
        }

        private VisualElement CreateAbilityStatRow(string abilityName)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("character-creation-ability-stat-row");
            row.name = $"ability-stat-{abilityName.ToLower()}";
            row.userData = abilityName; // Store ability name for drag/drop

            Label nameLabel = new Label(abilityName);
            nameLabel.AddToClassList("character-creation-ability-stat-name");
            row.Add(nameLabel);

            VisualElement values = new VisualElement();
            values.AddToClassList("character-creation-ability-stat-values");

            // Score input column
            VisualElement scoreColumn = new VisualElement();
            scoreColumn.AddToClassList("character-creation-ability-stat-column");
            Label scoreLabel = new Label("Score");
            scoreLabel.AddToClassList("character-creation-ability-stat-label");
            
            // Drop zone container for the score
            VisualElement scoreDropZone = new VisualElement();
            scoreDropZone.AddToClassList("character-creation-ability-score-drop-zone");
            scoreDropZone.name = $"ability-{abilityName.ToLower()}-drop-zone";
            scoreDropZone.userData = abilityName; // Store ability name for easier lookup
            
            Label scoreValueLabel = new Label(""); // Blank when unassigned
            scoreValueLabel.AddToClassList("character-creation-ability-score-value");
            scoreValueLabel.name = $"ability-{abilityName.ToLower()}-score-label";
            scoreDropZone.Add(scoreValueLabel);
            
            // Make drop zone accept pointer events
            scoreDropZone.pickingMode = PickingMode.Position;
            
            scoreColumn.Add(scoreLabel);
            scoreColumn.Add(scoreDropZone);
            values.Add(scoreColumn);

            // Modifier column
            VisualElement modColumn = new VisualElement();
            modColumn.AddToClassList("character-creation-ability-stat-column");
            Label modLabel = new Label("Mod");
            modLabel.AddToClassList("character-creation-ability-stat-label");
            Label modValue = new Label("—");
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

            // Update rolled scores pool
            UpdateRolledScores(state.RolledScores, state.AssignedRolledScoreIndices);

            // Update ability scores without triggering change events
            if (state.AbilityScores != null && state.AbilityScores.Length == 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (_abilityScoreLabels[i] != null)
                    {
                        int score = state.AbilityScores[i];
                        string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
                        VisualElement row = _root.Q<VisualElement>($"ability-stat-{abilityNames[i]}");
                        
                        if (score < 0)
                        {
                            _abilityScoreLabels[i].text = ""; // Blank when unassigned
                            if (row != null)
                            {
                                row.AddToClassList("unassigned");
                                row.RemoveFromClassList("assigned");
                            }
                        }
                        else
                        {
                            _abilityScoreLabels[i].text = score.ToString();
                            if (row != null)
                            {
                                row.RemoveFromClassList("unassigned");
                                row.AddToClassList("assigned");
                            }
                        }
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

            // Update score label value
            if (_abilityScoreLabels[index] != null)
            {
                VisualElement row = _root.Q<VisualElement>($"ability-stat-{abilityName}");
                
                if (score < 0)
                {
                    _abilityScoreLabels[index].text = ""; // Blank when unassigned
                    if (row != null)
                    {
                        row.AddToClassList("unassigned");
                        row.RemoveFromClassList("assigned");
                    }
                }
                else
                {
                    _abilityScoreLabels[index].text = score.ToString();
                    if (row != null)
                    {
                        row.RemoveFromClassList("unassigned");
                        row.AddToClassList("assigned");
                    }
                }
            }

            // Update modifier display
            Label modLabel = _root.Q<Label>($"ability-mod-{abilityName}");
            if (modLabel != null)
            {
                if (score < 0)
                {
                    modLabel.text = "—";
                    modLabel.RemoveFromClassList("negative");
                }
                else
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

        private void SetupDragAndDrop()
        {
            // Setup ability rows to raise drag events
            if (_abilityScoresGrid != null)
            {
                string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
                for (int i = 0; i < abilityNames.Length; i++)
                {
                    VisualElement row = _root.Q<VisualElement>($"ability-stat-{abilityNames[i]}");
                    if (row != null)
                    {
                        int abilityIndex = i; // Capture for closure
                        SetupDropZone(row, abilityIndex);
                    }
                }
            }

            // Register global pointer move and up to notify Presenter
            if (_root != null)
            {
                _root.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
                _root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
            }
        }


        private void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            // Update drag preview position if it exists
            // Presenter will handle visual feedback via public methods
            if (_dragPreview != null && _root != null)
            {
                Vector2 screenPos = evt.position;
                Rect rootRect = _root.worldBound;
                Vector2 localPos = new Vector2(screenPos.x - rootRect.x, screenPos.y - rootRect.y);
                _dragPreview.style.left = localPos.x - 25;
                _dragPreview.style.top = localPos.y - 25;
            }
        }

        private void OnGlobalPointerUp(PointerUpEvent evt)
        {
            // Simply notify Presenter of drop - Presenter handles all logic
            DropOccurred?.Invoke(evt.position);
        }


        private void RemoveDragPreview()
        {
            if (_dragPreview != null && _root != null)
            {
                _root.Remove(_dragPreview);
                _dragPreview = null;
            }
        }

        // ========== Public UI Update Methods (Called by Presenter) ==========

        /// <summary>
        /// Shows a drag preview element with the given score value.
        /// Called by Presenter when drag starts.
        /// </summary>
        public void ShowDragPreview(int scoreValue)
        {
            if (_root == null) return;
            RemoveDragPreview();

            _dragPreview = new VisualElement();
            _dragPreview.AddToClassList("character-creation-rolled-score-item");
            _dragPreview.AddToClassList("drag-preview");
            _dragPreview.style.position = Position.Absolute;
            _dragPreview.style.left = 0;
            _dragPreview.style.top = 0;
            _dragPreview.pickingMode = PickingMode.Ignore;
            _dragPreview.focusable = false;

            Label valueLabel = new Label(scoreValue.ToString());
            valueLabel.AddToClassList("character-creation-rolled-score-value");
            valueLabel.pickingMode = PickingMode.Ignore;
            _dragPreview.Add(valueLabel);

            _root.Add(_dragPreview);
            _dragPreview.BringToFront();
        }

        /// <summary>
        /// Updates the drag preview position to follow the cursor.
        /// Called by Presenter during drag.
        /// </summary>
        public void UpdateDragPreviewPosition(Vector2 position)
        {
            if (_dragPreview == null || _root == null) return;

            Vector2 screenPos = position;
            Rect rootRect = _root.worldBound;
            Vector2 localPos = new Vector2(screenPos.x - rootRect.x, screenPos.y - rootRect.y);

            _dragPreview.style.left = localPos.x - 25;
            _dragPreview.style.top = localPos.y - 25;
        }

        /// <summary>
        /// Hides the drag preview.
        /// Called by Presenter when drag ends.
        /// </summary>
        public void HideDragPreview()
        {
            RemoveDragPreview();
        }

        /// <summary>
        /// Highlights a drop zone to indicate it can accept a drop.
        /// Called by Presenter during drag.
        /// </summary>
        public void HighlightDropZone(int abilityIndex)
        {
            if (_abilityScoresGrid == null) return;

            string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
            if (abilityIndex < 0 || abilityIndex >= abilityNames.Length) return;

            VisualElement row = _root.Q<VisualElement>($"ability-stat-{abilityNames[abilityIndex]}");
            if (row != null)
            {
                row.AddToClassList("drag-over");
            }
        }

        /// <summary>
        /// Highlights the rolled scores container as a drop zone.
        /// Called by Presenter during drag from ability.
        /// </summary>
        public void HighlightRolledScoresContainer()
        {
            if (_rolledScoresContainer != null)
            {
                _rolledScoresContainer.AddToClassList("drag-over");
            }
        }

        /// <summary>
        /// Clears all drop zone highlights.
        /// Called by Presenter when drag ends or moves away.
        /// </summary>
        public void ClearDropZoneHighlights()
        {
            if (_abilityScoresGrid != null)
            {
                foreach (VisualElement row in _abilityScoresGrid.Children())
                {
                    row.RemoveFromClassList("drag-over");
                }
            }

            if (_rolledScoresContainer != null)
            {
                _rolledScoresContainer.RemoveFromClassList("drag-over");
            }
        }

        /// <summary>
        /// Marks a visual element as being dragged (adds dragging class).
        /// Called by Presenter when drag starts.
        /// </summary>
        public void MarkElementAsDragging(VisualElement element)
        {
            if (element != null)
            {
                element.AddToClassList("dragging");
            }
        }

        /// <summary>
        /// Unmarks a visual element as being dragged (removes dragging class).
        /// Called by Presenter when drag ends.
        /// </summary>
        public void UnmarkElementAsDragging(VisualElement element)
        {
            if (element != null)
            {
                element.RemoveFromClassList("dragging");
            }
        }

        private void SetupDropZone(VisualElement dropZone, int abilityIndex)
        {
            // Allow dragging from ability row - just raise event, Presenter handles logic
            Label scoreLabel = dropZone.Q<Label>($"ability-{dropZone.userData.ToString().ToLower()}-score-label");
            if (scoreLabel != null)
            {
                scoreLabel.RegisterCallback<PointerDownEvent>(evt =>
                {
                    // Check if this ability has an assigned score (label text is not empty)
                    if (!string.IsNullOrEmpty(scoreLabel.text) && evt.button == 0)
                    {
                        DragStartedFromAbility?.Invoke(abilityIndex);
                        evt.StopPropagation();
                    }
                });
            }
        }

        private VisualElement CreateRolledScoreElement(int rolledScoreIndex, int scoreValue, bool isAssigned)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("character-creation-rolled-score-item");
            item.name = $"rolled-score-{rolledScoreIndex}";
            item.userData = rolledScoreIndex;

            if (isAssigned)
            {
                item.AddToClassList("assigned");
            }

            Label valueLabel = new Label(scoreValue.ToString());
            valueLabel.AddToClassList("character-creation-rolled-score-value");
            item.Add(valueLabel);

            // Setup drag handler - just raise event, Presenter handles logic
            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0 && !isAssigned) // Left mouse button
                {
                    DragStartedFromRolledScore?.Invoke(rolledScoreIndex, scoreValue);
                    evt.StopPropagation();
                }
            });

            return item;
        }

        public void UpdateRolledScores(int[] rolledScores, int[] assignedRolledScoreIndices)
        {
            if (_rolledScoresContainer == null) return;

            _rolledScoresContainer.Clear();

            if (rolledScores == null || rolledScores.Length != 6)
            {
                // Hide pool if no scores
                if (_rolledScoresPool != null)
                {
                    _rolledScoresPool.style.display = DisplayStyle.None;
                }
                return;
            }

            // Show pool when scores are available
            if (_rolledScoresPool != null)
            {
                _rolledScoresPool.style.display = DisplayStyle.Flex;
            }

            for (int i = 0; i < 6; i++)
            {
                bool isAssigned = false;
                if (assignedRolledScoreIndices != null)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        if (assignedRolledScoreIndices[j] == i)
                        {
                            isAssigned = true;
                            break;
                        }
                    }
                }

                VisualElement scoreElement = CreateRolledScoreElement(i, rolledScores[i], isAssigned);
                _rolledScoresContainer.Add(scoreElement);
            }
        }
    }
}

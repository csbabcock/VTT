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

        // Drag and drop state
        private VisualElement _draggedElement;
        private int _draggedRolledScoreIndex = -1;
        private bool _isDraggingFromAbility = false; // True if dragging from ability row to unassign
        private VisualElement _dragPreview; // Visual preview of dragged score
        private int _draggedScoreValue = -1; // The actual score value being dragged
        private int _sourceAbilityIndex = -1; // If dragging from ability, which ability index (-1 if from pool)
        private int[] _currentAssignedRolledScoreIndices; // Track which rolled scores are assigned to which abilities

        // Events - View only raises events, doesn't handle business logic
        public event System.Action<string> ClassSelected;
        public event System.Action<string> RaceSelected;
        public event System.Action RollAbilitiesClicked;
        public event System.Action<int, int> RolledScoreAssignedToAbility; // rolledScoreIndex, abilityIndex
        public event System.Action<int> AbilityScoreUnassigned; // abilityIndex
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
            
            // Store assigned indices for drag/drop logic
            if (state.AssignedRolledScoreIndices != null)
            {
                _currentAssignedRolledScoreIndices = new int[6];
                Array.Copy(state.AssignedRolledScoreIndices, _currentAssignedRolledScoreIndices, 6);
            }

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
            // Setup ability rows as drop zones
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

            // Setup rolled scores container as drop zone for unassigning
            if (_rolledScoresContainer != null)
            {
                _rolledScoresContainer.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    if (_draggedElement != null && _isDraggingFromAbility)
                    {
                        _rolledScoresContainer.AddToClassList("drag-over");
                    }
                });

                _rolledScoresContainer.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    _rolledScoresContainer.RemoveFromClassList("drag-over");
                });

                _rolledScoresContainer.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (_draggedElement != null && _isDraggingFromAbility)
                    {
                        // Find which ability this is
                        string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
                        for (int i = 0; i < abilityNames.Length; i++)
                        {
                            if (_draggedElement.name == $"ability-stat-{abilityNames[i]}")
                            {
                                AbilityScoreUnassigned?.Invoke(i);
                                break;
                            }
                        }
                        _rolledScoresContainer.RemoveFromClassList("drag-over");
                        RemoveDragPreview();
                        _draggedElement = null;
                        _draggedRolledScoreIndex = -1;
                        _draggedScoreValue = -1;
                        _isDraggingFromAbility = false;
                    }
                });
            }

            // Register global pointer move and up to track drag
            if (_root != null)
            {
                _root.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
                _root.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMoveForPreview);
                _root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
            }
        }

        private VisualElement FindAncestorByName(VisualElement element, string name)
        {
            VisualElement current = element;
            while (current != null)
            {
                if (current.name == name)
                    return current;
                current = current.parent;
            }
            return null;
        }

        private VisualElement FindAncestorByClass(VisualElement element, string className)
        {
            if (element == null) return null;
            
            VisualElement current = element;
            int depth = 0;
            const int maxDepth = 20; // Safety limit
            
            while (current != null && depth < maxDepth)
            {
                if (current.ClassListContains(className))
                    return current;
                current = current.parent;
                depth++;
            }
            return null;
        }

        private void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            if (_draggedElement == null) return;

            // Find element under pointer
            VisualElement elementUnderPointer = evt.target as VisualElement;
            if (elementUnderPointer == null) return;

            if (_isDraggingFromAbility)
            {
                // Dragging from ability row - check if over rolled scores container
                VisualElement rolledContainer = FindAncestorByName(elementUnderPointer, "rolled-scores-container");
                if (rolledContainer != null)
                {
                    rolledContainer.AddToClassList("drag-over");
                }
                else
                {
                    if (_rolledScoresContainer != null)
                    {
                        _rolledScoresContainer.RemoveFromClassList("drag-over");
                    }
                }
            }
            else
            {
                // Dragging from rolled score - check if over an ability row
                VisualElement dropZone = FindAncestorByClass(elementUnderPointer, "character-creation-ability-stat-row");
                if (dropZone != null)
                {
                    // Remove drag-over from all rows first
                    if (_abilityScoresGrid != null)
                    {
                        foreach (VisualElement row in _abilityScoresGrid.Children())
                        {
                            if (row != dropZone)
                                row.RemoveFromClassList("drag-over");
                        }
                    }
                    dropZone.AddToClassList("drag-over");
                }
                else
                {
                    // Remove all drag-over states
                    if (_abilityScoresGrid != null)
                    {
                        foreach (VisualElement row in _abilityScoresGrid.Children())
                        {
                            row.RemoveFromClassList("drag-over");
                        }
                    }
                }
            }
        }

        private void OnGlobalPointerMoveForPreview(PointerMoveEvent evt)
        {
            if (_dragPreview == null || _root == null) return;

            // Update drag preview position to follow cursor
            // evt.position is in screen coordinates, convert to local coordinates
            Vector2 screenPos = evt.position;
            
            // Get the root's world position to calculate offset
            Rect rootRect = _root.worldBound;
            Vector2 localPos = new Vector2(screenPos.x - rootRect.x, screenPos.y - rootRect.y);
            
            _dragPreview.style.left = localPos.x - 25; // Offset by half width (50px / 2)
            _dragPreview.style.top = localPos.y - 25; // Offset by half height (50px / 2)
        }

        private void OnGlobalPointerUp(PointerUpEvent evt)
        {
            if (_draggedElement == null) return;

            // Use the panel's Pick method to find what's actually under the pointer
            // This is more reliable than evt.target which might be the dragged element
            VisualElement target = null;
            if (_root != null && _root.panel != null)
            {
                target = _root.panel.Pick(evt.position);
            }
            
            // Fallback to evt.target if Pick doesn't work
            if (target == null)
            {
                target = evt.target as VisualElement;
            }
            
            if (target == null)
            {
                ReturnDraggedScoreToPool();
                return;
            }

            // Skip if target is the drag preview or its children
            if (target.ClassListContains("drag-preview"))
            {
                return;
            }
            
            // Check if target is a child of drag preview
            VisualElement current = target.parent;
            while (current != null)
            {
                if (current.ClassListContains("drag-preview"))
                {
                    return;
                }
                current = current.parent;
            }
            
            // Skip if target is the dragged element itself or its children
            if (target == _draggedElement)
            {
                return;
            }
            
            current = target.parent;
            while (current != null)
            {
                if (current == _draggedElement)
                {
                    return;
                }
                current = current.parent;
            }

            // First try to find ability row (more reliable since it's higher in hierarchy)
            VisualElement abilityRow = FindAncestorByClass(target, "character-creation-ability-stat-row");
            
            // If we found the ability row, try to find the drop zone within it
            VisualElement dropZone = null;
            if (abilityRow != null)
            {
                // Try finding drop zone by traversing up from target first
                dropZone = FindAncestorByClass(target, "character-creation-ability-score-drop-zone");
                
                // If not found, query it directly from the ability row
                if (dropZone == null)
                {
                    dropZone = abilityRow.Q<VisualElement>(className: "character-creation-ability-score-drop-zone");
                }
            }
            else
            {
                // Try finding drop zone directly if we didn't find the row
                dropZone = FindAncestorByClass(target, "character-creation-ability-score-drop-zone");
            }

            if (abilityRow != null)
            {
                // Find which ability this is by checking the row name
                string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
                int targetAbilityIndex = -1;
                string rowName = abilityRow.name;
                
                for (int i = 0; i < abilityNames.Length; i++)
                {
                    if (rowName == $"ability-stat-{abilityNames[i]}")
                    {
                        targetAbilityIndex = i;
                        break;
                    }
                }

                if (targetAbilityIndex >= 0)
                {
                    HandleDropOnAbility(targetAbilityIndex);
                    return;
                }
            }

            // Check if dropped over rolled scores container
            VisualElement rolledContainer = FindAncestorByName(target, "rolled-scores-container");
            if (rolledContainer != null && _isDraggingFromAbility)
            {
                // Unassign ability score back to pool
                if (_sourceAbilityIndex >= 0)
                {
                    AbilityScoreUnassigned?.Invoke(_sourceAbilityIndex);
                }
                CleanupDrag();
                return;
            }

            // Dropped somewhere else - return to pool
            ReturnDraggedScoreToPool();
        }

        private void HandleDropOnAbility(int targetAbilityIndex)
        {
            if (_draggedElement == null)
            {
                return;
            }

            if (_isDraggingFromAbility)
            {
                // Dragging from one ability to another
                if (_sourceAbilityIndex >= 0 && _sourceAbilityIndex != targetAbilityIndex && _draggedRolledScoreIndex >= 0)
                {
                    // Get the rolled score index currently assigned to target ability (if any)
                    int targetRolledScoreIndex = -1;
                    if (_currentAssignedRolledScoreIndices != null && targetAbilityIndex >= 0 && targetAbilityIndex < 6)
                    {
                        targetRolledScoreIndex = _currentAssignedRolledScoreIndices[targetAbilityIndex];
                    }
                    
                    // Assign the dragged rolled score to target ability
                    // This automatically unassigns it from source ability
                    RolledScoreAssignedToAbility?.Invoke(_draggedRolledScoreIndex, targetAbilityIndex);
                    
                    // If target had a score, assign it to source (swap)
                    if (targetRolledScoreIndex >= 0 && targetRolledScoreIndex != _draggedRolledScoreIndex)
                    {
                        RolledScoreAssignedToAbility?.Invoke(targetRolledScoreIndex, _sourceAbilityIndex);
                    }
                    
                    CleanupDrag();
                }
                else if (_sourceAbilityIndex == targetAbilityIndex)
                {
                    // Dropped on same ability - do nothing, just cleanup
                    CleanupDrag();
                }
            }
            else
            {
                // Dragging from rolled scores pool to ability
                if (_draggedRolledScoreIndex >= 0)
                {
                    RolledScoreAssignedToAbility?.Invoke(_draggedRolledScoreIndex, targetAbilityIndex);
                    CleanupDrag();
                }
            }
        }

        private void ReturnDraggedScoreToPool()
        {
            if (_isDraggingFromAbility && _sourceAbilityIndex >= 0)
            {
                // Return ability score to pool
                AbilityScoreUnassigned?.Invoke(_sourceAbilityIndex);
            }
            // If dragging from pool, it just stays in pool (no action needed)
            CleanupDrag();
        }

        private void CleanupDrag()
        {
            // Remove drag-over from all elements
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
            if (_draggedElement != null)
            {
                _draggedElement.RemoveFromClassList("dragging");
            }
            RemoveDragPreview();
            _draggedElement = null;
            _draggedRolledScoreIndex = -1;
            _draggedScoreValue = -1;
            _sourceAbilityIndex = -1;
            _isDraggingFromAbility = false;
        }

        private void CreateDragPreview(int scoreValue)
        {
            if (_root == null) return;

            // Remove existing preview if any
            RemoveDragPreview();

            // Create preview element
            _dragPreview = new VisualElement();
            _dragPreview.AddToClassList("character-creation-rolled-score-item");
            _dragPreview.AddToClassList("drag-preview");
            _dragPreview.style.position = Position.Absolute;
            _dragPreview.style.left = 0;
            _dragPreview.style.top = 0;
            _dragPreview.pickingMode = PickingMode.Ignore; // Don't interfere with pointer events
            
            // Make sure it doesn't block events
            _dragPreview.focusable = false;

            Label valueLabel = new Label(scoreValue.ToString());
            valueLabel.AddToClassList("character-creation-rolled-score-value");
            valueLabel.pickingMode = PickingMode.Ignore;
            _dragPreview.Add(valueLabel);

            // Add to root but make sure it's at the end so it doesn't interfere
            _root.Add(_dragPreview);
            _dragPreview.BringToFront(); // Put it on top visually but it won't block events due to Ignore
        }

        private void RemoveDragPreview()
        {
            if (_dragPreview != null && _root != null)
            {
                _root.Remove(_dragPreview);
                _dragPreview = null;
            }
        }

        private void SetupDropZone(VisualElement dropZone, int abilityIndex)
        {
            dropZone.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (_draggedElement != null)
                {
                    dropZone.AddToClassList("drag-over");
                }
            });

            dropZone.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                dropZone.RemoveFromClassList("drag-over");
            });

            // Note: Drop handling is now done in OnGlobalPointerUp to handle all cases

            // Allow dragging from ability row to unassign
            Label scoreLabel = dropZone.Q<Label>($"ability-{dropZone.userData.ToString().ToLower()}-score-label");
            if (scoreLabel != null)
            {
                scoreLabel.RegisterCallback<PointerDownEvent>(evt =>
                {
                    // Check if this ability has an assigned score (label text is not empty)
                    if (!string.IsNullOrEmpty(scoreLabel.text) && evt.button == 0)
                    {
                        // Parse the score value from the label
                        if (int.TryParse(scoreLabel.text, out int scoreValue))
                        {
                            // Find which rolled score index is assigned to this ability
                            int rolledScoreIndex = -1;
                            if (_currentAssignedRolledScoreIndices != null && abilityIndex >= 0 && abilityIndex < 6)
                            {
                                rolledScoreIndex = _currentAssignedRolledScoreIndices[abilityIndex];
                            }
                            
                            // Start dragging
                            _draggedElement = dropZone;
                            _draggedRolledScoreIndex = rolledScoreIndex; // Store the rolled score index
                            _draggedScoreValue = scoreValue;
                            _sourceAbilityIndex = abilityIndex;
                            _isDraggingFromAbility = true;
                            dropZone.AddToClassList("dragging");
                            CreateDragPreview(scoreValue);
                            evt.StopPropagation();
                        }
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

            // Setup drag handlers
            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0 && !isAssigned) // Left mouse button
                {
                    _draggedElement = item;
                    _draggedRolledScoreIndex = rolledScoreIndex;
                    _draggedScoreValue = scoreValue;
                    item.AddToClassList("dragging");
                    CreateDragPreview(scoreValue);
                    evt.StopPropagation();
                }
            });

            // Note: Drop handling is now done in OnGlobalPointerUp

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

using GameCore.UI;
using GameCore.UI.MainMenu.Services;
using GameCore.PlayerData.Rulesets;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Presenter for character creation UI.
    /// Connects CharacterCreationModel and CharacterCreationView.
    /// Follows MVP pattern - handles all business logic, delegates UI updates to View.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterCreationPresenter : MonoBehaviour, IUIPresenter<CharacterCreationModel, CharacterCreationView>
    {
        [Header("References")]
        [SerializeField] private CharacterCreationView _view;

        public CharacterCreationModel Model { get; private set; }
        public CharacterCreationView View => _view;

        private bool _initialized;
        private IRulesetCalculator _calculator;
        private DragAndDropHandler _dragAndDropHandler;
        private DragState _currentDragState;

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<CharacterCreationView>();
            }

            Model = new CharacterCreationModel();
            _calculator = RulesetCalculatorFactory.GetDefaultCalculator();
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            if (_view == null)
            {
                Debug.LogError("CharacterCreationPresenter: View reference is missing.");
                return;
            }

            _view.Initialize();

            // Initialize drag and drop handler after view is initialized
            InitializeDragAndDropHandler();

            // Subscribe to view events
            _view.ClassSelected += HandleClassSelected;
            _view.RaceSelected += HandleRaceSelected;
            _view.RollAbilitiesClicked += HandleRollAbilitiesClicked;
            _view.StandardArrayClicked += HandleStandardArrayClicked;
            _view.ManualClicked += HandleManualClicked;
            _view.ManualScoreChanged += HandleManualScoreChanged;
            _view.DragStartedFromRolledScore += HandleDragStartedFromRolledScore;
            _view.DragStartedFromAbility += HandleDragStartedFromAbility;
            _view.DropOccurred += HandleDropOccurred;
            _view.CancelClicked += HandleCancelClicked;
            _view.CreateCharacterClicked += HandleCreateCharacterClicked;

            // Register for pointer move to update drag preview
            if (_view.Root != null)
            {
                _view.Root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            }

            // Subscribe to model events
            Model.StateChanged += HandleModelStateChanged;

            // Start hidden
            _view.Hide();
            _view.UpdateView(Model.State);

            _initialized = true;
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            if (_view != null)
            {
                _view.ClassSelected -= HandleClassSelected;
                _view.RaceSelected -= HandleRaceSelected;
                _view.RollAbilitiesClicked -= HandleRollAbilitiesClicked;
                _view.StandardArrayClicked -= HandleStandardArrayClicked;
                _view.ManualClicked -= HandleManualClicked;
                _view.ManualScoreChanged -= HandleManualScoreChanged;
                _view.DragStartedFromRolledScore -= HandleDragStartedFromRolledScore;
                _view.DragStartedFromAbility -= HandleDragStartedFromAbility;
                _view.DropOccurred -= HandleDropOccurred;
                _view.CancelClicked -= HandleCancelClicked;
                _view.CreateCharacterClicked -= HandleCreateCharacterClicked;

                if (_view.Root != null)
                {
                    _view.Root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                }
            }

            if (Model != null)
            {
                Model.StateChanged -= HandleModelStateChanged;
            }

            _initialized = false;
        }

        public void Show()
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_view != null)
            {
                _view.Show();
            }
            
            Model.SetVisible(true);
        }

        public void Hide()
        {
            if (_view != null)
            {
                _view.Hide();
            }
            
            Model.SetVisible(false);
        }

        private void HandleClassSelected(string className)
        {
            Model.SetSelectedClass(className);
        }

        private void HandleRaceSelected(string raceName)
        {
            Model.SetSelectedRace(raceName);
        }

        private void InitializeDragAndDropHandler()
        {
            if (_view.Root == null) return;

            VisualElement abilityScoresGrid = _view.Root.Q<VisualElement>("ability-scores-grid");
            VisualElement rolledScoresContainer = _view.Root.Q<VisualElement>("rolled-scores-container");
            if (abilityScoresGrid != null && rolledScoresContainer != null)
            {
                _dragAndDropHandler = new DragAndDropHandler(_view.Root, abilityScoresGrid, rolledScoresContainer);
            }
        }

        private void HandleRollAbilitiesClicked()
        {
            Model.SetSelectedScoreMethod("Roll");
            int[] newScores = new int[6];
            for (int i = 0; i < 6; i++)
            {
                newScores[i] = Roll4d6DropLowest();
            }
            Model.SetRolledScores(newScores, isManualMode: false);
        }

        private void HandleStandardArrayClicked()
        {
            Model.SetSelectedScoreMethod("StandardArray");
            // D&D 5e standard array: 15, 14, 13, 12, 10, 8
            Model.SetRolledScores(new int[] { 15, 14, 13, 12, 10, 8 }, isManualMode: false);
        }

        private void HandleManualClicked()
        {
            Model.SetSelectedScoreMethod("Manual");
            Model.SetRolledScores(new int[] { -1, -1, -1, -1, -1, -1 }, isManualMode: true);
        }

        private void HandleManualScoreChanged(int index, string text)
        {
            if (index < 0 || index >= 6) return;
            int value = -1;
            if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text.Trim(), out int parsed))
                value = parsed;
            Model.SetRolledScoreAt(index, value);
        }

        // ========== Drag and Drop Handlers ==========

        private void HandleDragStartedFromRolledScore(int rolledScoreIndex, int scoreValue)
        {
            if (Model.State.RolledScores == null || rolledScoreIndex < 0 || rolledScoreIndex >= 6)
                return;

            // Use value from model (so manual-typed values are correct)
            int value = Model.State.RolledScores[rolledScoreIndex];
            if (value < 3)
                return; // Don't drag empty or invalid manual slots

            // Update drag state
            _currentDragState = new DragState
            {
                IsDragging = true,
                RolledScoreIndex = rolledScoreIndex,
                SourceAbilityIndex = -1,
                IsDraggingFromAbility = false,
                ScoreValue = value
            };

            // Update UI - show drag preview
            _view.ShowDragPreview(value);

            // Mark rolled score element as dragging
            VisualElement rolledScoreElement = _view.Root?.Q<VisualElement>($"rolled-score-{rolledScoreIndex}");
            if (rolledScoreElement != null)
            {
                _view.MarkElementAsDragging(rolledScoreElement);
            }
        }

        private void HandleDragStartedFromAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= 6 || Model.State.AssignedRolledScoreIndices == null)
                return;

            int rolledScoreIndex = Model.State.AssignedRolledScoreIndices[abilityIndex];
            if (rolledScoreIndex < 0 || Model.State.RolledScores == null || rolledScoreIndex >= Model.State.RolledScores.Length)
                return;

            int scoreValue = Model.State.RolledScores[rolledScoreIndex];

            // Update drag state
            _currentDragState = new DragState
            {
                IsDragging = true,
                RolledScoreIndex = rolledScoreIndex,
                SourceAbilityIndex = abilityIndex,
                IsDraggingFromAbility = true,
                ScoreValue = scoreValue
            };

            // Update UI - show drag preview
            _view.ShowDragPreview(scoreValue);

            // Mark ability row as dragging
            string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
            VisualElement abilityRow = _view.Root?.Q<VisualElement>($"ability-stat-{abilityNames[abilityIndex]}");
            if (abilityRow != null)
            {
                _view.MarkElementAsDragging(abilityRow);
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_currentDragState.IsDragging) return;

            // Update drag preview position
            _view.UpdateDragPreviewPosition(evt.position);

            // Update visual feedback for drop zones
            if (_dragAndDropHandler == null) return;

            VisualElement elementUnderPointer = _dragAndDropHandler.GetElementUnderPointer(evt.position);
            if (elementUnderPointer == null)
            {
                _dragAndDropHandler.ClearDropZoneFeedback();
                return;
            }

            // Skip if over drag preview or dragged element
            if (_dragAndDropHandler.IsDragPreviewOrChild(elementUnderPointer))
            {
                return;
            }

            if (_currentDragState.IsDraggingFromAbility)
            {
                // Dragging from ability - highlight rolled scores container if over it
                VisualElement rolledContainer = _dragAndDropHandler.FindRolledScoresContainer(elementUnderPointer);
                if (rolledContainer != null)
                {
                    _view.HighlightRolledScoresContainer();
                    _dragAndDropHandler.ClearDropZoneFeedback();
                }
                else
                {
                    _view.ClearDropZoneHighlights();
                }
            }
            else
            {
                // Dragging from pool - highlight ability rows
                VisualElement abilityRow = _dragAndDropHandler.FindAbilityRow(elementUnderPointer);
                if (abilityRow != null)
                {
                    int abilityIndex = _dragAndDropHandler.GetAbilityIndexFromRow(abilityRow);
                    if (abilityIndex >= 0)
                    {
                        _view.HighlightDropZone(abilityIndex);
                    }
                }
                else
                {
                    _view.ClearDropZoneHighlights();
                }
            }
        }

        private void HandleDropOccurred(Vector2 position)
        {
            if (!_currentDragState.IsDragging) return;

            if (_dragAndDropHandler == null)
            {
                CleanupDrag();
                return;
            }

            // Find element under drop position
            VisualElement target = _dragAndDropHandler.GetElementUnderPointer(position);
            if (target == null)
            {
                // Dropped outside - return to pool if dragging from ability
                if (_currentDragState.IsDraggingFromAbility)
                {
                    Model.UnassignAbilityScore(_currentDragState.SourceAbilityIndex);
                }
                CleanupDrag();
                return;
            }

            // Skip if target is drag preview or dragged element
            if (_dragAndDropHandler.IsDragPreviewOrChild(target))
            {
                return;
            }

            // Check if dropped on ability row
            VisualElement abilityRow = _dragAndDropHandler.FindAbilityRow(target);
            if (abilityRow != null)
            {
                int targetAbilityIndex = _dragAndDropHandler.GetAbilityIndexFromRow(abilityRow);
                if (targetAbilityIndex >= 0)
                {
                    HandleDropOnAbility(targetAbilityIndex);
                    CleanupDrag();
                    return;
                }
            }

            // Check if dropped on rolled scores container
            VisualElement rolledContainer = _dragAndDropHandler.FindRolledScoresContainer(target);
            if (rolledContainer != null && _currentDragState.IsDraggingFromAbility)
            {
                // Unassign ability score back to pool
                Model.UnassignAbilityScore(_currentDragState.SourceAbilityIndex);
                CleanupDrag();
                return;
            }

            // Dropped elsewhere - return to pool if dragging from ability
            if (_currentDragState.IsDraggingFromAbility)
            {
                Model.UnassignAbilityScore(_currentDragState.SourceAbilityIndex);
            }
            CleanupDrag();
        }

        private void HandleDropOnAbility(int targetAbilityIndex)
        {
            if (_currentDragState.IsDraggingFromAbility)
            {
                // Dragging from one ability to another - swap them
                if (_currentDragState.SourceAbilityIndex >= 0 && 
                    _currentDragState.SourceAbilityIndex != targetAbilityIndex && 
                    _currentDragState.RolledScoreIndex >= 0)
                {
                    // Get the rolled score index currently assigned to target ability (if any)
                    int targetRolledScoreIndex = -1;
                    if (Model.State.AssignedRolledScoreIndices != null && 
                        targetAbilityIndex >= 0 && targetAbilityIndex < 6)
                    {
                        targetRolledScoreIndex = Model.State.AssignedRolledScoreIndices[targetAbilityIndex];
                    }

                    // Assign the dragged rolled score to target ability
                    Model.AssignRolledScoreToAbility(_currentDragState.RolledScoreIndex, targetAbilityIndex);

                    // If target had a score, assign it to source (swap)
                    if (targetRolledScoreIndex >= 0 && targetRolledScoreIndex != _currentDragState.RolledScoreIndex)
                    {
                        Model.AssignRolledScoreToAbility(targetRolledScoreIndex, _currentDragState.SourceAbilityIndex);
                    }
                }
                // If dropped on same ability, do nothing (just cleanup)
            }
            else
            {
                // Dragging from rolled scores pool to ability
                if (_currentDragState.RolledScoreIndex >= 0)
                {
                    Model.AssignRolledScoreToAbility(_currentDragState.RolledScoreIndex, targetAbilityIndex);
                }
            }
        }

        private void CleanupDrag()
        {
            // Clear visual feedback
            _view.ClearDropZoneHighlights();
            _view.HideDragPreview();

            // Unmark dragged elements
            if (_currentDragState.IsDraggingFromAbility && _currentDragState.SourceAbilityIndex >= 0)
            {
                string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
                VisualElement abilityRow = _view.Root?.Q<VisualElement>($"ability-stat-{abilityNames[_currentDragState.SourceAbilityIndex]}");
                if (abilityRow != null)
                {
                    _view.UnmarkElementAsDragging(abilityRow);
                }
            }
            else if (_currentDragState.RolledScoreIndex >= 0)
            {
                VisualElement rolledScoreElement = _view.Root?.Q<VisualElement>($"rolled-score-{_currentDragState.RolledScoreIndex}");
                if (rolledScoreElement != null)
                {
                    _view.UnmarkElementAsDragging(rolledScoreElement);
                }
            }

            // Reset drag state
            _currentDragState = DragState.None;
        }

        private int Roll4d6DropLowest()
        {
            int[] rolls = new int[4];
            for (int i = 0; i < 4; i++)
            {
                rolls[i] = Random.Range(1, 7); // 1-6
            }

            // Find lowest and drop it
            int lowest = rolls[0];
            int sum = rolls[0];
            for (int i = 1; i < 4; i++)
            {
                if (rolls[i] < lowest)
                    lowest = rolls[i];
                sum += rolls[i];
            }

            return sum - lowest;
        }

        private void HandleCancelClicked()
        {
            Hide();
        }

        private void HandleCreateCharacterClicked()
        {
            if (!ValidateCharacterCreation())
            {
                return;
            }

            // TODO: Save character to file using CharacterFileService
            Debug.Log($"CharacterCreationPresenter: Creating character - Class: {Model.State.SelectedClass}, Race: {Model.State.SelectedRace}, Background: {Model.State.SelectedBackground}");

            Hide();
            // TODO: Notify MainMenuPresenter to refresh character list
        }

        private bool ValidateCharacterCreation()
        {
            if (string.IsNullOrEmpty(Model.State.SelectedClass))
            {
                Debug.LogWarning("CharacterCreationPresenter: Class must be selected.");
                return false;
            }

            if (string.IsNullOrEmpty(Model.State.SelectedRace))
            {
                Debug.LogWarning("CharacterCreationPresenter: Race must be selected.");
                return false;
            }

            if (string.IsNullOrEmpty(Model.State.SelectedBackground))
            {
                Debug.LogWarning("CharacterCreationPresenter: Background must be selected.");
                return false;
            }

            return true;
        }

        private void HandleModelStateChanged(CharacterCreationState state)
        {
            _view.UpdateView(state);
            UpdateDetailPanel(state);
            UpdateCharacterStats(state);
        }

        private void UpdateDetailPanel(CharacterCreationState state)
        {
            string name = string.Empty;
            string type = string.Empty;
            string description = string.Empty;
            List<FeatureData> features = null;

            // Race takes priority for detail panel
            if (!string.IsNullOrEmpty(state.SelectedRace))
            {
                name = state.SelectedRace;
                type = "Race";
                description = CharacterCreationDataService.GetRaceDescription(state.SelectedRace);
                features = CharacterCreationDataService.GetRaceFeatures(state.SelectedRace);
            }
            else if (!string.IsNullOrEmpty(state.SelectedClass))
            {
                name = state.SelectedClass;
                type = "Class";
                description = CharacterCreationDataService.GetClassDescription(state.SelectedClass);
            }

            _view.UpdateDetailPanel(name, type, description, features);
        }

        private void UpdateCharacterStats(CharacterCreationState state)
        {
            if (state.AbilityScores == null || state.AbilityScores.Length != 6)
                return;

            // Update ability score displays
            for (int i = 0; i < 6; i++)
            {
                int score = state.AbilityScores[i];
                if (score < 0)
                {
                    // Unassigned - show placeholder
                    _view.UpdateAbilityScoreDisplay(i, -1, 0);
                }
                else
                {
                    int modifier = _calculator.CalculateAbilityModifier(score);
                    _view.UpdateAbilityScoreDisplay(i, score, modifier);
                }
            }

            // Calculate each derived stat when its respective ability is assigned
            int? hitPoints = null;
            if (state.AbilityScores[2] >= 0) // CON
            {
                int conMod = _calculator.CalculateAbilityModifier(state.AbilityScores[2]);
                hitPoints = CalculateHitPoints(state.SelectedClass, conMod);
            }

            int? armorClass = null;
            int? initiative = null;
            if (state.AbilityScores[1] >= 0) // DEX
            {
                int dexMod = _calculator.CalculateAbilityModifier(state.AbilityScores[1]);
                armorClass = 10 + dexMod;
                initiative = dexMod;
            }

            bool allAssigned = true;
            for (int i = 0; i < 6; i++)
            {
                if (state.AbilityScores[i] < 0)
                {
                    allAssigned = false;
                    break;
                }
            }
            int? proficiencyBonus = allAssigned ? _calculator.CalculateProficiencyBonus(1) : (int?)null; // Level 1

            int? spellSaveDC = null;
            int? spellAttack = null;
            if (IsSpellcaster(state.SelectedClass) && HasCastingAbilityAssigned(state.SelectedClass, state.AbilityScores))
            {
                int castingModifier = GetCastingModifier(state.SelectedClass, state.AbilityScores);
                int prof = _calculator.CalculateProficiencyBonus(1);
                spellSaveDC = 8 + prof + castingModifier;
                spellAttack = prof + castingModifier;
            }

            _view.UpdateDerivedStats(hitPoints, armorClass, initiative, proficiencyBonus, spellSaveDC, spellAttack);
        }

        private bool HasCastingAbilityAssigned(string className, int[] abilityScores)
        {
            if (abilityScores == null || abilityScores.Length < 6) return false;
            return className switch
            {
                "Wizard" => abilityScores[3] >= 0, // INT
                "Cleric" or "Druid" or "Ranger" => abilityScores[4] >= 0, // WIS
                "Bard" or "Paladin" or "Sorcerer" or "Warlock" => abilityScores[5] >= 0, // CHA
                _ => false
            };
        }

        private int CalculateHitPoints(string className, int conModifier)
        {
            // Simplified - would use class hit die table
            int baseHP = className switch
            {
                "Barbarian" => 12,
                "Fighter" or "Paladin" or "Ranger" => 10,
                "Bard" or "Cleric" or "Druid" or "Monk" or "Rogue" or "Warlock" => 8,
                "Sorcerer" or "Wizard" => 6,
                _ => 8
            };

            return baseHP + conModifier;
        }

        private bool IsSpellcaster(string className)
        {
            return className == "Cleric" || className == "Wizard" || className == "Bard" || 
                   className == "Druid" || className == "Sorcerer" || className == "Warlock" || 
                   className == "Paladin" || className == "Ranger";
        }

        private int GetCastingModifier(string className, int[] abilityScores)
        {
            // Returns the appropriate ability modifier for spellcasting
            return className switch
            {
                "Wizard" => _calculator.CalculateAbilityModifier(abilityScores[3]), // INT
                "Cleric" or "Druid" or "Ranger" => _calculator.CalculateAbilityModifier(abilityScores[4]), // WIS
                "Bard" or "Paladin" or "Sorcerer" or "Warlock" => _calculator.CalculateAbilityModifier(abilityScores[5]), // CHA
                _ => 0
            };
        }
    }
}

using System;
using GameCore.UI;
using GameCore.UI.MainMenu.Services;
using GameCore.PlayerData.Rulesets;
using GameCore.PlayerData.Rulesets.Definitions;
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

        [Header("Ruleset")]
        [SerializeField] private string _rulesetId = "DnD5e";

        public CharacterCreationModel Model { get; private set; }
        public CharacterCreationView View => _view;

        private bool _initialized;
        private IRulesetContentQuery _contentQuery;
        private IRulesetCalculator _calculator;
        private IAbilityScoreRoller _abilityScoreRoller;
        private DragAndDropHandler _dragAndDropHandler;
        private DragState _currentDragState;

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<CharacterCreationView>();
            }

            Model = new CharacterCreationModel();
            _contentQuery = RulesetContentQueryProvider.GetOrCreate(_rulesetId);
            _calculator = RulesetCalculatorFactory.GetDefaultCalculator();
            _abilityScoreRoller = AbilityScoreRollerFactory.GetDefault();
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

            _view.VisualTreeBound += HandleViewVisualTreeBound;
            _view.Initialize();

            // Subscribe to view events
            _view.ClassSelected += HandleClassSelected;
            _view.RaceSelected += HandleRaceSelected;
            _view.RaceAbilityScoreChoiceSelected += HandleRaceAbilityScoreChoiceSelected;
            _view.RaceChoiceSelected += HandleRaceChoiceSelected;
            _view.BackgroundSelected += HandleBackgroundSelected;
            _view.SelectedClassLevelChanged += HandleSelectedClassLevelChanged;
            _view.RollAbilitiesClicked += HandleRollAbilitiesClicked;
            _view.StandardArrayClicked += HandleStandardArrayClicked;
            _view.ManualClicked += HandleManualClicked;
            _view.PointBuyClicked += HandlePointBuyClicked;
            _view.PointBuyIncrementClicked += HandlePointBuyIncrementClicked;
            _view.PointBuyDecrementClicked += HandlePointBuyDecrementClicked;
            _view.ManualScoreChanged += HandleManualScoreChanged;
            _view.ManualAbilityEntryChanged += HandleManualAbilityEntryChanged;
            _view.DragStartedFromRolledScore += HandleDragStartedFromRolledScore;
            _view.DragStartedFromAbility += HandleDragStartedFromAbility;
            _view.DropOccurred += HandleDropOccurred;
            _view.ConfirmScoresClicked += HandleConfirmScoresClicked;
            _view.CancelClicked += HandleCancelClicked;
            _view.CreateCharacterClicked += HandleCreateCharacterClicked;

            // Subscribe to model events
            Model.StateChanged += HandleModelStateChanged;

            // Start hidden
            _view.Hide();
            _view.UpdateView(Model.State);

            BindRaceClassBackgroundOptionsFromContent();
            HandleViewVisualTreeBound();
            // Touch spell index once so large spell folders load in a predictable place (lazy load on first access).
            _ = _contentQuery.GetSpells();

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
                _view.RaceAbilityScoreChoiceSelected -= HandleRaceAbilityScoreChoiceSelected;
                _view.RaceChoiceSelected -= HandleRaceChoiceSelected;
                _view.BackgroundSelected -= HandleBackgroundSelected;
                _view.SelectedClassLevelChanged -= HandleSelectedClassLevelChanged;
                _view.RollAbilitiesClicked -= HandleRollAbilitiesClicked;
                _view.StandardArrayClicked -= HandleStandardArrayClicked;
                _view.ManualClicked -= HandleManualClicked;
                _view.PointBuyClicked -= HandlePointBuyClicked;
                _view.PointBuyIncrementClicked -= HandlePointBuyIncrementClicked;
                _view.PointBuyDecrementClicked -= HandlePointBuyDecrementClicked;
                _view.ManualScoreChanged -= HandleManualScoreChanged;
                _view.ManualAbilityEntryChanged -= HandleManualAbilityEntryChanged;
                _view.DragStartedFromRolledScore -= HandleDragStartedFromRolledScore;
                _view.DragStartedFromAbility -= HandleDragStartedFromAbility;
                _view.DropOccurred -= HandleDropOccurred;
                _view.ConfirmScoresClicked -= HandleConfirmScoresClicked;
                _view.CancelClicked -= HandleCancelClicked;
                _view.CreateCharacterClicked -= HandleCreateCharacterClicked;
                _view.VisualTreeBound -= HandleViewVisualTreeBound;

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

        private void HandleViewVisualTreeBound()
        {
            if (_view == null || _view.Root == null || Model == null)
                return;

            InitializeDragAndDropHandler();
            _view.Root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _view.Root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            BindRaceClassBackgroundOptionsFromContent();
            HandleModelStateChanged(Model.State);
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

        private void BindRaceClassBackgroundOptionsFromContent()
        {
            (List<(string id, string displayName)> classes,
                List<RaceOptionData> races,
                List<(string id, string displayName)> backgrounds) =
                CharacterCreationRulesetOptionLists.CreateSortedRaceClassBackground(_contentQuery);

            _view.BindRaceClassBackgroundOptions(classes, races, backgrounds);
        }

        private void HandleClassSelected(string classId)
        {
            Model.SetSelectedClassId(classId);
        }

        private void HandleRaceSelected(string raceId)
        {
            if (_contentQuery.TryGetRace(raceId, out RaceDefinition race) && race.isGroupOnly)
                return;
            Model.SetSelectedRaceId(raceId);
        }

        private void HandleRaceAbilityScoreChoiceSelected(string choiceId, string abilityCode)
        {
            if (!IsValidRaceAbilityChoice(Model.State, choiceId, abilityCode))
                return;
            Model.SetRaceAbilityScoreChoice(choiceId, abilityCode);
        }

        private void HandleRaceChoiceSelected(string choiceId, string selectedOption)
        {
            if (!IsValidRaceChoice(Model.State, choiceId, selectedOption))
                return;
            Model.SetRaceChoice(choiceId, selectedOption);
        }

        private void HandleBackgroundSelected(string backgroundId)
        {
            Model.SetSelectedBackgroundId(backgroundId);
        }

        private void HandleSelectedClassLevelChanged(int level)
        {
            Model.SetSelectedClassLevel(level);
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
            int[][] newBreakdown = new int[6][];
            int[] newDroppedIndices = new int[6];
            for (int i = 0; i < 6; i++)
            {
                (int[] dice, int sum, int droppedIndex) = _abilityScoreRoller.Roll4d6DropLowest();
                newBreakdown[i] = dice;
                newScores[i] = sum;
                newDroppedIndices[i] = droppedIndex;
            }
            Model.SetRolledScores(newScores, isManualMode: false, diceBreakdown: newBreakdown, droppedIndices: newDroppedIndices);
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

        private void HandlePointBuyClicked()
        {
            Model.SetSelectedScoreMethod("PointBuy");
        }

        private void HandlePointBuyIncrementClicked(int abilityIndex)
        {
            if (Model.State.SelectedScoreMethod != "PointBuy" || abilityIndex < 0 || abilityIndex >= 6)
                return;
            int current = GetPointBuyScoreForAbility(abilityIndex);
            if (current >= PointBuyCostTable.MaxScore)
                return;
            Model.SetPointBuyAbilityScore(abilityIndex, current + 1);
        }

        private void HandlePointBuyDecrementClicked(int abilityIndex)
        {
            if (Model.State.SelectedScoreMethod != "PointBuy" || abilityIndex < 0 || abilityIndex >= 6)
                return;
            int current = GetPointBuyScoreForAbility(abilityIndex);
            if (current <= PointBuyCostTable.MinScore)
                return;
            Model.SetPointBuyAbilityScore(abilityIndex, current - 1);
        }

        private void HandleConfirmScoresClicked()
        {
            if (Model.State.AbilityScoresLocked)
                return;
            var scores = Model.State.AbilityScores;
            if (scores == null || scores.Length != 6)
            {
                Debug.LogWarning("CharacterCreationPresenter: Cannot confirm - ability scores not ready.");
                return;
            }
            for (int i = 0; i < 6; i++)
            {
                if (scores[i] < CharacterCreationModel.MinManualAbilityEntryScore ||
                    scores[i] > CharacterCreationModel.MaxManualAbilityEntryScore)
                {
                    Debug.LogWarning($"CharacterCreationPresenter: Cannot confirm - ability score at index {i} is {scores[i]}. All scores must be {CharacterCreationModel.MinManualAbilityEntryScore}–{CharacterCreationModel.MaxManualAbilityEntryScore}.");
                    return;
                }
            }
            Model.SetAbilityScoresLocked(true);
        }

        private static int GetPointBuyScoreForAbility(CharacterCreationState state, int abilityIndex)
        {
            if (state.AbilityScores == null || abilityIndex < 0 || abilityIndex >= state.AbilityScores.Length)
                return PointBuyCostTable.MinScore;
            int s = state.AbilityScores[abilityIndex];
            return (s >= PointBuyCostTable.MinScore && s <= PointBuyCostTable.MaxScore)
                ? s
                : PointBuyCostTable.MinScore;
        }

        private int GetPointBuyScoreForAbility(int abilityIndex)
        {
            return GetPointBuyScoreForAbility(Model.State, abilityIndex);
        }

        private static void ComputePointBuyButtonStates(CharacterCreationState state, out bool[] minusEnabled, out bool[] plusEnabled)
        {
            minusEnabled = new bool[6];
            plusEnabled = new bool[6];
            if (state.AbilityScores == null || state.AbilityScores.Length != 6)
                return;
            int pointsRemaining = PointBuyCostTable.GetPointsRemaining(state.AbilityScores);
            for (int i = 0; i < 6; i++)
            {
                int score = GetPointBuyScoreForAbility(state, i);
                int costCurrent = PointBuyCostTable.CostForScore(score);
                int costNext = PointBuyCostTable.CostForScore(score + 1);
                minusEnabled[i] = score > PointBuyCostTable.MinScore;
                plusEnabled[i] = score < PointBuyCostTable.MaxScore && (costNext - costCurrent) <= pointsRemaining;
            }
        }

        private void HandleManualScoreChanged(int index, string text)
        {
            if (index < 0 || index >= 6) return;
            int value = -1;
            if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text.Trim(), out int parsed))
                value = Mathf.Clamp(parsed, CharacterCreationModel.MinManualAbilityEntryScore, CharacterCreationModel.MaxManualAbilityEntryScore);
            Model.SetRolledScoreAt(index, value);
        }

        private void HandleManualAbilityEntryChanged(int abilityIndex, string text)
        {
            if (abilityIndex < 0 || abilityIndex >= 6) return;
            if (Model.State.SelectedScoreMethod != "Manual" || !Model.State.IsManualMode)
                return;
            int value = -1;
            if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text.Trim(), out int parsed))
                value = Mathf.Clamp(parsed, CharacterCreationModel.MinManualAbilityEntryScore, CharacterCreationModel.MaxManualAbilityEntryScore);
            Model.SetManualAbilityEntry(abilityIndex, value);
        }

        // ========== Drag and Drop Handlers ==========

        private void HandleDragStartedFromRolledScore(int rolledScoreIndex, int scoreValue)
        {
            if (Model.State.SelectedScoreMethod == "Manual" && Model.State.IsManualMode)
                return;
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
            if (Model.State.SelectedScoreMethod == "Manual" && Model.State.IsManualMode)
                return;
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
            int clsLv = CharacterCreationModel.GetClassLevel(Model.State.ClassLevels, Model.State.SelectedClassId);
            string mapDump = Model.State.ClassLevels != null
                ? string.Join(", ", Model.State.ClassLevels.Select(kv => $"{kv.Key}={kv.Value}"))
                : string.Empty;
            Debug.Log(
                $"CharacterCreationPresenter: Creating character - Total level: {Model.State.CharacterLevel}, selected class level: {clsLv}, ClassId: {Model.State.SelectedClassId}, RaceId: {Model.State.SelectedRaceId}, BackgroundId: {Model.State.SelectedBackgroundId}, class map: [{mapDump}]");

            Hide();
            // TODO: Notify MainMenuPresenter to refresh character list
        }

        private bool ValidateCharacterCreation()
        {
            if (string.IsNullOrEmpty(Model.State.SelectedClassId))
            {
                Debug.LogWarning("CharacterCreationPresenter: Class must be selected.");
                return false;
            }

            if (string.IsNullOrEmpty(Model.State.SelectedRaceId))
            {
                Debug.LogWarning("CharacterCreationPresenter: Race must be selected.");
                return false;
            }

            if (string.IsNullOrEmpty(Model.State.SelectedBackgroundId))
            {
                Debug.LogWarning("CharacterCreationPresenter: Background must be selected.");
                return false;
            }

            return true;
        }

        private void HandleModelStateChanged(CharacterCreationState state)
        {
            _view.UpdateView(state);
            if (state.SelectedScoreMethod == "PointBuy")
            {
                int pointsRemaining = PointBuyCostTable.GetPointsRemaining(state.AbilityScores);
                _view.UpdatePointBuyPointsRemaining(pointsRemaining);
                ComputePointBuyButtonStates(state, out bool[] minusEnabled, out bool[] plusEnabled);
                _view.UpdatePointBuyButtonStates(minusEnabled, plusEnabled);
            }
            UpdateDetailPanel(state);
            UpdateCharacterStats(state);
        }

        private void UpdateDetailPanel(CharacterCreationState state)
        {
            string name = string.Empty;
            string type = string.Empty;
            string description = string.Empty;
            List<FeatureData> features = null;

            if (!string.IsNullOrEmpty(state.SelectedRaceId) &&
                _contentQuery.TryGetRace(state.SelectedRaceId, out RaceDefinition race))
            {
                name = race.name ?? state.SelectedRaceId;
                type = "Race";
                AbilityModifierTextInterpolator.InterpolationResult descMeta = InterpolateRulesText(race.description, state);
                description = descMeta.Text;
                features = InterpolateFeatureDescriptions(
                    CharacterCreationDataService.GetRaceFeatures(state.SelectedRaceId), state);
                _view.UpdateDetailPanel(name, type, description, features, null, "Race features",
                    descMeta.Substituted);
                return;
            }
            else if (!string.IsNullOrEmpty(state.SelectedClassId) &&
                     _contentQuery.TryGetClass(state.SelectedClassId, out ClassDefinition cls))
            {
                name = cls.name ?? state.SelectedClassId;
                type = "Class";
                int classLevel = CharacterCreationModel.GetClassLevel(state.ClassLevels, state.SelectedClassId);
                int classLevelMax =
                    CharacterCreationModel.MaxClassLevelAllowed(state.ClassLevels, state.SelectedClassId);
                AbilityModifierTextInterpolator.InterpolationResult clsDescMeta = InterpolateRulesText(cls.description, state);
                description = clsDescMeta.Text;
                features = InterpolateFeatureDescriptions(
                    CharacterCreationClassContentBuilder.BuildFeaturesThroughLevel(cls, classLevel), state);
                List<CharacterDetailSection> sectionList = InterpolateDetailSections(
                    CharacterCreationClassContentBuilder.BuildStructuredDescription(cls), state);
                string featHeading =
                    CharacterCreationClassContentBuilder.ClassFeaturesSectionHeading(classLevel);
                _view.UpdateDetailPanel(name, type, description, features, sectionList, featHeading,
                    clsDescMeta.Substituted, true, classLevel, classLevelMax);
                return;
            }
            else if (!string.IsNullOrEmpty(state.SelectedBackgroundId) &&
                     _contentQuery.TryGetBackground(state.SelectedBackgroundId, out BackgroundDefinition bg))
            {
                name = bg.name ?? state.SelectedBackgroundId;
                type = "Background";
                AbilityModifierTextInterpolator.InterpolationResult bgDescMeta = InterpolateRulesText(bg.description, state);
                description = bgDescMeta.Text;
                features = InterpolateFeatureDescriptions(
                    CharacterCreationDataService.GetBackgroundFeatures(state.SelectedBackgroundId), state);
                _view.UpdateDetailPanel(name, type, description, features, null, "Background features",
                    bgDescMeta.Substituted);
                return;
            }

            _view.UpdateDetailPanel(name, type, description, features, null, null, false);
        }

        private AbilityModifierTextInterpolator.InterpolationResult InterpolateRulesText(
            string rawText,
            CharacterCreationState state)
        {
            return AbilityModifierTextInterpolator.InterpolateWithMeta(
                rawText ?? string.Empty, state.AbilityScores, _calculator);
        }

        private List<FeatureData> InterpolateFeatureDescriptions(
            List<FeatureData> features, CharacterCreationState state)
        {
            if (features == null || features.Count == 0)
                return features;
            var results = new List<FeatureData>(features.Count);
            foreach (FeatureData f in features)
            {
                AbilityModifierTextInterpolator.InterpolationResult meta =
                    AbilityModifierTextInterpolator.InterpolateWithMeta(
                        f.Description ?? string.Empty, state.AbilityScores, _calculator);
                results.Add(new FeatureData(f.Name, meta.Text, meta.Substituted));
            }

            return results;
        }

        private List<CharacterDetailSection> InterpolateDetailSections(
            List<CharacterDetailSection> sections, CharacterCreationState state)
        {
            if (sections == null || sections.Count == 0)
                return sections;
            var results = new List<CharacterDetailSection>(sections.Count);
            foreach (CharacterDetailSection sec in sections)
            {
                AbilityModifierTextInterpolator.InterpolationResult headMeta =
                    AbilityModifierTextInterpolator.InterpolateWithMeta(
                        sec.Heading ?? string.Empty, state.AbilityScores, _calculator);
                AbilityModifierTextInterpolator.InterpolationResult bodyMeta =
                    AbilityModifierTextInterpolator.InterpolateWithMeta(
                        sec.Body ?? string.Empty, state.AbilityScores, _calculator);
                bool hints = headMeta.Substituted || bodyMeta.Substituted;
                results.Add(new CharacterDetailSection(headMeta.Text, bodyMeta.Text, hints));
            }

            return results;
        }

        private void UpdateRaceAbilityChoiceControls(CharacterCreationState state, RaceDefinition race)
        {
            if (race?.abilityScoreChoices == null || race.abilityScoreChoices.Count == 0)
            {
                _view.BindRaceAbilityScoreChoices(null);
            }
            else
            {
                var viewModels = new List<RaceAbilityScoreChoiceViewModel>();
                foreach (AbilityScoreChoiceDefinition choice in race.abilityScoreChoices)
                {
                    if (choice == null || string.IsNullOrEmpty(choice.id))
                        continue;

                    string selected = string.Empty;
                    state.SelectedRaceAbilityChoices?.TryGetValue(choice.id, out selected);
                    string label = string.IsNullOrEmpty(choice.name)
                        ? $"+{choice.bonus} ability"
                        : $"{choice.name} (+{choice.bonus})";
                    IReadOnlyList<string> abilities = choice.abilities != null && choice.abilities.Count > 0
                        ? choice.abilities
                        : new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
                    viewModels.Add(new RaceAbilityScoreChoiceViewModel(choice.id, label, abilities, selected));
                }

                _view.BindRaceAbilityScoreChoices(viewModels);
            }

            UpdateRaceChoiceControls(state, race);
        }

        private void UpdateRaceChoiceControls(CharacterCreationState state, RaceDefinition race)
        {
            if (race?.selectableChoices == null || race.selectableChoices.Count == 0)
            {
                _view.BindRaceChoices(null);
                return;
            }

            var viewModels = new List<RaceChoiceViewModel>();
            foreach (SelectableChoiceDefinition choice in race.selectableChoices)
            {
                if (choice == null || string.IsNullOrEmpty(choice.id) ||
                    choice.options == null || choice.options.Count == 0)
                    continue;

                string selected = string.Empty;
                state.SelectedRaceChoices?.TryGetValue(choice.id, out selected);
                viewModels.Add(new RaceChoiceViewModel(
                    choice.id,
                    string.IsNullOrEmpty(choice.name) ? choice.type : choice.name,
                    choice.options,
                    selected));
            }

            _view.BindRaceChoices(viewModels);
        }

        private bool IsValidRaceAbilityChoice(CharacterCreationState state, string choiceId, string abilityCode)
        {
            if (string.IsNullOrEmpty(state.SelectedRaceId) ||
                !_contentQuery.TryGetRace(state.SelectedRaceId, out RaceDefinition race) ||
                race.abilityScoreChoices == null)
                return false;

            AbilityScoreChoiceDefinition choice = race.abilityScoreChoices
                .FirstOrDefault(c => c != null && string.Equals(c.id, choiceId, StringComparison.OrdinalIgnoreCase));
            if (choice == null || !DnD5eAbilityCodes.TryIndexFromCode(abilityCode, out _))
                return false;

            if (choice.abilities != null && choice.abilities.Count > 0 &&
                !choice.abilities.Any(a => string.Equals(a, abilityCode, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!choice.requiresUniqueAbility || state.SelectedRaceAbilityChoices == null)
                return true;

            foreach (KeyValuePair<string, string> kv in state.SelectedRaceAbilityChoices)
            {
                if (!string.Equals(kv.Key, choiceId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(kv.Value, abilityCode, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private bool IsValidRaceChoice(CharacterCreationState state, string choiceId, string selectedOption)
        {
            if (string.IsNullOrEmpty(state.SelectedRaceId) ||
                !_contentQuery.TryGetRace(state.SelectedRaceId, out RaceDefinition race) ||
                race.selectableChoices == null)
                return false;

            SelectableChoiceDefinition choice = race.selectableChoices
                .FirstOrDefault(c => c != null && string.Equals(c.id, choiceId, StringComparison.OrdinalIgnoreCase));
            if (choice?.options == null || choice.options.Count == 0)
                return false;

            return choice.options.Any(o => string.Equals(o, selectedOption, StringComparison.OrdinalIgnoreCase));
        }

        private static int[] BuildEffectiveAbilityScores(CharacterCreationState state, RaceDefinition race)
        {
            if (state.AbilityScores == null || state.AbilityScores.Length != 6)
                return state.AbilityScores;

            int[] scores = new int[6];
            Array.Copy(state.AbilityScores, scores, 6);
            if (race == null)
                return scores;

            if (race.abilityScoreBonuses != null)
            {
                foreach (AbilityScoreBonusDefinition bonus in race.abilityScoreBonuses)
                {
                    if (bonus == null || !DnD5eAbilityCodes.TryIndexFromCode(bonus.ability, out int index))
                        continue;
                    if (scores[index] >= 0)
                        scores[index] += bonus.bonus;
                }
            }

            if (race.abilityScoreChoices != null && state.SelectedRaceAbilityChoices != null)
            {
                foreach (AbilityScoreChoiceDefinition choice in race.abilityScoreChoices)
                {
                    if (choice == null || string.IsNullOrEmpty(choice.id))
                        continue;
                    if (!state.SelectedRaceAbilityChoices.TryGetValue(choice.id, out string ability) ||
                        !DnD5eAbilityCodes.TryIndexFromCode(ability, out int index))
                        continue;
                    if (scores[index] >= 0)
                        scores[index] += choice.bonus;
                }
            }

            return scores;
        }

        private void UpdateCharacterStats(CharacterCreationState state)
        {
            if (state.AbilityScores == null || state.AbilityScores.Length != 6)
                return;

            RaceDefinition race = null;
            if (!string.IsNullOrEmpty(state.SelectedRaceId))
                _contentQuery.TryGetRace(state.SelectedRaceId, out race);
            UpdateRaceAbilityChoiceControls(state, race);
            int[] effectiveScores = BuildEffectiveAbilityScores(state, race);

            for (int i = 0; i < 6; i++)
            {
                int score = effectiveScores[i];
                if (score < 0)
                {
                    _view.UpdateAbilityScoreDisplay(i, -1, 0);
                }
                else
                {
                    int modifier = _calculator.CalculateAbilityModifier(score);
                    _view.UpdateAbilityScoreDisplay(i, score, modifier);
                }
            }

            ClassDefinition classDef = null;
            if (!string.IsNullOrEmpty(state.SelectedClassId))
                _contentQuery.TryGetClass(state.SelectedClassId, out classDef);

            int totalLevel = Mathf.Clamp(state.CharacterLevel, CharacterCreationModel.MinCharacterLevel,
                CharacterCreationModel.MaxCharacterLevel);
            int classLevel = CharacterCreationModel.GetClassLevel(state.ClassLevels, state.SelectedClassId);
            classLevel = Mathf.Clamp(classLevel, CharacterCreationModel.MinCharacterLevel,
                CharacterCreationModel.MaxCharacterLevel);
            string hitDiceDisplay = CharacterCreationRulesPreview.FormatHitDicePool(classDef, classLevel);

            int? hitPoints = null;
            if (effectiveScores[2] >= 0 && classDef != null)
            {
                int conMod = _calculator.CalculateAbilityModifier(effectiveScores[2]);
                hitPoints = DnD5eDerivedStats.CalculateMaxHitPointsForLevel(classDef, conMod, classLevel);
            }

            int? armorClass = CharacterCreationRulesPreview.ComputeUnarmoredArmorClassPreview(
                classDef, effectiveScores, _calculator);
            int? raceArmorClass = CharacterCreationRulesPreview.ComputeRaceArmorClassPreview(
                race, effectiveScores, _calculator);
            if (raceArmorClass.HasValue && (!armorClass.HasValue || raceArmorClass.Value > armorClass.Value))
                armorClass = raceArmorClass;

            int? initiative = null;
            if (effectiveScores[1] >= 0)
                initiative = _calculator.CalculateAbilityModifier(effectiveScores[1]);

            int? proficiencyBonus =
                classDef != null ? _calculator.CalculateProficiencyBonus(totalLevel) : (int?)null;

            int? spellSaveDC = null;
            int? spellAttack = null;
            if (CharacterCreationRulesPreview.TryGetSpellcastingPreview(
                    classDef, effectiveScores, totalLevel, _calculator, out int dc, out int atk))
            {
                spellSaveDC = dc;
                spellAttack = atk;
            }

            _view.UpdateDerivedStats(hitPoints, armorClass, initiative, proficiencyBonus, spellSaveDC, spellAttack,
                hitDiceDisplay);

            if (race != null)
            {
                _view.UpdatePhysicalTraits(
                    race.size ?? "Medium",
                    CharacterCreationRulesPreview.FormatRaceSpeed(race),
                    CharacterCreationRulesPreview.FormatRaceSenses(race));
            }
            else
            {
                _view.UpdatePhysicalTraits("—", "—", "—");
            }

            BackgroundDefinition background = null;
            if (!string.IsNullOrEmpty(state.SelectedBackgroundId))
                _contentQuery.TryGetBackground(state.SelectedBackgroundId, out background);

            _view.UpdateProficiencyPanel(
                CharacterCreationRulesPreview.BuildProficiencySections(
                    classDef, background, race, state.SelectedRaceChoices));
        }

    }
}

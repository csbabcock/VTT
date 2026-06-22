using GameCore.DmTools;
using GameCore.Actors;
using GameCore.UI;
using GameCore;
using GameCore.UI.InGame.Services;
using GameCore.UI.InGame.Models;
using GameCore.EncounterMode.Services;
using GameCore.EncounterMode;
using GameCore.PlayerData;
using GameCore.PlayerData.Rulesets;
using GameCore.Networking;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Presenter for in-game UI following the MVP pattern.
    /// Coordinates between the model and view, handles input, and manages player input state.
    /// </summary>
    [DisallowMultipleComponent]
    public class InGameUIPresenter : MonoBehaviour, IUIPresenter<InGameUIModel, InGameUIView>
    {
        #region Serialized Fields
        [SerializeField] private InGameUIView _view;
        [Header("Input")]
        [Tooltip("PlayerInputs component to disable when character sheet is open. If not assigned, will search for it.")]
        [SerializeField] private PlayerInputs _playerInputs;
        #endregion

        #region Properties

        public InGameUIModel Model { get; private set; }
        public InGameUIView View => _view;

        /// <summary>
        /// True when the local machine is the host acting as Dungeon Master. Gates the
        /// DM player-management panel and server-authoritative HP adjustments.
        /// </summary>
        public bool IsLocalPlayerDungeonMaster => SessionRoleLocator.IsDungeonMaster;
        #endregion

        #region Private Fields
        private bool _initialized;
        private DiceRollService _diceRollService;
        private GameLogService _gameLogService;
        private InGameActionLogController _actionLog;
        private IPlayerDataService _localDataService;
        private IPlayerDataService _boundDataService;
        private IActor _inspectedActor;
        private int _selectedOwnerId = -1;
        private bool _dmToolsInitialized;
        private IEncounterSessionAuthority _encounterAuthority;
        private KeyboardNavigationService _keyboardNavigationService;
        private EncounterModeManager _encounterModeManager;
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<InGameUIView>();
            }

            // Find PlayerInputs if not assigned
            if (_playerInputs == null)
            {
                _playerInputs = FindAnyObjectByType<PlayerInputs>();
            }

            // Find EncounterModeManager
            _encounterModeManager = FindAnyObjectByType<EncounterModeManager>();

            Model = new InGameUIModel();
            
            // Initialize services
            // NOTE: Services are instantiated directly here. For a larger project, consider using
            // dependency injection (e.g., Zenject, VContainer) to improve testability and follow
            // Dependency Inversion Principle. For now, direct instantiation is acceptable as these
            // are stateless services with no external dependencies.
            _diceRollService = new DiceRollService();
            _gameLogService = new GameLogService();
            _actionLog = new InGameActionLogController(_diceRollService, _gameLogService, _view, GetActiveSheet);
            // Bind to the local player's actor when one exists so this UI follows a
            // specific participant (the foundation for per-client sheets in multiplayer).
            // Falls back to the global locator until a PlayerActor is present in the scene.
            _localDataService = ActorRegistry.LocalActor?.DataService ?? PlayerDataServiceLocator.Service;
            _keyboardNavigationService = new KeyboardNavigationService();

            BindActiveDataService();
            
            // Register the UI input gate (centralized UI blocking logic) for world-input consumers.
            if (_view != null)
            {
                UIInputGateLocator.Gate = new UIInteractionService(_view);
            }
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
                Debug.LogError("InGameUIPresenter: View reference is missing.");
                return;
            }

            _view.Initialize();
            _view.Show();

            // Validate UI input system configuration
            UIInputValidator.ValidateRuntimePanel(_view.GetComponent<IPanelComponent>());
            UIInputValidator.ValidateInputSystem();

            _view.TabClicked += OnTabClicked;
            _view.AbilityScoreClicked += OnAbilityScoreClicked;
            _view.SkillClicked += OnSkillClicked;
            _view.ActionClicked += OnActionClicked;
            _view.AttackClicked += OnAttackClicked;
            _view.FeatureClicked += OnFeatureClicked;
            _view.RestClicked += OnRestClicked;
            _view.ClearLogClicked += OnClearLogClicked;
            _view.LogEntryDeleteClicked += OnLogEntryDeleteClicked;
            _view.MoveButtonClicked += OnMoveButtonClicked;
            _view.CombatHitPointsAdjusted += OnCombatHitPointsAdjusted;
            _view.CombatTemporaryHitPointsAdjusted += OnCombatTemporaryHitPointsAdjusted;
            _view.CombatDeathSavesChanged += OnCombatDeathSavesChanged;
            _view.CombatDeathSavesReset += OnCombatDeathSavesReset;
            _view.CombatConditionToggled += OnCombatConditionToggled;
            _view.CombatInspirationChanged += OnCombatInspirationChanged;
            _view.CombatExhaustionAdjusted += OnCombatExhaustionAdjusted;
            _view.VisualTreeBound += OnViewVisualTreeBound;
            Model.StateChanged += OnModelStateChanged;

            // Push initial state to the view so it starts in sync with the model.
            // This will also configure input properly (UI starts closed, so input should be enabled)
            if (!IsLocalPlayerDungeonMaster)
            {
                _view.UpdateView(Model.State);
            }

            // PanelRenderer may attach the visual tree one frame after Initialize() when the reload callback fires.
            var initialSheet = GetActiveSheet();
            if (_view.Root != null && !IsLocalPlayerDungeonMaster)
            {
                UpdateCharacterSheetUI(initialSheet);
            }
            else if (_view.Root == null && !IsLocalPlayerDungeonMaster)
            {
                StartCoroutine(CoApplyInitialCharacterSheetWhenRootReady(initialSheet));
            }
            
            // Explicitly ensure input is enabled on startup (character sheet starts closed)
            if (_playerInputs != null && !IsLocalPlayerDungeonMaster)
            {
                _playerInputs.SetInputEnabled(true);
            }

            Debug.Log($"InGameUIPresenter: Local role = {(IsLocalPlayerDungeonMaster ? "Dungeon Master" : "Player")}.");

            SetupDmToolsIfNeeded();
            if (IsLocalPlayerDungeonMaster)
            {
                ApplyDmUiMode();
            }
            BindEncounterAuthorityIfNeeded();
            RefreshDmPanelUi();
            RefreshEncounterTurnUi();

            _initialized = true;
        }

        private IEnumerator CoApplyInitialCharacterSheetWhenRootReady(ICharacterSheet initialSheet)
        {
            int waited = 0;
            while (_view != null && _view.Root == null && waited < 120)
            {
                waited++;
                yield return null;
            }

            if (_view == null || !_initialized)
            {
                yield break;
            }

            UpdateCharacterSheetUI(initialSheet);
            _view.UpdateView(Model.State);
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            if (_view != null)
            {
                _view.TabClicked -= OnTabClicked;
                _view.AbilityScoreClicked -= OnAbilityScoreClicked;
                _view.SkillClicked -= OnSkillClicked;
                _view.ActionClicked -= OnActionClicked;
                _view.AttackClicked -= OnAttackClicked;
                _view.FeatureClicked -= OnFeatureClicked;
                _view.RestClicked -= OnRestClicked;
                _view.ClearLogClicked -= OnClearLogClicked;
                _view.LogEntryDeleteClicked -= OnLogEntryDeleteClicked;
                _view.MoveButtonClicked -= OnMoveButtonClicked;
                _view.CombatHitPointsAdjusted -= OnCombatHitPointsAdjusted;
                _view.CombatTemporaryHitPointsAdjusted -= OnCombatTemporaryHitPointsAdjusted;
                _view.CombatDeathSavesChanged -= OnCombatDeathSavesChanged;
                _view.CombatDeathSavesReset -= OnCombatDeathSavesReset;
                _view.CombatConditionToggled -= OnCombatConditionToggled;
                _view.CombatInspirationChanged -= OnCombatInspirationChanged;
                _view.CombatExhaustionAdjusted -= OnCombatExhaustionAdjusted;
                _view.VisualTreeBound -= OnViewVisualTreeBound;
            }

            if (Model != null)
            {
                Model.StateChanged -= OnModelStateChanged;
            }

            TeardownDmTools();
            UnbindEncounterAuthority();
            UnbindActiveDataService();

            _initialized = false;
        }
        #endregion

        #region DM Tools

        private void OnViewVisualTreeBound()
        {
            SetupDmToolsIfNeeded();
            if (IsLocalPlayerDungeonMaster)
            {
                ApplyDmUiMode();
            }
            RefreshDmPanelUi();
            RefreshEncounterTurnUi();
        }

        private void SetupDmToolsIfNeeded()
        {
            if (!IsLocalPlayerDungeonMaster || _dmToolsInitialized || _view == null)
                return;

            var dmPanel = _view.DmPanel;
            dmPanel.PlayerSelected += OnDmPlayerSelected;
            dmPanel.StartEncounterClicked += OnDmStartEncounterClicked;
            dmPanel.EndEncounterClicked += OnDmEndEncounterClicked;
            dmPanel.NextTurnClicked += OnDmNextTurnClicked;

            ActorRegistry.ActorRegistered += OnActorRegistryChanged;
            ActorRegistry.ActorUnregistered += OnActorUnregistered;
            ActorRegistry.ActorUpdated += OnActorUpdated;
            dmPanel.SetVisible(true);
            DmToolsBootstrap.EnsureForLocalSession();
            _dmToolsInitialized = true;
        }

        private void ApplyDmUiMode()
        {
            if (_view == null)
                return;

            Model.SetCharacterSheetOpen(false);
            _view.SetPlayerHudVisible(false);
            _view.SetDmHudMode(true);
            ApplyDmCursorState();

            if (_playerInputs != null)
                _playerInputs.SetInputEnabled(false);
        }

        private static void ApplyDmCursorState()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void TeardownDmTools()
        {
            if (!_dmToolsInitialized || _view == null)
                return;

            var dmPanel = _view.DmPanel;
            dmPanel.PlayerSelected -= OnDmPlayerSelected;
            dmPanel.StartEncounterClicked -= OnDmStartEncounterClicked;
            dmPanel.EndEncounterClicked -= OnDmEndEncounterClicked;
            dmPanel.NextTurnClicked -= OnDmNextTurnClicked;

            ActorRegistry.ActorRegistered -= OnActorRegistryChanged;
            ActorRegistry.ActorUnregistered -= OnActorUnregistered;
            ActorRegistry.ActorUpdated -= OnActorUpdated;
            dmPanel.SetVisible(false);
            _view.SetDmHudMode(false);
            _view.SetPlayerHudVisible(true);
            _dmToolsInitialized = false;
        }

        private void OnActorRegistryChanged(IActor actor) => RefreshDmPanelUi();

        private void OnActorUpdated(IActor actor)
        {
            RefreshDmPanelUi();

            if (_inspectedActor != null && _inspectedActor.OwnerId == actor.OwnerId)
            {
                BindActiveDataService();
                RefreshInspectedCharacterSheetUi();
            }
        }

        private void OnActorUnregistered(IActor actor)
        {
            if (ReferenceEquals(_inspectedActor, actor))
                SetInspectedActor(null);
            RefreshDmPanelUi();
        }

        private void OnDmPlayerSelected(int ownerId)
        {
            if (_selectedOwnerId == ownerId)
                SetInspectedActor(null);
            else
                SetInspectedActor(ActorRegistry.GetByOwner(ownerId));
        }

        private void OnCombatHitPointsAdjusted(int delta)
        {
            var actor = GetCombatTargetActor();
            CharacterSheetAuthorityHelper.TryGetMutableAuthority(actor)?.RequestAdjustCurrentHitPoints(delta);
            RefreshCharacterSheetAfterCombatEdit();
        }

        private void OnCombatTemporaryHitPointsAdjusted(int delta)
        {
            var actor = GetCombatTargetActor();
            var authority = CharacterSheetAuthorityHelper.TryGetMutableAuthority(actor);
            if (authority == null)
                return;

            authority.RequestSetTemporaryHitPoints(authority.TemporaryHitPoints + delta);
            RefreshCharacterSheetAfterCombatEdit();
        }

        private void OnCombatDeathSavesChanged(int successes, int failures)
        {
            CharacterSheetAuthorityHelper.TryGetMutableAuthority(GetCombatTargetActor())
                ?.RequestSetDeathSaves(successes, failures);
            RefreshCharacterSheetAfterCombatEdit();
        }

        private void OnCombatDeathSavesReset()
        {
            CharacterSheetAuthorityHelper.TryGetMutableAuthority(GetCombatTargetActor())?.RequestResetDeathSaves();
            RefreshCharacterSheetAfterCombatEdit();
        }

        private void OnCombatConditionToggled(string conditionId)
        {
            CharacterSheetAuthorityHelper.TryGetMutableAuthority(GetCombatTargetActor())
                ?.RequestToggleCondition(conditionId);
            RefreshCharacterSheetAfterCombatEdit();
        }

        private void OnCombatInspirationChanged(bool hasInspiration)
        {
            CharacterSheetAuthorityHelper.TryGetMutableAuthority(GetCombatTargetActor())
                ?.RequestSetInspiration(hasInspiration);
            RefreshCharacterSheetAfterCombatEdit();
        }

        private void OnCombatExhaustionAdjusted(int delta)
        {
            var authority = CharacterSheetAuthorityHelper.TryGetMutableAuthority(GetCombatTargetActor());
            if (authority == null)
                return;

            authority.RequestSetExhaustionLevel(authority.ExhaustionLevel + delta);
            RefreshCharacterSheetAfterCombatEdit();
        }

        private IActor GetCombatTargetActor()
        {
            if (IsLocalPlayerDungeonMaster)
                return _inspectedActor;

            return ActorRegistry.LocalActor;
        }

        private void RefreshCharacterSheetAfterCombatEdit()
        {
            RefreshInspectedCharacterSheetUi();
            if (IsLocalPlayerDungeonMaster)
                RefreshDmPanelUi();
        }

        private void OnDmStartEncounterClicked()
        {
            EncounterSessionLocator.Authority?.RequestStartTurnOrder();
        }

        private void OnDmEndEncounterClicked()
        {
            var authority = EncounterSessionLocator.Authority;
            if (authority != null && authority.IsEncounterActive)
                authority.RequestToggleEncounter();
        }

        private void OnDmNextTurnClicked()
        {
            EncounterSessionLocator.Authority?.RequestEndTurn();
        }

        private void BindEncounterAuthorityIfNeeded()
        {
            var authority = EncounterSessionLocator.Authority;
            if (ReferenceEquals(authority, _encounterAuthority))
                return;

            UnbindEncounterAuthority();
            _encounterAuthority = authority;
            if (_encounterAuthority == null)
            {
                RefreshEncounterTurnUi();
                return;
            }

            _encounterAuthority.EncounterActiveChanged += OnEncounterActiveChanged;
            _encounterAuthority.TurnOwnerChanged += OnTurnOwnerChanged;
            RefreshEncounterTurnUi();
        }

        private void UnbindEncounterAuthority()
        {
            if (_encounterAuthority == null)
                return;

            _encounterAuthority.EncounterActiveChanged -= OnEncounterActiveChanged;
            _encounterAuthority.TurnOwnerChanged -= OnTurnOwnerChanged;
            _encounterAuthority = null;
        }

        private void OnEncounterActiveChanged(bool isActive)
        {
            RefreshEncounterTurnUi();
        }

        private void OnTurnOwnerChanged(int ownerId)
        {
            RefreshEncounterTurnUi();
            if (_encounterModeManager != null && !_encounterModeManager.IsLocalTurnActive)
                _encounterModeManager.DisableMovementMode();
        }

        private void RefreshEncounterTurnUi()
        {
            if (_view == null)
                return;

            var authority = EncounterSessionLocator.Authority;
            if (authority == null || !authority.IsEncounterActive)
            {
                _view.UpdateEncounterTurnIndicator(null, false);
                return;
            }

            if (!authority.HasActiveTurnOrder)
            {
                _view.UpdateEncounterTurnIndicator("Encounter active", true);
                return;
            }

            var actor = ActorRegistry.GetByOwner(authority.CurrentTurnOwnerId);
            string turnName = actor != null ? actor.DisplayName : $"Player {authority.CurrentTurnOwnerId}";
            bool isLocalTurn = _encounterModeManager != null && _encounterModeManager.IsLocalTurnActive;
            string suffix = isLocalTurn ? " (your turn)" : string.Empty;
            _view.UpdateEncounterTurnIndicator($"{turnName}'s turn{suffix}", true);
        }

        private void SetInspectedActor(IActor actor)
        {
            _inspectedActor = actor;
            _selectedOwnerId = actor?.OwnerId ?? -1;
            BindActiveDataService();
            RefreshDmPanelUi();

            if (actor != null)
            {
                RefreshInspectedCharacterSheetUi();
                Model.SetCharacterSheetOpen(true);
                _view.SetMoveButtonVisible(false);
            }
            else
            {
                Model.SetCharacterSheetOpen(false);
            }
        }

        private void RefreshInspectedCharacterSheetUi()
        {
            if (!IsLocalPlayerDungeonMaster || _inspectedActor == null)
                return;

            UpdateCharacterSheetUI(GetActiveSheet());
        }

        private void RefreshDmPanelUi()
        {
            if (!IsLocalPlayerDungeonMaster || _view == null)
                return;

            if (!_dmToolsInitialized)
                SetupDmToolsIfNeeded();

            _view.DmPanel.SetVisible(true);
            _view.DmPanel.RefreshPlayerList(BuildDmPlayerRows());
        }

        private List<DmPlayerRowState> BuildDmPlayerRows()
        {
            var rows = new List<DmPlayerRowState>();
            var actors = ActorRegistry.Actors;
            for (int i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor == null)
                    continue;

                rows.Add(new DmPlayerRowState
                {
                    OwnerId = actor.OwnerId,
                    DisplayName = actor.DisplayName,
                    IsSelected = actor.OwnerId == _selectedOwnerId,
                });
            }

            return rows;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!IsLocalPlayerDungeonMaster || !hasFocus)
                return;

            ApplyDmCursorState();
            EnsureDmHudInteractive();
        }

        private void EnsureDmHudInteractive()
        {
            if (_view == null)
                return;

            _view.Show();
            _view.SetDmHudMode(true);
            _view.DmPanel.SetVisible(true);
        }

        private IPlayerDataService GetActiveDataService()
        {
            if (_inspectedActor?.DataService != null)
                return _inspectedActor.DataService;

            return _localDataService ?? PlayerDataServiceLocator.Service;
        }

        private ICharacterSheet GetActiveSheet() => GetActiveDataService()?.GetCharacterSheet();

        private void BindActiveDataService()
        {
            var next = GetActiveDataService();
            if (ReferenceEquals(_boundDataService, next))
                return;

            UnbindActiveDataService();
            _boundDataService = next;
            if (_boundDataService != null)
                _boundDataService.CharacterSheetChanged += OnCharacterSheetChanged;
        }

        private void UnbindActiveDataService()
        {
            if (_boundDataService == null)
                return;

            _boundDataService.CharacterSheetChanged -= OnCharacterSheetChanged;
            _boundDataService = null;
        }

        #endregion

        #region Input Handling
        private void Update()
        {
            if (!_initialized)
                return;

            BindEncounterAuthorityIfNeeded();

            if (IsLocalPlayerDungeonMaster)
            {
                ApplyDmCursorState();
#if ENABLE_INPUT_SYSTEM
                HandleDmKeyboardInput();
#endif
                return;
            }

            UpdateLookInputState();

#if ENABLE_INPUT_SYSTEM
            HandleMouseMovement();
            HandleKeyboardInput();
#endif
        }

        private void UpdateLookInputState()
        {
            if (_playerInputs == null)
                return;

            if (IsEncounterModeActive())
            {
                _playerInputs.cursorInputForLook = false;
                return;
            }

            if (Model != null && Model.IsCharacterSheetOpen)
            {
                _playerInputs.cursorInputForLook = !UIInputGateLocator.ShouldBlockInput();
                return;
            }

            _playerInputs.cursorInputForLook = true;
        }

        private bool IsEncounterModeActive()
        {
            if (_encounterModeManager == null)
            {
                _encounterModeManager = FindAnyObjectByType<EncounterModeManager>();
            }

            return _encounterModeManager != null && _encounterModeManager.IsEncounterModeActive;
        }

#if ENABLE_INPUT_SYSTEM
        private void HandleDmKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _inspectedActor == null)
                return;

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                Model.ToggleCharacterSheet();
                if (!Model.IsCharacterSheetOpen && _keyboardNavigationService != null)
                    _keyboardNavigationService.Reset();
            }
        }

        /// <summary>
        /// Handles mouse movement to clear keyboard selection when mouse is used.
        /// Delegates to KeyboardNavigationService to follow Single Responsibility Principle.
        /// </summary>
        private void HandleMouseMovement()
        {
            if (!Model.IsCharacterSheetOpen || _view == null || _keyboardNavigationService == null)
                return;

            bool isMouseOverUI = _view.IsMouseOverCharacterSheet();
            bool selectionCleared = _keyboardNavigationService.HandleMouseMovement(
                Model.IsCharacterSheetOpen, 
                isMouseOverUI
            );

            if (selectionCleared)
            {
                _view.ClearButtonSelection(Model.State.CharacterSheetTabIndex);
            }
        }

        /// <summary>
        /// Handles keyboard input for UI navigation.
        /// </summary>
        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                Model.ToggleCharacterSheet();
                // Reset button selection when closing
                if (!Model.IsCharacterSheetOpen && _keyboardNavigationService != null)
                {
                    _keyboardNavigationService.Reset();
                }
                return;
            }

            // WASD and Arrow key navigation when character sheet is open
            if (Model.IsCharacterSheetOpen && _view != null && _keyboardNavigationService != null)
            {
                // Tab navigation: A/Left Arrow = previous tab, D/Right Arrow = next tab
                bool navigateLeft = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
                bool navigateRight = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
                
                if (navigateRight)
                {
                    Model.NextTab();
                    _keyboardNavigationService.Reset();
                    _view.ClearButtonSelection(Model.State.CharacterSheetTabIndex);
                }
                else if (navigateLeft)
                {
                    Model.PreviousTab();
                    _keyboardNavigationService.Reset();
                    _view.ClearButtonSelection(Model.State.CharacterSheetTabIndex);
                }

                // Button navigation: W/Up Arrow = previous button, S/Down Arrow = next button
                bool navigateUp = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
                bool navigateDown = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
                
                // If keyboard navigation is starting and no button is currently selected,
                // check if mouse is hovering over a button and start from there
                if ((navigateUp || navigateDown) && !_keyboardNavigationService.HasSelection)
                {
                    int hoveredIndex = _view.GetHoveredButtonIndex(Model.State.CharacterSheetTabIndex);
                    if (hoveredIndex >= 0)
                    {
                        _keyboardNavigationService.SetSelectedButtonIndex(hoveredIndex);
                        // Update view immediately to show the hovered button as selected
                        _view.SetSelectedButtonIndex(Model.State.CharacterSheetTabIndex, hoveredIndex);
                    }
                }
                
                // Delegate button navigation to service
                var buttons = _view.GetButtonsInTab(Model.State.CharacterSheetTabIndex);
                int newSelectedIndex = _keyboardNavigationService.HandleButtonNavigation(
                    buttons.Count, 
                    navigateUp, 
                    navigateDown
                );

                // Update view if selection changed
                if (newSelectedIndex >= 0)
                {
                    _view.SetSelectedButtonIndex(Model.State.CharacterSheetTabIndex, newSelectedIndex);
                }

                // Enter key: Activate selected button
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    int selectedIndex = _keyboardNavigationService.SelectedButtonIndex;
                    if (selectedIndex >= 0)
                    {
                        _view.ActivateSelectedButton(Model.State.CharacterSheetTabIndex, selectedIndex);
                    }
                }
            }
            else
            {
                // Reset key state when character sheet is closed
                if (_keyboardNavigationService != null)
                {
                    _keyboardNavigationService.ResetKeyState();
                }
            }
        }
#endif
        #endregion

        #region Model Event Handlers
        private void OnModelStateChanged(InGameUIState state)
        {
            if (IsLocalPlayerDungeonMaster)
            {
                _view.UpdateView(state);
                if (state.IsCharacterSheetOpen)
                    StartCoroutine(RefreshUIAfterDelay(0.1f));
                ApplyDmCursorState();
                UpdateCursorState(state.IsCharacterSheetOpen);
                return;
            }

            _view.UpdateView(state);
            
            // Refresh UI data when character sheet opens
            if (state.IsCharacterSheetOpen)
            {
                // Small delay to ensure UI is fully visible before updating
                StartCoroutine(RefreshUIAfterDelay(0.1f));
            }
            
            // Keep the action map enabled for UI Toolkit; look input is filtered separately.
            UpdatePlayerInput(state.IsCharacterSheetOpen);
            UpdateCursorState(state.IsCharacterSheetOpen);
            
            // Restore cursor input for look when character sheet closes outside encounter mode.
            if (!state.IsCharacterSheetOpen && _playerInputs != null && !IsEncounterModeActive())
            {
                _playerInputs.cursorInputForLook = true;
            }
        }

        /// <summary>
        /// Refreshes the UI after a short delay to ensure elements are visible.
        /// </summary>
        private System.Collections.IEnumerator RefreshUIAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            var sheet = GetActiveSheet();
            UpdateCharacterSheetUI(sheet);
        }
        #endregion

        #region UI State Management
        /// <summary>
        /// Updates cursor lock state and visibility based on character sheet state.
        /// Shows cursor when sheet opens, hides it when sheet closes.
        /// Uses Confined mode when open so UI and tactical controls remain clickable.
        /// </summary>
        private void UpdateCursorState(bool isCharacterSheetOpen)
        {
            if (IsLocalPlayerDungeonMaster)
            {
                ApplyDmCursorState();
                return;
            }

            if (isCharacterSheetOpen || IsEncounterModeActive())
            {
                // Encounter mode keeps the cursor available for UI/tactical interaction.
                UnityEngine.Cursor.lockState = CursorLockMode.Confined;
                UnityEngine.Cursor.visible = true;
            }
            else
            {
                // Hide cursor when character sheet closes
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        /// <summary>
        /// Updates player input enabled state based on UI visibility.
        /// Look input is filtered separately so encounter mode can keep UI input without camera control.
        /// Movement input is handled by PlayerController which checks if character sheet is open.
        /// </summary>
        private void UpdatePlayerInput(bool isUIOpen)
        {
            // The networked player spawns after this presenter's Awake, so resolve the
            // PlayerInputs lazily (preferring the local player's actor) and retry on later
            // state changes instead of warning.
            if (_playerInputs == null)
            {
                _playerInputs = ResolvePlayerInputs();
            }

            if (_playerInputs == null)
            {
                return;
            }

            // Keep input enabled so camera control and UI input still work
            // PlayerController will check if character sheet is open and conditionally use movement input
            // This follows SOLID principles - input system remains unchanged, PlayerController decides usage
            _playerInputs.SetInputEnabled(true);
        }

        /// <summary>
        /// Finds the local player's PlayerInputs. Prefers the local actor's GameObject so we
        /// never bind to a remote player's (disabled) input component.
        /// </summary>
        private PlayerInputs ResolvePlayerInputs()
        {
            var localActor = Actors.ActorRegistry.LocalActor;
            if (localActor?.Transform != null)
            {
                var actorInputs = localActor.Transform.GetComponent<PlayerInputs>();
                if (actorInputs != null)
                {
                    return actorInputs;
                }
            }

            return FindAnyObjectByType<PlayerInputs>();
        }
        #endregion

        #region Player Data Event Handlers

        /// <summary>
        /// Called when the character sheet changes. Updates UI to reflect new data.
        /// </summary>
        private void OnCharacterSheetChanged(ICharacterSheet sheet)
        {
            if (IsLocalPlayerDungeonMaster)
            {
                if (_inspectedActor != null)
                    UpdateCharacterSheetUI(sheet);
                RefreshDmPanelUi();
                return;
            }

            UpdateCharacterSheetUI(sheet);
        }

        /// <summary>
        /// Updates the character sheet UI from the current character sheet.
        /// </summary>
        private void UpdateCharacterSheetUI(ICharacterSheet sheet)
        {
            if (_view == null || sheet == null)
            {
                Debug.LogWarning("InGameUIPresenter: Cannot update UI - view or sheet is null");
                return;
            }

            var root = _view.Root;
            if (root == null)
            {
                Debug.LogWarning("InGameUIPresenter: Cannot update UI - root element is null");
                return;
            }

            CharacterSheetUIUpdater.UpdateCharacterSheet(root, sheet, sheet.RulesetId);

            var targetActor = IsLocalPlayerDungeonMaster ? _inspectedActor : ActorRegistry.LocalActor;
            if (targetActor != null)
            {
                var combat = CharacterSheetAuthorityHelper.GetCombatState(targetActor);
                _view.BindCombatSection(combat, CharacterSheetAuthorityHelper.GetMaxHitPoints(targetActor));
            }

            _view.SetMoveButtonVisible(!IsLocalPlayerDungeonMaster);

            if (_inspectedActor == null
                && _encounterModeManager != null
                && _encounterModeManager.IsEncounterModeActive)
            {
                _encounterModeManager.RefreshMovementDisplay();
            }
        }

        #endregion

        #region View Event Handlers
        private void OnTabClicked(int tabIndex)
        {
            Model.SetTab(tabIndex);
            UpdateCharacterSheetUI(GetActiveSheet());
        }

        private void OnAbilityScoreClicked(string abilityName) => _actionLog.RollAbilityCheck(abilityName);

        private void OnSkillClicked(string skillName) => _actionLog.RollSkillCheck(skillName);

        private void OnActionClicked(string actionName)
        {
            _actionLog.LogAction(actionName);

            // Handle Dash action - double movement speed
            if (actionName == "Dash" && _encounterModeManager != null)
            {
                if (!_encounterModeManager.IsLocalTurnActive)
                    return;

                _encounterModeManager.SetDashActive(true);
            }
        }

        private void OnAttackClicked(string weaponName) => _actionLog.RollAttack(weaponName);

        private void OnFeatureClicked(string featureName) => _actionLog.LogFeature(featureName);

        private void OnRestClicked(string restType) => _actionLog.LogRest(restType);

        private void OnClearLogClicked() => _actionLog.ClearLog();

        private void OnLogEntryDeleteClicked(UnityEngine.UIElements.VisualElement entryCard)
            => _actionLog.DeleteLogEntry(entryCard);

        private void OnMoveButtonClicked()
        {
            if (_encounterModeManager == null || !_encounterModeManager.IsEncounterModeActive)
                return;

            if (!_encounterModeManager.IsLocalTurnActive)
                return;

            // Toggle movement mode: if active, disable it; if not active, enable it
            if (_encounterModeManager.IsMovementModeActive)
            {
                _encounterModeManager.DisableMovementMode();
            }
            else
            {
                _encounterModeManager.EnableGridSelection();
            }
        }
        #endregion
    }
}


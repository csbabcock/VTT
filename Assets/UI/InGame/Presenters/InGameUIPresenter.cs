using GameCore.UI;
using GameCore;
using GameCore.UI.InGame.Services;
using GameCore.UI.InGame.Models;
using GameCore.EncounterMode.Services;
using GameCore.EncounterMode;
using GameCore.PlayerData;
using GameCore.PlayerData.Rulesets;
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
        #endregion

        #region Private Fields
        private bool _initialized;
        private DiceRollService _diceRollService;
        private GameLogService _gameLogService;
        private IPlayerDataService _playerDataService;
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
            _playerDataService = PlayerDataServiceLocator.Service;
            _keyboardNavigationService = new KeyboardNavigationService();
            
            // Subscribe to player data changes
            _playerDataService.PlayerDataChanged += OnPlayerDataChanged;
            
            // Initialize UI interaction service (centralized UI blocking logic)
            if (_view != null)
            {
                UIInteractionService.Instance.Initialize(_view);
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
            Model.StateChanged += OnModelStateChanged;

            // Push initial state to the view so it starts in sync with the model.
            // This will also configure input properly (UI starts closed, so input should be enabled)
            _view.UpdateView(Model.State);

            // PanelRenderer may attach the visual tree one frame after Initialize() when the reload callback fires.
            var initialData = _playerDataService.GetPlayerData();
            if (_view.Root != null)
            {
                UpdateCharacterSheetUI(initialData);
            }
            else
            {
                StartCoroutine(CoApplyInitialCharacterSheetWhenRootReady(initialData));
            }
            
            // Explicitly ensure input is enabled on startup (character sheet starts closed)
            if (_playerInputs != null)
            {
                _playerInputs.SetInputEnabled(true);
            }

            _initialized = true;
        }

        private IEnumerator CoApplyInitialCharacterSheetWhenRootReady(CharacterData initialData)
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

            UpdateCharacterSheetUI(initialData);
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
            }

            if (Model != null)
            {
                Model.StateChanged -= OnModelStateChanged;
            }

            // Unsubscribe from player data service
            if (_playerDataService != null)
            {
                _playerDataService.PlayerDataChanged -= OnPlayerDataChanged;
            }

            _initialized = false;
        }
        #endregion

        #region Input Handling
        private void Update()
        {
            if (!_initialized)
                return;

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
                _playerInputs.cursorInputForLook = !UIInteractionService.Instance.ShouldBlockCameraInput();
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
            
            var data = _playerDataService.GetPlayerData();
            UpdateCharacterSheetUI(data);
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
            if (_playerInputs == null)
            {
                Debug.LogWarning("InGameUIPresenter: PlayerInputs is null! Input may not be disabled when UI is open.");
                return;
            }

            // Keep input enabled so camera control and UI input still work
            // PlayerController will check if character sheet is open and conditionally use movement input
            // This follows SOLID principles - input system remains unchanged, PlayerController decides usage
            _playerInputs.SetInputEnabled(true);
        }
        #endregion

        #region Player Data Event Handlers

        /// <summary>
        /// Called when player data changes. Updates UI to reflect new data.
        /// </summary>
        private void OnPlayerDataChanged(CharacterData data)
        {
            UpdateCharacterSheetUI(data);
        }

        /// <summary>
        /// Updates the character sheet UI with current character data.
        /// </summary>
        private void UpdateCharacterSheetUI(CharacterData data)
        {
            if (_view == null || data == null)
            {
                Debug.LogWarning("InGameUIPresenter: Cannot update UI - view or data is null");
                return;
            }

            var root = _view.Root;
            if (root == null)
            {
                Debug.LogWarning("InGameUIPresenter: Cannot update UI - root element is null");
                return;
            }

            Debug.Log($"InGameUIPresenter: Updating character sheet UI for {data.CharacterName}");

            // Use refactored updater with ruleset-agnostic architecture
            // Detect ruleset from service (defaults to DnD5e)
            string rulesetId = "DnD5e";
            if (_playerDataService is JsonPlayerDataService)
            {
                rulesetId = "DnD5e"; // Could be detected from JSON metadata in future
            }

            CharacterSheetUIUpdater.UpdateCharacterSheet(root, data, rulesetId);
        }

        #endregion

        #region View Event Handlers
        private void OnTabClicked(int tabIndex)
        {
            Model.SetTab(tabIndex);
            
            // Refresh UI when switching tabs to ensure all data is up to date
            // This is especially important if buttons are in different tabs
            var data = _playerDataService.GetPlayerData();
            UpdateCharacterSheetUI(data);
        }

        private void OnAbilityScoreClicked(string abilityName)
        {
            var characterData = _playerDataService.GetPlayerData();
            int modifier = characterData.GetAbilityModifier(abilityName);
            var rollResult = _diceRollService.RollD20Check(
                characterData.CharacterName,
                $"{abilityName} Check",
                modifier,
                new List<ModifierBreakdown>
                {
                    new ModifierBreakdown { Source = abilityName, Value = modifier }
                }
            );

            var formatted = _gameLogService.FormatRollResult(rollResult);
            _view.AddLogEntry(formatted);
        }

        private void OnSkillClicked(string skillName)
        {
            var characterData = _playerDataService.GetPlayerData();
            string abilityName = CharacterData.GetSkillAbility(skillName);
            int modifier = characterData.GetSkillModifier(skillName, abilityName);
            bool isProficient = characterData.ProficientSkills.Contains(skillName);

            var breakdowns = new List<ModifierBreakdown>
            {
                new ModifierBreakdown { Source = abilityName, Value = characterData.GetAbilityModifier(abilityName) }
            };

            if (isProficient)
            {
                breakdowns.Add(new ModifierBreakdown 
                { 
                    Source = "Proficiency", 
                    Value = characterData.ProficiencyBonus 
                });
            }

            var rollResult = _diceRollService.RollD20Check(
                characterData.CharacterName,
                skillName,
                modifier,
                breakdowns
            );

            var formatted = _gameLogService.FormatRollResult(rollResult);
            _view.AddLogEntry(formatted);
        }

        private void OnActionClicked(string actionName)
        {
            var characterData = _playerDataService.GetPlayerData();
            // Log the action (non-dice actions)
            var formatted = _gameLogService.FormatAction(characterData.CharacterName, actionName);
            _view.AddLogEntry(formatted);

            // Handle Dash action - double movement speed
            if (actionName == "Dash" && _encounterModeManager != null)
            {
                _encounterModeManager.SetDashActive(true);
            }
        }

        private void OnAttackClicked(string weaponName)
        {
            // Get character name and weapon data using ruleset system
            string characterName;
            WeaponData weaponData;
            
            // Try to get D&D 5e character data for proper calculations
            if (_playerDataService is JsonPlayerDataService jsonService)
            {
                var dnD5eData = jsonService.GetDnD5eCharacterData();
                if (dnD5eData != null)
                {
                    // Use proper 5e weapon calculator through ruleset system
                    characterName = dnD5eData.characterName;
                    var calculator = RulesetCalculatorFactory.GetDefaultCalculator();
                    var adapter = RulesetAdapterFactory.GetDefaultAdapter();
                    weaponData = adapter.GetWeaponData(weaponName, dnD5eData, calculator);
                }
                else
                {
                    // Fallback: Use ruleset system with legacy CharacterData
                    var characterData = _playerDataService.GetPlayerData();
                    characterName = characterData.CharacterName;
                    // Convert to DnD5eCharacterData for ruleset system
                    var dnD5eFallback = new DnD5eCharacterData
                    {
                        characterName = characterData.CharacterName,
                        strength = characterData.Strength,
                        dexterity = characterData.Dexterity,
                        constitution = characterData.Constitution,
                        intelligence = characterData.Intelligence,
                        wisdom = characterData.Wisdom,
                        charisma = characterData.Charisma,
                        level = 1, // Default level
                        proficientWeapons = new List<string> { "Simple", "Martial" } // Assume basic proficiency
                    };
                    var calculator = RulesetCalculatorFactory.GetDefaultCalculator();
                    var adapter = RulesetAdapterFactory.GetDefaultAdapter();
                    weaponData = adapter.GetWeaponData(weaponName, dnD5eFallback, calculator);
                }
            }
            else
            {
                // Legacy service - convert to DnD5eCharacterData for ruleset system
                var characterData = _playerDataService.GetPlayerData();
                characterName = characterData.CharacterName;
                var dnD5eFallback = new DnD5eCharacterData
                {
                    characterName = characterData.CharacterName,
                    strength = characterData.Strength,
                    dexterity = characterData.Dexterity,
                    constitution = characterData.Constitution,
                    intelligence = characterData.Intelligence,
                    wisdom = characterData.Wisdom,
                    charisma = characterData.Charisma,
                    level = 1, // Default level
                    proficientWeapons = new List<string> { "Simple", "Martial" } // Assume basic proficiency
                };
                var calculator = RulesetCalculatorFactory.GetDefaultCalculator();
                var adapter = RulesetAdapterFactory.GetDefaultAdapter();
                weaponData = adapter.GetWeaponData(weaponName, dnD5eFallback, calculator);
            }

            var (attackRoll, damageRoll) = _diceRollService.RollAttack(
                characterName,
                weaponData.WeaponName,
                weaponData.AttackBonus,
                weaponData.DamageDice,
                weaponData.DamageDieType,
                weaponData.DamageModifier
            );

            var formatted = _gameLogService.FormatAttackRoll(attackRoll, damageRoll);
            _view.AddLogEntry(formatted);
        }

        private void OnFeatureClicked(string featureName)
        {
            var characterData = _playerDataService.GetPlayerData();
            // Log feature usage (non-dice actions for now)
            var formatted = _gameLogService.FormatAction(characterData.CharacterName, $"Used: {featureName}");
            _view.AddLogEntry(formatted);
        }

        private void OnRestClicked(string restType)
        {
            var characterData = _playerDataService.GetPlayerData();
            // Log rest action
            var formatted = _gameLogService.FormatAction(characterData.CharacterName, restType);
            _view.AddLogEntry(formatted);
        }

        private void OnClearLogClicked()
        {
            _view.ClearLog();
        }

        private void OnLogEntryDeleteClicked(UnityEngine.UIElements.VisualElement entryCard)
        {
            _view.RemoveLogEntry(entryCard);
        }

        private void OnMoveButtonClicked()
        {
            if (_encounterModeManager == null || !_encounterModeManager.IsEncounterModeActive)
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


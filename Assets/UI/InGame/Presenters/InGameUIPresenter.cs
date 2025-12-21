using GameCore.UI;
using GameCore;
using GameCore.UI.InGame.Services;
using GameCore.UI.InGame.Models;
using GameCore.EncounterMode.Services;
using UnityEngine;
using UnityEngine.UIElements;
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
        private CharacterData _characterData;
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
                _playerInputs = FindFirstObjectByType<PlayerInputs>();
            }

            Model = new InGameUIModel();
            
            // Initialize services
            // NOTE: Services are instantiated directly here. For a larger project, consider using
            // dependency injection (e.g., Zenject, VContainer) to improve testability and follow
            // Dependency Inversion Principle. For now, direct instantiation is acceptable as these
            // are stateless services with no external dependencies.
            _diceRollService = new DiceRollService();
            _gameLogService = new GameLogService();
            _characterData = new CharacterData();
            
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
            var uiDocument = _view.GetComponent<UIDocument>();
            UIInputValidator.ValidateUIDocument(uiDocument);
            UIInputValidator.ValidateInputSystem();

            _view.TabClicked += OnTabClicked;
            _view.AbilityScoreClicked += OnAbilityScoreClicked;
            _view.SkillClicked += OnSkillClicked;
            _view.ActionClicked += OnActionClicked;
            _view.AttackClicked += OnAttackClicked;
            _view.FeatureClicked += OnFeatureClicked;
            _view.RestClicked += OnRestClicked;
            Model.StateChanged += OnModelStateChanged;

            // Push initial state to the view so it starts in sync with the model.
            // This will also configure input properly (UI starts closed, so input should be enabled)
            _view.UpdateView(Model.State);
            
            // Explicitly ensure input is enabled on startup (character sheet starts closed)
            if (_playerInputs != null)
            {
                _playerInputs.SetInputEnabled(true);
            }

            _initialized = true;
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
            }

            if (Model != null)
            {
                Model.StateChanged -= OnModelStateChanged;
            }

            _initialized = false;
        }
        #endregion

        #region Input Handling
        private void Update()
        {
            if (!_initialized)
                return;

            // Update look input based on whether mouse is over UI
            // This allows camera control when character sheet is open but mouse is not over it
            if (Model != null && Model.IsCharacterSheetOpen && _playerInputs != null)
            {
                // Only disable look input when mouse is actually over UI
                // This ensures camera control works when character sheet is open but mouse is not over it
                _playerInputs.cursorInputForLook = !UIInteractionService.Instance.ShouldBlockCameraInput();
            }

#if ENABLE_INPUT_SYSTEM
            HandleKeyboardInput();
#endif
        }

#if ENABLE_INPUT_SYSTEM
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
                return;
            }

            // Arrow key navigation when character sheet is open
            if (Model.IsCharacterSheetOpen)
            {
                if (keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    Model.NextTab();
                }
                else if (keyboard.leftArrowKey.wasPressedThisFrame)
                {
                    Model.PreviousTab();
                }
            }
        }
#endif
        #endregion

        #region Model Event Handlers
        private void OnModelStateChanged(InGameUIState state)
        {
            _view.UpdateView(state);
            // Don't disable all input when character sheet opens - we want camera control to work
            // Only disable movement input, not look input
            UpdatePlayerInput(state.IsCharacterSheetOpen);
            UpdateCursorState(state.IsCharacterSheetOpen);
            
            // Restore cursor input for look when character sheet closes
            if (!state.IsCharacterSheetOpen && _playerInputs != null)
            {
                _playerInputs.cursorInputForLook = true;
            }
        }
        #endregion

        #region UI State Management
        /// <summary>
        /// Updates cursor lock state and visibility based on character sheet state.
        /// Shows cursor when sheet opens, hides it when sheet closes.
        /// Uses Confined mode when open to allow camera control while cursor is visible.
        /// </summary>
        private void UpdateCursorState(bool isCharacterSheetOpen)
        {
            if (isCharacterSheetOpen)
            {
                // Show cursor when character sheet opens
                // Use Confined mode to allow camera control while cursor is visible
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
        /// In encounter mode, we keep look input enabled for camera control.
        /// </summary>
        private void UpdatePlayerInput(bool isUIOpen)
        {
            if (_playerInputs == null)
            {
                Debug.LogWarning("InGameUIPresenter: PlayerInputs is null! Input may not be disabled when UI is open.");
                return;
            }

            // In encounter mode, we want camera control to work even when character sheet is open
            // So we don't disable all input - we'll handle look input separately based on mouse position
            // For now, keep input enabled so camera works
            // Movement will be handled by encounter mode movement system anyway
            _playerInputs.SetInputEnabled(true);
        }
        #endregion

        #region View Event Handlers
        private void OnTabClicked(int tabIndex)
        {
            Model.SetTab(tabIndex);
        }

        private void OnAbilityScoreClicked(string abilityName)
        {
            int modifier = _characterData.GetAbilityModifier(abilityName);
            var rollResult = _diceRollService.RollD20Check(
                _characterData.CharacterName,
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
            string abilityName = CharacterData.GetSkillAbility(skillName);
            int modifier = _characterData.GetSkillModifier(skillName, abilityName);
            bool isProficient = _characterData.ProficientSkills.Contains(skillName);

            var breakdowns = new List<ModifierBreakdown>
            {
                new ModifierBreakdown { Source = abilityName, Value = _characterData.GetAbilityModifier(abilityName) }
            };

            if (isProficient)
            {
                breakdowns.Add(new ModifierBreakdown 
                { 
                    Source = "Proficiency", 
                    Value = _characterData.ProficiencyBonus 
                });
            }

            var rollResult = _diceRollService.RollD20Check(
                _characterData.CharacterName,
                skillName,
                modifier,
                breakdowns
            );

            var formatted = _gameLogService.FormatRollResult(rollResult);
            _view.AddLogEntry(formatted);
        }

        private void OnActionClicked(string actionName)
        {
            // Log the action (non-dice actions)
            var formatted = _gameLogService.FormatAction(_characterData.CharacterName, actionName);
            _view.AddLogEntry(formatted);
        }

        private void OnAttackClicked(string weaponName)
        {
            // Get weapon data from model (calculates bonuses based on character stats)
            var weaponData = WeaponData.GetWeaponData(weaponName, _characterData);

            var (attackRoll, damageRoll) = _diceRollService.RollAttack(
                _characterData.CharacterName,
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
            // Log feature usage (non-dice actions for now)
            var formatted = _gameLogService.FormatAction(_characterData.CharacterName, $"Used: {featureName}");
            _view.AddLogEntry(formatted);
        }

        private void OnRestClicked(string restType)
        {
            // Log rest action
            var formatted = _gameLogService.FormatAction(_characterData.CharacterName, restType);
            _view.AddLogEntry(formatted);
        }
        #endregion
    }
}


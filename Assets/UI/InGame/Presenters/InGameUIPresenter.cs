using GameCore.UI;
using GameCore;
using GameCore.UI.InGame.Services;
using GameCore.UI.InGame.Models;
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
            UpdateCursorState(state.IsCharacterSheetOpen);
            _view.UpdateView(state);
            UpdatePlayerInput(state.IsCharacterSheetOpen);
        }
        #endregion

        #region UI State Management
        /// <summary>
        /// Updates cursor lock state and visibility based on UI state.
        /// </summary>
        private void UpdateCursorState(bool isUIOpen)
        {
            if (isUIOpen)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            else
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        /// <summary>
        /// Updates player input enabled state based on UI visibility.
        /// </summary>
        private void UpdatePlayerInput(bool isUIOpen)
        {
            if (_playerInputs == null)
            {
                Debug.LogWarning("InGameUIPresenter: PlayerInputs is null! Input may not be disabled when UI is open.");
                return;
            }

            _playerInputs.SetInputEnabled(!isUIOpen);
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


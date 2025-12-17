using GameCore.UI;
using GameCore;
using GameCore.UI.InGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Minimal presenter for in-game UI.
    /// Currently just wires the model and view; ready to grow with diegetic HUD logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class InGameUIPresenter : MonoBehaviour, IUIPresenter<InGameUIModel, InGameUIView>
    {
        [SerializeField] private InGameUIView _view;
        [Header("Input")]
        [Tooltip("PlayerInputs component to disable when character sheet is open. If not assigned, will search for it.")]
        [SerializeField] private PlayerInputs _playerInputs;

        public InGameUIModel Model { get; private set; }
        public InGameUIView View => _view;

        private bool _initialized;

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
            }

            if (Model != null)
            {
                Model.StateChanged -= OnModelStateChanged;
            }

            _initialized = false;
        }

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


        private void OnModelStateChanged(InGameUIState state)
        {
            UpdateCursorState(state.IsCharacterSheetOpen);
            _view.UpdateView(state);
            UpdatePlayerInput(state.IsCharacterSheetOpen);
        }

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

        private void OnTabClicked(int tabIndex)
        {
            Model.SetTab(tabIndex);
        }

        private void OnAbilityScoreClicked(string abilityName)
        {
            // TODO: Implement ability score interaction logic
            // This will be handled by a future ability score system
        }

        private void OnSkillClicked(string skillName)
        {
            // TODO: Implement skill interaction logic
            // This will be handled by a future skill system
        }
    }
}


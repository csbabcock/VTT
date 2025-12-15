using GameCore.UI;
using GameCore;
using UnityEngine;

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

            // Ensure input is enabled initially (character sheet starts closed)
            if (_playerInputs != null)
            {
                _playerInputs.SetInputEnabled(true);
            }

            _view.CharacterSheetToggleRequested += OnCharacterSheetToggleRequested;
            _view.NextPageRequested += OnNextPageRequested;
            _view.PreviousPageRequested += OnPreviousPageRequested;
            _view.AbilityScoreClicked += OnAbilityScoreClicked;
            _view.SkillClicked += OnSkillClicked;
            Model.StateChanged += OnModelStateChanged;

            // Push initial state to the view so it starts in sync with the model.
            _view.UpdateView(Model.State);

            _initialized = true;
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            if (_view != null)
            {
                _view.CharacterSheetToggleRequested -= OnCharacterSheetToggleRequested;
                _view.NextPageRequested -= OnNextPageRequested;
                _view.PreviousPageRequested -= OnPreviousPageRequested;
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
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.tabKey.wasPressedThisFrame)
                {
                    ToggleCharacterSheet();
                }

                // Arrow key navigation when character sheet is open
                if (Model.IsCharacterSheetOpen)
                {
                    if (keyboard.rightArrowKey.wasPressedThisFrame)
                    {
                        Model.NextPage();
                    }
                    else if (keyboard.leftArrowKey.wasPressedThisFrame)
                    {
                        Model.PreviousPage();
                    }
                }
            }
#endif
        }

        private void OnCharacterSheetToggleRequested()
        {
            ToggleCharacterSheet();
        }

        private void ToggleCharacterSheet()
        {
            Model.ToggleCharacterSheet();
        }

        private void OnModelStateChanged(InGameUIState state)
        {
            _view.UpdateView(state);
            
            // Disable/enable player input based on character sheet visibility
            if (_playerInputs != null)
            {
                _playerInputs.SetInputEnabled(!state.IsCharacterSheetOpen);
            }
        }

        private void OnNextPageRequested()
        {
            Model.NextPage();
        }

        private void OnPreviousPageRequested()
        {
            Model.PreviousPage();
        }

        private void OnAbilityScoreClicked(string abilityName)
        {
            // Placeholder for future ability score logic
            Debug.Log($"Ability score clicked: {abilityName}");
        }

        private void OnSkillClicked(string skillName)
        {
            // Placeholder for future skill logic
            Debug.Log($"Skill clicked: {skillName}");
        }
    }
}


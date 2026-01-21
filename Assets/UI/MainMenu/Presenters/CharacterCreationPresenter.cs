using GameCore.UI;
using UnityEngine;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Presenter for character creation UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterCreationPresenter : MonoBehaviour, IUIPresenter<CharacterCreationModel, CharacterCreationView>
    {
        [Header("References")]
        [SerializeField] private CharacterCreationView _view;

        public CharacterCreationModel Model { get; private set; }
        public CharacterCreationView View => _view;

        private bool _initialized;

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<CharacterCreationView>();
            }

            Model = new CharacterCreationModel();
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

            // Subscribe to view events
            _view.ClassSelected += HandleClassSelected;
            _view.RaceSelected += HandleRaceSelected;
            _view.BackgroundSelected += HandleBackgroundSelected;
            _view.AbilityScoreChanged += HandleAbilityScoreChanged;
            _view.RollAbilitiesClicked += HandleRollAbilitiesClicked;
            _view.CancelClicked += HandleCancelClicked;
            _view.CreateCharacterClicked += HandleCreateCharacterClicked;

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
                _view.BackgroundSelected -= HandleBackgroundSelected;
                _view.AbilityScoreChanged -= HandleAbilityScoreChanged;
                _view.RollAbilitiesClicked -= HandleRollAbilitiesClicked;
                _view.CancelClicked -= HandleCancelClicked;
                _view.CreateCharacterClicked -= HandleCreateCharacterClicked;
            }

            if (Model != null)
            {
                Model.StateChanged -= HandleModelStateChanged;
            }

            _initialized = false;
        }

        public void Show()
        {
            // Ensure initialized before showing
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

        private void HandleBackgroundSelected(string backgroundName)
        {
            Model.SetSelectedBackground(backgroundName);
        }

        private void HandleAbilityScoreChanged(int index, int value)
        {
            Model.SetAbilityScore(index, value);
        }

        private void HandleRollAbilitiesClicked()
        {
            // Roll 4d6 drop lowest for each ability score
            int[] newScores = new int[6];
            for (int i = 0; i < 6; i++)
            {
                newScores[i] = Roll4d6DropLowest();
            }
            Model.SetAbilityScores(newScores);
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
            // Validate that required fields are filled
            if (string.IsNullOrEmpty(Model.State.SelectedClass))
            {
                Debug.LogWarning("CharacterCreationPresenter: Class must be selected.");
                return;
            }

            if (string.IsNullOrEmpty(Model.State.SelectedRace))
            {
                Debug.LogWarning("CharacterCreationPresenter: Race must be selected.");
                return;
            }

            if (string.IsNullOrEmpty(Model.State.SelectedBackground))
            {
                Debug.LogWarning("CharacterCreationPresenter: Background must be selected.");
                return;
            }

            // TODO: Save character to file
            Debug.Log($"CharacterCreationPresenter: Creating character - Class: {Model.State.SelectedClass}, Race: {Model.State.SelectedRace}, Background: {Model.State.SelectedBackground}");

            // Hide the character creation UI
            Hide();

            // TODO: Refresh character list in main menu
        }

        private void HandleModelStateChanged(CharacterCreationState state)
        {
            _view.UpdateView(state);
        }
    }
}

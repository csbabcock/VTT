using GameCore.UI;
using GameCore.PlayerData.Rulesets;
using UnityEngine;
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
                int modifier = _calculator.CalculateAbilityModifier(score);
                _view.UpdateAbilityScoreDisplay(i, score, modifier);
            }

            // Calculate derived stats
            int conMod = _calculator.CalculateAbilityModifier(state.AbilityScores[2]); // CON
            int dexMod = _calculator.CalculateAbilityModifier(state.AbilityScores[1]); // DEX
            int wisMod = _calculator.CalculateAbilityModifier(state.AbilityScores[4]); // WIS
            int proficiencyBonus = _calculator.CalculateProficiencyBonus(1); // Level 1

            // Calculate HP (simplified - would use class hit die)
            int hitPoints = CalculateHitPoints(state.SelectedClass, conMod);

            // Calculate AC (simplified)
            int armorClass = 10 + dexMod;

            // Calculate spellcasting stats if applicable
            int? spellSaveDC = null;
            int? spellAttack = null;
            if (IsSpellcaster(state.SelectedClass))
            {
                int castingModifier = GetCastingModifier(state.SelectedClass, state.AbilityScores);
                spellSaveDC = 8 + proficiencyBonus + castingModifier;
                spellAttack = proficiencyBonus + castingModifier;
            }

            _view.UpdateDerivedStats(hitPoints, armorClass, dexMod, proficiencyBonus, spellSaveDC, spellAttack);
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

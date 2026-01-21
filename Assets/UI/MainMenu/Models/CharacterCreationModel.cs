using System;
using GameCore.UI;
using UnityEngine;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Model for character creation state.
    /// </summary>
    public class CharacterCreationModel : IUIModel<CharacterCreationState>
    {
        public event Action<CharacterCreationState> StateChanged;

        private CharacterCreationState _state;

        public CharacterCreationState State => _state;

        public CharacterCreationModel()
        {
            _state = new CharacterCreationState
            {
                SelectedClass = null,
                SelectedRace = null,
                SelectedBackground = null,
                AbilityScores = new int[] { 10, 12, 13, 8, 15, 10 }, // STR, DEX, CON, INT, WIS, CHA
                IsVisible = false
            };
        }

        public void SetSelectedClass(string className)
        {
            if (_state.SelectedClass != className)
            {
                _state.SelectedClass = className;
                NotifyStateChanged();
            }
        }

        public void SetSelectedRace(string raceName)
        {
            if (_state.SelectedRace != raceName)
            {
                _state.SelectedRace = raceName;
                NotifyStateChanged();
            }
        }

        public void SetSelectedBackground(string backgroundName)
        {
            if (_state.SelectedBackground != backgroundName)
            {
                _state.SelectedBackground = backgroundName;
                NotifyStateChanged();
            }
        }

        public void SetAbilityScore(int index, int value)
        {
            if (index >= 0 && index < _state.AbilityScores.Length)
            {
                _state.AbilityScores[index] = Mathf.Clamp(value, 3, 18);
                NotifyStateChanged();
            }
        }

        public void SetAbilityScores(int[] scores)
        {
            if (scores != null && scores.Length == 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    _state.AbilityScores[i] = Mathf.Clamp(scores[i], 3, 18);
                }
                NotifyStateChanged();
            }
        }

        public void SetVisible(bool visible)
        {
            if (_state.IsVisible != visible)
            {
                _state.IsVisible = visible;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(_state);
        }
    }

    /// <summary>
    /// State snapshot for character creation UI.
    /// </summary>
    [Serializable]
    public class CharacterCreationState
    {
        public string SelectedClass;
        public string SelectedRace;
        public string SelectedBackground;
        public int[] AbilityScores; // STR, DEX, CON, INT, WIS, CHA
        public bool IsVisible;
    }
}

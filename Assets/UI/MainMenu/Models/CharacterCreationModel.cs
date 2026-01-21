using System;
using GameCore.UI;
using UnityEngine;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Immutable snapshot of character creation state.
    /// </summary>
    public struct CharacterCreationState
    {
        public bool IsVisible;
        public string SelectedClass;
        public string SelectedRace;
        public string SelectedBackground;
        public int[] AbilityScores; // STR, DEX, CON, INT, WIS, CHA
    }

    /// <summary>
    /// Model for character creation. Holds state and raises StateChanged event when state updates.
    /// Follows same pattern as MainMenuModel with immutable state snapshots.
    /// </summary>
    public class CharacterCreationModel : IUIModel<CharacterCreationState>
    {
        public event Action<CharacterCreationState> StateChanged;

        public CharacterCreationState State { get; private set; }

        public CharacterCreationModel()
        {
            State = new CharacterCreationState
            {
                IsVisible = false,
                SelectedClass = string.Empty,
                SelectedRace = string.Empty,
                SelectedBackground = string.Empty,
                AbilityScores = new int[] { 10, 12, 13, 8, 15, 10 } // STR, DEX, CON, INT, WIS, CHA
            };
        }

        public void SetVisible(bool visible)
        {
            if (State.IsVisible == visible)
                return;

            State = new CharacterCreationState
            {
                IsVisible = visible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = State.AbilityScores
            };

            StateChanged?.Invoke(State);
        }

        public void SetSelectedClass(string className)
        {
            if (State.SelectedClass == className)
                return;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = className ?? string.Empty,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = State.AbilityScores
            };

            StateChanged?.Invoke(State);
        }

        public void SetSelectedRace(string raceName)
        {
            if (State.SelectedRace == raceName)
                return;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = raceName ?? string.Empty,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = State.AbilityScores
            };

            StateChanged?.Invoke(State);
        }

        public void SetSelectedBackground(string backgroundName)
        {
            if (State.SelectedBackground == backgroundName)
                return;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = backgroundName ?? string.Empty,
                AbilityScores = State.AbilityScores
            };

            StateChanged?.Invoke(State);
        }

        public void SetAbilityScore(int index, int value)
        {
            if (index < 0 || index >= State.AbilityScores.Length)
                return;

            int clampedValue = Mathf.Clamp(value, 3, 18);
            if (State.AbilityScores[index] == clampedValue)
                return;

            int[] newScores = new int[6];
            Array.Copy(State.AbilityScores, newScores, 6);
            newScores[index] = clampedValue;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = newScores
            };

            StateChanged?.Invoke(State);
        }

        public void SetAbilityScores(int[] scores)
        {
            if (scores == null || scores.Length != 6)
                return;

            int[] clampedScores = new int[6];
            bool changed = false;
            for (int i = 0; i < 6; i++)
            {
                clampedScores[i] = Mathf.Clamp(scores[i], 3, 18);
                if (clampedScores[i] != State.AbilityScores[i])
                    changed = true;
            }

            if (!changed)
                return;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = clampedScores
            };

            StateChanged?.Invoke(State);
        }
    }
}


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
        public int[] AbilityScores; // STR, DEX, CON, INT, WIS, CHA (final assigned scores)
        public int[] RolledScores; // The 6 rolled scores from dice (null if not rolled yet)
        public int[] AssignedRolledScoreIndices; // Which rolled score index is assigned to each ability (-1 if unassigned)
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
                AbilityScores = new int[] { -1, -1, -1, -1, -1, -1 }, // Unassigned by default
                RolledScores = null, // No scores rolled yet
                AssignedRolledScoreIndices = new int[] { -1, -1, -1, -1, -1, -1 } // No assignments
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
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices
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
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices
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
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices
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
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices
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
                AbilityScores = newScores,
                RolledScores = State.RolledScores,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices
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
                AbilityScores = clampedScores,
                RolledScores = State.RolledScores,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Sets the rolled scores from dice rolling. Resets all assignments.
        /// </summary>
        public void SetRolledScores(int[] rolledScores)
        {
            if (rolledScores == null || rolledScores.Length != 6)
                return;

            int[] clampedScores = new int[6];
            for (int i = 0; i < 6; i++)
            {
                clampedScores[i] = Mathf.Clamp(rolledScores[i], 3, 18);
            }

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = new int[] { -1, -1, -1, -1, -1, -1 }, // Reset to unassigned
                RolledScores = clampedScores,
                AssignedRolledScoreIndices = new int[] { -1, -1, -1, -1, -1, -1 } // Reset assignments
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Assigns a rolled score to an ability. If the rolled score is already assigned elsewhere, swaps them.
        /// </summary>
        public void AssignRolledScoreToAbility(int rolledScoreIndex, int abilityIndex)
        {
            if (State.RolledScores == null || rolledScoreIndex < 0 || rolledScoreIndex >= 6 || 
                abilityIndex < 0 || abilityIndex >= 6)
                return;

            int[] newAssignedIndices = new int[6];
            Array.Copy(State.AssignedRolledScoreIndices, newAssignedIndices, 6);

            // If this rolled score is already assigned to another ability, unassign it
            for (int i = 0; i < 6; i++)
            {
                if (newAssignedIndices[i] == rolledScoreIndex)
                {
                    newAssignedIndices[i] = -1;
                    break;
                }
            }

            // If this ability already has a rolled score assigned, unassign it
            int previousRolledIndex = newAssignedIndices[abilityIndex];
            if (previousRolledIndex >= 0)
            {
                // The previous rolled score becomes unassigned (handled above if it's the same)
            }

            // Assign the rolled score to this ability
            newAssignedIndices[abilityIndex] = rolledScoreIndex;

            // Calculate new ability scores
            int[] newAbilityScores = new int[6];
            for (int i = 0; i < 6; i++)
            {
                if (newAssignedIndices[i] >= 0 && newAssignedIndices[i] < State.RolledScores.Length)
                {
                    newAbilityScores[i] = State.RolledScores[newAssignedIndices[i]];
                }
                else
                {
                    newAbilityScores[i] = -1; // Unassigned
                }
            }

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = newAbilityScores,
                RolledScores = State.RolledScores,
                AssignedRolledScoreIndices = newAssignedIndices
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Unassigns a rolled score from an ability (drags it back to pool).
        /// </summary>
        public void UnassignAbilityScore(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= 6)
                return;

            int[] newAssignedIndices = new int[6];
            Array.Copy(State.AssignedRolledScoreIndices, newAssignedIndices, 6);
            newAssignedIndices[abilityIndex] = -1;

            int[] newAbilityScores = new int[6];
            Array.Copy(State.AbilityScores, newAbilityScores, 6);
            newAbilityScores[abilityIndex] = -1;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = newAbilityScores,
                RolledScores = State.RolledScores,
                AssignedRolledScoreIndices = newAssignedIndices
            };

            StateChanged?.Invoke(State);
        }
    }
}


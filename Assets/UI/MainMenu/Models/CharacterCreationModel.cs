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
        public int[] RolledScores; // The 6 rolled scores from dice (null if not rolled yet). In manual mode can contain -1 for empty.
        public int[][] RolledDiceBreakdown; // For Roll option: RolledDiceBreakdown[i] = 4 dice for slot i, or null
        public int[] RolledDroppedIndices; // For Roll option: which die index (0-3) was dropped per slot, or -1
        public int[] AssignedRolledScoreIndices; // Which rolled score index is assigned to each ability (-1 if unassigned)
        public bool IsManualMode; // When true, pool slots are editable and can be -1 (empty)
        public string SelectedScoreMethod; // "Roll", "StandardArray", "Manual", "PointBuy", or ""
    }

    /// <summary>
    /// Point Buy cost table: score 8 = 0, 9 = 1, 10 = 2, 11 = 3, 12 = 4, 13 = 5, 14 = 7, 15 = 9.
    /// Used for Point Buy ability score mode (27 points total).
    /// </summary>
    public static class PointBuyCostTable
    {
        public const int TotalPoints = 27;
        public const int MinScore = 8;
        public const int MaxScore = 15;

        public static int CostForScore(int score)
        {
            if (score < MinScore || score > MaxScore)
                return 0;
            return score switch
            {
                8 => 0,
                9 => 1,
                10 => 2,
                11 => 3,
                12 => 4,
                13 => 5,
                14 => 7,
                15 => 9,
                _ => 0
            };
        }

        public static int GetPointsRemaining(int[] abilityScores)
        {
            if (abilityScores == null || abilityScores.Length != 6)
                return TotalPoints;
            int spent = 0;
            for (int i = 0; i < 6; i++)
            {
                int s = abilityScores[i];
                spent += CostForScore(s >= MinScore && s <= MaxScore ? s : MinScore);
            }
            return TotalPoints - spent;
        }
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
                RolledDiceBreakdown = null,
                RolledDroppedIndices = null,
                AssignedRolledScoreIndices = new int[] { -1, -1, -1, -1, -1, -1 }, // No assignments
                IsManualMode = false,
                SelectedScoreMethod = string.Empty
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
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
            };

            StateChanged?.Invoke(State);
        }

        public void SetSelectedScoreMethod(string method)
        {
            string value = method ?? string.Empty;
            if (State.SelectedScoreMethod == value)
                return;

            int[] abilityScores = State.AbilityScores;
            int[] rolledScores = State.RolledScores;
            int[][] diceBreakdown = State.RolledDiceBreakdown;
            int[] droppedIndices = State.RolledDroppedIndices;
            int[] assignedIndices = State.AssignedRolledScoreIndices;
            bool isManualMode = State.IsManualMode;

            if (value == "PointBuy")
            {
                abilityScores = new int[] { 8, 8, 8, 8, 8, 8 };
                rolledScores = null;
                diceBreakdown = null;
                droppedIndices = null;
                assignedIndices = new int[] { -1, -1, -1, -1, -1, -1 };
                isManualMode = false;
            }

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = abilityScores,
                RolledScores = rolledScores,
                RolledDiceBreakdown = diceBreakdown,
                RolledDroppedIndices = droppedIndices,
                AssignedRolledScoreIndices = assignedIndices,
                IsManualMode = isManualMode,
                SelectedScoreMethod = value
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
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
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
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
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
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
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
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
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
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Sets the rolled scores from dice rolling, standard array, or manual (empty slots). Resets all assignments.
        /// </summary>
        /// <param name="rolledScores">Six values. For manual mode use -1 for empty slots.</param>
        /// <param name="isManualMode">When true, -1 is allowed for empty editable slots.</param>
        /// <param name="diceBreakdown">For Roll option: 6 slots, each int[4] (the 4 dice). Null for Standard Array/Manual.</param>
        /// <param name="droppedIndices">For Roll option: which die index (0-3) was dropped per slot. Null when no breakdown.</param>
        public void SetRolledScores(int[] rolledScores, bool isManualMode = false, int[][] diceBreakdown = null, int[] droppedIndices = null)
        {
            if (rolledScores == null || rolledScores.Length != 6)
                return;

            int[] clampedScores = new int[6];
            for (int i = 0; i < 6; i++)
            {
                if (isManualMode && rolledScores[i] < 0)
                    clampedScores[i] = -1;
                else
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
                RolledDiceBreakdown = diceBreakdown,
                RolledDroppedIndices = droppedIndices,
                AssignedRolledScoreIndices = new int[] { -1, -1, -1, -1, -1, -1 }, // Reset assignments
                IsManualMode = isManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Updates a single score in the pool (for manual mode). Value must be -1 (clear) or 3–18.
        /// </summary>
        public void SetRolledScoreAt(int index, int value)
        {
            if (!State.IsManualMode || State.RolledScores == null || index < 0 || index >= 6)
                return;
            int clamped = value < 0 ? -1 : Mathf.Clamp(value, 3, 18);
            if (State.RolledScores[index] == clamped)
                return;

            int[] newScores = new int[6];
            Array.Copy(State.RolledScores, newScores, 6);
            newScores[index] = clamped;

            // Recompute ability scores from assignments
            int[] newAbilityScores = new int[6];
            for (int i = 0; i < 6; i++)
            {
                int ri = State.AssignedRolledScoreIndices[i];
                if (ri >= 0 && ri < 6 && newScores[ri] >= 0)
                    newAbilityScores[i] = newScores[ri];
                else
                    newAbilityScores[i] = -1;
            }

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = newAbilityScores,
                RolledScores = newScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = true,
                SelectedScoreMethod = State.SelectedScoreMethod
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
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = newAssignedIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Sets an ability score in Point Buy mode. Only valid when SelectedScoreMethod == "PointBuy".
        /// Score must be 8-15; total cost of all six scores must not exceed 27 points.
        /// </summary>
        public void SetPointBuyAbilityScore(int abilityIndex, int newScore)
        {
            if (State.SelectedScoreMethod != "PointBuy" || abilityIndex < 0 || abilityIndex >= 6)
                return;
            int clamped = Mathf.Clamp(newScore, PointBuyCostTable.MinScore, PointBuyCostTable.MaxScore);
            int[] current = State.AbilityScores;
            if (current == null || current.Length != 6)
                return;
            int currentScore = current[abilityIndex];
            if (currentScore >= PointBuyCostTable.MinScore && currentScore <= PointBuyCostTable.MaxScore)
            { }
            else
                currentScore = PointBuyCostTable.MinScore; // treat invalid as 8 for cost

            int currentCost = PointBuyCostTable.CostForScore(currentScore);
            int newCost = PointBuyCostTable.CostForScore(clamped);
            int pointsRemaining = PointBuyCostTable.GetPointsRemaining(current);
            int deltaCost = newCost - currentCost;
            if (deltaCost > pointsRemaining)
                return; // not enough points to increase

            int[] newScores = new int[6];
            Array.Copy(current, newScores, 6);
            newScores[abilityIndex] = clamped;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClass = State.SelectedClass,
                SelectedRace = State.SelectedRace,
                SelectedBackground = State.SelectedBackground,
                AbilityScores = newScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
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
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = newAssignedIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod
            };

            StateChanged?.Invoke(State);
        }
    }
}


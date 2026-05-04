using System;
using System.Collections.Generic;
using GameCore.UI;
using UnityEngine;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// One titled group of proficiency strings for the character creation proficiencies panel.
    /// </summary>
    public readonly struct CharacterProficiencySection
    {
        public string CategoryTitle { get; }
        public IReadOnlyList<string> Items { get; }

        public CharacterProficiencySection(string categoryTitle, IReadOnlyList<string> items)
        {
            CategoryTitle = categoryTitle ?? string.Empty;
            Items = items ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Immutable snapshot of character creation state.
    /// </summary>
    public struct CharacterCreationState
    {
        public bool IsVisible;
        /// <summary>Ruleset content id, e.g. class.fighter</summary>
        public string SelectedClassId;
        /// <summary>Ruleset content id, e.g. race.hill_dwarf</summary>
        public string SelectedRaceId;
        /// <summary>Ruleset content id, e.g. background.acolyte</summary>
        public string SelectedBackgroundId;
        /// <summary>Total character level (1–20). Sum of <see cref="ClassLevels"/> (clamped). Used for proficiency and global scaling.</summary>
        public int CharacterLevel;
        /// <summary>Per-class levels (ruleset id → level). Total level is the sum; each class is capped so the sum does not exceed 20.</summary>
        public Dictionary<string, int> ClassLevels;
        public int[] AbilityScores; // STR, DEX, CON, INT, WIS, CHA (final assigned scores)
        public int[] RolledScores; // The 6 rolled scores from dice (null if not rolled yet). In manual mode can contain -1 for empty.
        public int[][] RolledDiceBreakdown; // For Roll option: RolledDiceBreakdown[i] = 4 dice for slot i, or null
        public int[] RolledDroppedIndices; // For Roll option: which die index (0-3) was dropped per slot, or -1
        public int[] AssignedRolledScoreIndices; // Which rolled score index is assigned to each ability (-1 if unassigned)
        public bool IsManualMode; // When true, pool slots are editable and can be -1 (empty)
        public string SelectedScoreMethod; // "Roll", "StandardArray", "Manual", "PointBuy", or ""
        public bool AbilityScoresLocked; // When true, pool and method buttons are hidden; only final ability scores shown
        public Dictionary<string, string> SelectedRaceAbilityChoices; // choice id -> ability code
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
        public const int MinCharacterLevel = 1;
        public const int MaxCharacterLevel = 20;

        /// <summary>Direct Manual ability tile entry (empty uses -1 in model).</summary>
        public const int MinManualAbilityEntryScore = 1;
        public const int MaxManualAbilityEntryScore = 20;

        public event Action<CharacterCreationState> StateChanged;

        public CharacterCreationState State { get; private set; }

        public CharacterCreationModel()
        {
            State = new CharacterCreationState
            {
                IsVisible = false,
                SelectedClassId = string.Empty,
                SelectedRaceId = string.Empty,
                SelectedBackgroundId = string.Empty,
                CharacterLevel = 1,
                ClassLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                AbilityScores = new int[] { -1, -1, -1, -1, -1, -1 }, // Unassigned by default
                RolledScores = null, // No scores rolled yet
                RolledDiceBreakdown = null,
                RolledDroppedIndices = null,
                AssignedRolledScoreIndices = new int[] { -1, -1, -1, -1, -1, -1 }, // No assignments
                IsManualMode = false,
                SelectedScoreMethod = string.Empty,
                AbilityScoresLocked = false,
                SelectedRaceAbilityChoices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        public void SetVisible(bool visible)
        {
            if (State.IsVisible == visible)
                return;

            State = new CharacterCreationState
            {
                IsVisible = visible,
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Sets level for one class (ruleset id). Clamps to 1–20 per class and caps so the sum of all class levels ≤ 20.
        /// </summary>
        public void SetClassLevel(string classId, int level)
        {
            if (string.IsNullOrEmpty(classId))
                return;

            Dictionary<string, int> dict = CloneClassLevels(State.ClassLevels);
            if (!dict.ContainsKey(classId))
                dict[classId] = MinCharacterLevel;

            int currentLevel = dict[classId];
            int othersSum = SumClassLevelsInternal(dict) - currentLevel;
            int cap = Mathf.Clamp(MaxCharacterLevel - othersSum, MinCharacterLevel, MaxCharacterLevel);
            int clamped = Mathf.Clamp(level, MinCharacterLevel, cap);

            if (clamped == currentLevel)
                return;

            dict[classId] = clamped;
            int total = SumClassLevelsInternal(dict);
            int characterLevel = Mathf.Clamp(total, MinCharacterLevel, MaxCharacterLevel);

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                CharacterLevel = characterLevel,
                ClassLevels = dict,
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
            };
            StateChanged?.Invoke(State);
        }

        /// <summary>Sets level for <see cref="CharacterCreationState.SelectedClassId"/>.</summary>
        public void SetSelectedClassLevel(int level)
        {
            if (string.IsNullOrEmpty(State.SelectedClassId))
                return;
            SetClassLevel(State.SelectedClassId, level);
        }

        /// <summary>Same constraints as <see cref="SetClassLevel"/> for the current selection.</summary>
        public void SetCharacterLevel(int level) => SetSelectedClassLevel(level);

        /// <summary>
        /// Locks ability scores: hides pool and method buttons in the UI; only final ability scores remain visible.
        /// </summary>
        public void SetAbilityScoresLocked(bool locked)
        {
            if (State.AbilityScoresLocked == locked)
                return;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = locked
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
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = abilityScores,
                RolledScores = rolledScores,
                RolledDiceBreakdown = diceBreakdown,
                RolledDroppedIndices = droppedIndices,
                AssignedRolledScoreIndices = assignedIndices,
                IsManualMode = isManualMode,
                SelectedScoreMethod = value,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
            };
            StateChanged?.Invoke(State);
        }

        public void SetSelectedClassId(string classId)
        {
            string id = classId ?? string.Empty;
            if (State.SelectedClassId == id)
                return;

            // Single-class list UX: switching the highlighted class replaces the build with that class at level 1.
            // For multiclass, call <see cref="TryAddMulticlassClass"/> (and stop replacing here) when the UI supports it.
            Dictionary<string, int> newLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(id))
                newLevels[id] = MinCharacterLevel;

            int total = SumClassLevelsInternal(newLevels);
            int characterLevel = total <= 0
                ? MinCharacterLevel
                : Mathf.Clamp(total, MinCharacterLevel, MaxCharacterLevel);

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClassId = id,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = characterLevel,
                ClassLevels = newLevels,
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Multiclass: adds <paramref name="classId"/> at level 1 if missing, provided total character level would stay ≤ 20.
        /// Does not change <see cref="CharacterCreationState.SelectedClassId"/>; set selection after if needed.
        /// </summary>
        public bool TryAddMulticlassClass(string classId)
        {
            string id = classId ?? string.Empty;
            if (string.IsNullOrEmpty(id))
                return false;

            Dictionary<string, int> dict = CloneClassLevels(State.ClassLevels);
            if (dict.ContainsKey(id))
                return true;

            int prospective = SumClassLevelsInternal(dict) + MinCharacterLevel;
            if (prospective > MaxCharacterLevel)
                return false;

            dict[id] = MinCharacterLevel;
            int total = SumClassLevelsInternal(dict);
            int characterLevel = Mathf.Clamp(total, MinCharacterLevel, MaxCharacterLevel);

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                CharacterLevel = characterLevel,
                ClassLevels = dict,
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
            };
            StateChanged?.Invoke(State);
            return true;
        }

        private static Dictionary<string, int> CloneClassLevels(Dictionary<string, int> src)
        {
            if (src == null || src.Count == 0)
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            return new Dictionary<string, int>(src, StringComparer.OrdinalIgnoreCase);
        }

        private static int SumClassLevelsInternal(Dictionary<string, int> levels)
        {
            if (levels == null || levels.Count == 0)
                return 0;
            int s = 0;
            foreach (KeyValuePair<string, int> kv in levels)
                s += kv.Value;
            return s;
        }

        /// <summary>Sum of all class levels in the map (not clamped).</summary>
        public static int SumClassLevels(IReadOnlyDictionary<string, int> levels)
        {
            if (levels == null || levels.Count == 0)
                return 0;
            int s = 0;
            foreach (KeyValuePair<string, int> kv in levels)
                s += kv.Value;
            return s;
        }

        public static int GetClassLevel(IReadOnlyDictionary<string, int> levels, string classId)
        {
            if (string.IsNullOrEmpty(classId) || levels == null)
                return MinCharacterLevel;
            if (!levels.TryGetValue(classId, out int v))
                return MinCharacterLevel;
            return Mathf.Clamp(v, MinCharacterLevel, MaxCharacterLevel);
        }

        public static int MaxClassLevelAllowed(IReadOnlyDictionary<string, int> levels, string classId)
        {
            if (string.IsNullOrEmpty(classId))
                return MaxCharacterLevel;

            int others = 0;
            if (levels != null)
            {
                foreach (KeyValuePair<string, int> kv in levels)
                {
                    if (!string.Equals(kv.Key, classId, StringComparison.OrdinalIgnoreCase))
                        others += kv.Value;
                }
            }

            return Mathf.Clamp(MaxCharacterLevel - others, MinCharacterLevel, MaxCharacterLevel);
        }

        public void SetSelectedRaceId(string raceId)
        {
            if (State.SelectedRaceId == raceId)
                return;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = raceId ?? string.Empty,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
            };

            StateChanged?.Invoke(State);
        }


        public void SetRaceAbilityScoreChoice(string choiceId, string abilityCode)
        {
            if (string.IsNullOrEmpty(choiceId) || string.IsNullOrEmpty(abilityCode))
                return;

            Dictionary<string, string> choices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices);
            if (choices.TryGetValue(choiceId, out string current) &&
                string.Equals(current, abilityCode, StringComparison.OrdinalIgnoreCase))
                return;

            choices[choiceId] = abilityCode.Trim().ToUpperInvariant();
            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = choices
            };

            StateChanged?.Invoke(State);
        }

        private static Dictionary<string, string> CloneRaceAbilityChoices(Dictionary<string, string> choices)
        {
            return choices != null
                ? new Dictionary<string, string>(choices, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetSelectedBackgroundId(string backgroundId)
        {
            if (State.SelectedBackgroundId == backgroundId)
                return;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = backgroundId ?? string.Empty,
                AbilityScores = State.AbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
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
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = newScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
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
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = clampedScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
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
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = new int[] { -1, -1, -1, -1, -1, -1 }, // Reset to unassigned
                RolledScores = clampedScores,
                RolledDiceBreakdown = diceBreakdown,
                RolledDroppedIndices = droppedIndices,
                AssignedRolledScoreIndices = new int[] { -1, -1, -1, -1, -1, -1 }, // Reset assignments
                IsManualMode = isManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Direct entry on an ability tile (Manual method only). Value -1 clears; otherwise 1–20.
        /// </summary>
        public void SetManualAbilityEntry(int abilityIndex, int value)
        {
            if (State.SelectedScoreMethod != "Manual" || !State.IsManualMode ||
                abilityIndex < 0 || abilityIndex >= 6 || State.AbilityScores == null)
                return;

            int v = value < 0 ? -1 : Mathf.Clamp(value, MinManualAbilityEntryScore, MaxManualAbilityEntryScore);
            if (State.AbilityScores[abilityIndex] == v)
                return;

            int[] newAbilityScores = new int[6];
            Array.Copy(State.AbilityScores, newAbilityScores, 6);
            newAbilityScores[abilityIndex] = v;

            State = new CharacterCreationState
            {
                IsVisible = State.IsVisible,
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = newAbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Updates a single score in the pool (for manual mode). Value must be -1 (clear) or 1–20.
        /// </summary>
        public void SetRolledScoreAt(int index, int value)
        {
            if (!State.IsManualMode || State.RolledScores == null || index < 0 || index >= 6)
                return;
            int clamped = value < 0 ? -1 : Mathf.Clamp(value, MinManualAbilityEntryScore, MaxManualAbilityEntryScore);
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
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = newAbilityScores,
                RolledScores = newScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = true,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
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
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = newAbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = newAssignedIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
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
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = newScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = State.AssignedRolledScoreIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
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
                SelectedClassId = State.SelectedClassId,
                SelectedRaceId = State.SelectedRaceId,
                SelectedBackgroundId = State.SelectedBackgroundId,
                AbilityScores = newAbilityScores,
                RolledScores = State.RolledScores,
                RolledDiceBreakdown = State.RolledDiceBreakdown,
                RolledDroppedIndices = State.RolledDroppedIndices,
                AssignedRolledScoreIndices = newAssignedIndices,
                IsManualMode = State.IsManualMode,
                SelectedScoreMethod = State.SelectedScoreMethod,
                CharacterLevel = State.CharacterLevel,
                ClassLevels = CloneClassLevels(State.ClassLevels),
                AbilityScoresLocked = State.AbilityScoresLocked,
                SelectedRaceAbilityChoices = CloneRaceAbilityChoices(State.SelectedRaceAbilityChoices)
            };

            StateChanged?.Invoke(State);
        }
    }
}


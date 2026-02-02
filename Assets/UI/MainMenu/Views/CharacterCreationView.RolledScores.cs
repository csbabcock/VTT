using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Partial View: rolled scores pool UI (Roll / Standard Array / Manual).
    /// Single responsibility: building and updating the ability score pool elements.
    /// MVP: View only displays and raises events; no business logic (validation, dropped-index) here.
    /// </summary>
    public partial class CharacterCreationView
    {
        private VisualElement CreateRolledScoreElement(int rolledScoreIndex, int scoreValue, bool isAssigned, bool isManualMode, int[] diceForSlot = null, int droppedDieIndex = -1)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("character-creation-rolled-score-item");
            item.name = $"rolled-score-{rolledScoreIndex}";
            item.userData = rolledScoreIndex;

            if (isAssigned)
                item.AddToClassList("assigned");

            if (isManualMode)
            {
                BuildManualScoreElement(item, rolledScoreIndex, scoreValue, isAssigned);
            }
            else
            {
                BuildRollOrStandardArrayElement(item, rolledScoreIndex, scoreValue, isAssigned, diceForSlot, droppedDieIndex);
            }

            return item;
        }

        private void BuildManualScoreElement(VisualElement item, int rolledScoreIndex, int scoreValue, bool isAssigned)
        {
            TextField valueField = new TextField();
            valueField.AddToClassList("character-creation-rolled-score-value");
            valueField.name = $"rolled-score-field-{rolledScoreIndex}";
            valueField.value = scoreValue >= 3 ? scoreValue.ToString() : "";
            valueField.maxLength = 2;
            valueField.isDelayed = true;
            valueField.RegisterValueChangedCallback(evt =>
            {
                string raw = evt.newValue ?? "";
                string digitsOnly = new string(System.Array.FindAll(raw.ToCharArray(), c => char.IsDigit(c)));
                if (digitsOnly != raw)
                    valueField.SetValueWithoutNotify(digitsOnly);
                ManualScoreChanged?.Invoke(rolledScoreIndex, digitsOnly);
            });
            valueField.RegisterCallback<FocusOutEvent>(_ =>
            {
                ManualScoreChanged?.Invoke(rolledScoreIndex, valueField.value ?? "");
            });

            VisualElement dragHandle = new VisualElement();
            dragHandle.AddToClassList("character-creation-rolled-score-drag-handle");
            dragHandle.name = $"rolled-score-handle-{rolledScoreIndex}";
            dragHandle.pickingMode = PickingMode.Position;
            Label handleIcon = new Label("\u22ee");
            handleIcon.AddToClassList("character-creation-rolled-score-drag-handle-icon");
            handleIcon.pickingMode = PickingMode.Ignore;
            dragHandle.Add(handleIcon);

            dragHandle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || isAssigned) return;
                ManualScoreChanged?.Invoke(rolledScoreIndex, valueField.value);
                if (int.TryParse(valueField.value?.Trim(), out int v) && v >= 3 && v <= 18)
                {
                    _pendingManualDragIndex = rolledScoreIndex;
                    _pendingManualDragValue = v;
                    _pendingManualDragPosition = evt.position;
                }
                evt.StopPropagation();
            });
            item.Add(dragHandle);
            item.Add(valueField);
        }

        private void BuildRollOrStandardArrayElement(VisualElement item, int rolledScoreIndex, int scoreValue, bool isAssigned, int[] diceForSlot, int droppedDieIndex)
        {
            bool hasDice = diceForSlot != null && diceForSlot.Length == 4;

            if (hasDice)
                item.AddToClassList("character-creation-rolled-score-item--with-dice");

            Label valueLabel = new Label(scoreValue >= 3 ? scoreValue.ToString() : "—");
            valueLabel.AddToClassList("character-creation-rolled-score-value");

            if (hasDice)
            {
                VisualElement valueBox = new VisualElement();
                valueBox.AddToClassList("character-creation-rolled-score-value-box");
                valueBox.Add(valueLabel);
                item.Add(valueBox);

                VisualElement diceRow = new VisualElement();
                diceRow.AddToClassList("character-creation-rolled-score-dice-row");
                for (int d = 0; d < 4; d++)
                {
                    Label dieChip = new Label(diceForSlot[d].ToString());
                    dieChip.AddToClassList("character-creation-rolled-score-die-chip");
                    if (d == droppedDieIndex)
                        dieChip.AddToClassList("character-creation-rolled-score-die-chip-dropped");
                    else
                        dieChip.AddToClassList("character-creation-rolled-score-die-chip-kept");
                    dieChip.pickingMode = PickingMode.Ignore;
                    diceRow.Add(dieChip);
                }
                item.Add(diceRow);
            }
            else
            {
                item.Add(valueLabel);
            }

            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0 && !isAssigned)
                {
                    DragStartedFromRolledScore?.Invoke(rolledScoreIndex, scoreValue);
                    evt.StopPropagation();
                }
            });
        }

        public void UpdateRolledScores(int[] rolledScores, int[] assignedRolledScoreIndices, bool isManualMode = false, int[][] diceBreakdown = null, int[] droppedIndices = null)
        {
            if (_rolledScoresContainer == null) return;

            if (rolledScores == null || rolledScores.Length != 6)
            {
                _rolledScoresContainer.Clear();
                if (_rolledScoresPool != null)
                    _rolledScoresPool.style.display = DisplayStyle.None;
                return;
            }

            if (_rolledScoresPool != null)
                _rolledScoresPool.style.display = DisplayStyle.Flex;

            if (isManualMode && _rolledScoresContainer.childCount == 6)
            {
                VisualElement first = _rolledScoresContainer[0];
                if (first.Q<TextField>() != null)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        TextField field = _rolledScoresContainer[i].Q<TextField>($"rolled-score-field-{i}");
                        if (field != null)
                        {
                            string newVal = rolledScores[i] >= 3 ? rolledScores[i].ToString() : "";
                            if (field.value != newVal)
                                field.SetValueWithoutNotify(newVal);
                        }
                        bool isAssigned = false;
                        if (assignedRolledScoreIndices != null)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                if (assignedRolledScoreIndices[j] == i) { isAssigned = true; break; }
                            }
                        }
                        if (isAssigned)
                            _rolledScoresContainer[i].AddToClassList("assigned");
                        else
                            _rolledScoresContainer[i].RemoveFromClassList("assigned");
                    }
                    return;
                }
            }

            _rolledScoresContainer.Clear();

            for (int i = 0; i < 6; i++)
            {
                bool isAssigned = false;
                if (assignedRolledScoreIndices != null)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        if (assignedRolledScoreIndices[j] == i)
                        {
                            isAssigned = true;
                            break;
                        }
                    }
                }

                int[] diceForSlot = (diceBreakdown != null && i < diceBreakdown.Length) ? diceBreakdown[i] : null;
                int droppedIndex = (droppedIndices != null && i < droppedIndices.Length) ? droppedIndices[i] : -1;
                VisualElement scoreElement = CreateRolledScoreElement(i, rolledScores[i], isAssigned, isManualMode, diceForSlot, droppedIndex);
                _rolledScoresContainer.Add(scoreElement);
            }
        }
    }
}

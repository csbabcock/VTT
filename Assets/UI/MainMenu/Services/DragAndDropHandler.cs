using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Handles drag-and-drop detection and state management for ability score assignment.
    /// Follows Single Responsibility Principle - only handles drag detection, not business logic.
    /// </summary>
    public class DragAndDropHandler
    {
        private VisualElement _root;
        private VisualElement _abilityScoresGrid;
        private VisualElement _rolledScoresContainer;

        public DragAndDropHandler(VisualElement root, VisualElement abilityScoresGrid, VisualElement rolledScoresContainer)
        {
            _root = root;
            _abilityScoresGrid = abilityScoresGrid;
            _rolledScoresContainer = rolledScoresContainer;
        }

        /// <summary>
        /// Finds the ability row that contains the given element.
        /// </summary>
        public VisualElement FindAbilityRow(VisualElement element)
        {
            return FindAncestorByClass(element, "character-creation-ability-stat-row");
        }

        /// <summary>
        /// Finds the drop zone within an ability row.
        /// </summary>
        public VisualElement FindDropZone(VisualElement abilityRow)
        {
            if (abilityRow == null) return null;
            return abilityRow.Q<VisualElement>(className: "character-creation-ability-score-drop-zone");
        }

        /// <summary>
        /// Finds the rolled scores container that contains the given element.
        /// </summary>
        public VisualElement FindRolledScoresContainer(VisualElement element)
        {
            return FindAncestorByName(element, "rolled-scores-container");
        }

        /// <summary>
        /// Gets the ability index from an ability row element name.
        /// Returns -1 if not found.
        /// </summary>
        public int GetAbilityIndexFromRow(VisualElement abilityRow)
        {
            if (abilityRow == null) return -1;

            string[] abilityNames = { "str", "dex", "con", "int", "wis", "cha" };
            string rowName = abilityRow.name;

            for (int i = 0; i < abilityNames.Length; i++)
            {
                if (rowName == $"ability-stat-{abilityNames[i]}")
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds the element under the pointer position.
        /// </summary>
        public VisualElement GetElementUnderPointer(Vector2 position)
        {
            if (_root == null || _root.panel == null)
                return null;

            return _root.panel.Pick(position);
        }

        /// <summary>
        /// Checks if an element is the drag preview or a child of it.
        /// </summary>
        public bool IsDragPreviewOrChild(VisualElement element)
        {
            if (element == null) return false;

            if (element.ClassListContains("drag-preview"))
                return true;

            VisualElement current = element.parent;
            while (current != null)
            {
                if (current.ClassListContains("drag-preview"))
                    return true;
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Checks if an element is the dragged element or a child of it.
        /// </summary>
        public bool IsDraggedElementOrChild(VisualElement element, VisualElement draggedElement)
        {
            if (element == null || draggedElement == null) return false;

            if (element == draggedElement)
                return true;

            VisualElement current = element.parent;
            while (current != null)
            {
                if (current == draggedElement)
                    return true;
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Updates visual feedback for drop zones during drag.
        /// </summary>
        public void UpdateDropZoneFeedback(VisualElement highlightedRow)
        {
            if (_abilityScoresGrid == null) return;

            foreach (VisualElement row in _abilityScoresGrid.Children())
            {
                if (row == highlightedRow)
                {
                    row.AddToClassList("drag-over");
                }
                else
                {
                    row.RemoveFromClassList("drag-over");
                }
            }
        }

        /// <summary>
        /// Clears all drop zone visual feedback.
        /// </summary>
        public void ClearDropZoneFeedback()
        {
            if (_abilityScoresGrid != null)
            {
                foreach (VisualElement row in _abilityScoresGrid.Children())
                {
                    row.RemoveFromClassList("drag-over");
                }
            }

            if (_rolledScoresContainer != null)
            {
                _rolledScoresContainer.RemoveFromClassList("drag-over");
            }
        }

        private VisualElement FindAncestorByClass(VisualElement element, string className)
        {
            if (element == null) return null;

            VisualElement current = element;
            int depth = 0;
            const int maxDepth = 20; // Safety limit

            while (current != null && depth < maxDepth)
            {
                if (current.ClassListContains(className))
                    return current;
                current = current.parent;
                depth++;
            }
            return null;
        }

        private VisualElement FindAncestorByName(VisualElement element, string name)
        {
            if (element == null) return null;

            VisualElement current = element;
            while (current != null)
            {
                if (current.name == name)
                    return current;
                current = current.parent;
            }
            return null;
        }
    }
}

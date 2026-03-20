using System;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Knows how stat rows are nested under <c>ability-scores-grid</c>: split hosts (3 + 3) vs legacy flat list.
    /// Keeps traversal logic in one place for views and drag-drop feedback (SRP / DRY).
    /// </summary>
    public static class AbilityScoresGridTraversal
    {
        /// <summary>USS class on each row host; must match CharacterCreationView.uxml / .uss.</summary>
        public const string RowHostUssClass = "character-creation-ability-scores-row";

        /// <summary>
        /// True when the grid’s direct children are row hosts; otherwise direct children are stat rows (legacy).
        /// </summary>
        public static bool UsesRowHostLayout(VisualElement abilityScoresGrid)
        {
            return abilityScoresGrid != null
                && abilityScoresGrid.childCount > 0
                && abilityScoresGrid[0].ClassListContains(RowHostUssClass);
        }

        /// <summary>
        /// Visits each ability stat row (tile) in visual order: primary row host (STR–CON), then secondary (INT–CHA), or legacy flat list.
        /// </summary>
        public static void ForEachStatRow(VisualElement abilityScoresGrid, Action<VisualElement> visitStatRow)
        {
            if (abilityScoresGrid == null || visitStatRow == null)
                return;

            if (UsesRowHostLayout(abilityScoresGrid))
            {
                foreach (VisualElement rowHost in abilityScoresGrid.Children())
                {
                    foreach (VisualElement statRow in rowHost.Children())
                        visitStatRow(statRow);
                }
            }
            else
            {
                foreach (VisualElement statRow in abilityScoresGrid.Children())
                    visitStatRow(statRow);
            }
        }
    }
}

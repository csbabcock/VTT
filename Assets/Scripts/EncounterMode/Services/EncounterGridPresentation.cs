using System.Collections.Generic;
using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Facade over the grid view components (renderer, selector, visualizers). Owns the
    /// enable/disable and visualization wiring so the encounter manager and movement
    /// coordinator never juggle individual MonoBehaviours directly.
    /// </summary>
    public sealed class EncounterGridPresentation
    {
        private readonly IGridRenderer _renderer;
        private readonly GridSelector _selector;
        private readonly GridSelectionVisualizer _selectionVisualizer;
        private readonly GridColumnVisualizer _columnVisualizer;
        private readonly GridReachableCellsVisualizer _reachableVisualizer;

        public EncounterGridPresentation(
            IGridRenderer renderer,
            GridSelector selector,
            GridSelectionVisualizer selectionVisualizer,
            GridColumnVisualizer columnVisualizer,
            GridReachableCellsVisualizer reachableVisualizer)
        {
            _renderer = renderer;
            _selector = selector;
            _selectionVisualizer = selectionVisualizer;
            _columnVisualizer = columnVisualizer;
            _reachableVisualizer = reachableVisualizer;
        }

        public void SetGridVisible(bool visible) => _renderer?.SetVisible(visible);

        /// <summary>Hides the grid and disables selection components at startup.</summary>
        public void InitializeHidden()
        {
            _renderer?.SetVisible(false);
            if (_selector != null)
                _selector.enabled = false;
            if (_selectionVisualizer != null)
                _selectionVisualizer.enabled = false;
        }

        public void EnableSelection(int maxElevation)
        {
            if (_selector != null)
            {
                _selector.SetMaxElevation(maxElevation);
                _selector.enabled = true;
            }

            if (_selectionVisualizer != null)
                _selectionVisualizer.enabled = true;

            if (_columnVisualizer != null)
                _columnVisualizer.enabled = true;

            if (_reachableVisualizer != null)
                _reachableVisualizer.enabled = true;
        }

        public void DisableSelection()
        {
            if (_selector != null)
            {
                _selector.ClearSelection();
                _selector.enabled = false;
            }

            if (_selectionVisualizer != null)
            {
                _selectionVisualizer.HideAllIndicators();
                _selectionVisualizer.enabled = false;
            }

            if (_columnVisualizer != null)
                _columnVisualizer.enabled = false;

            if (_reachableVisualizer != null)
            {
                _reachableVisualizer.ClearReachableCells();
                _reachableVisualizer.enabled = false;
            }
        }

        public void SetMaxElevation(int maxElevation) => _selector?.SetMaxElevation(maxElevation);

        public void UpdateReachableCells(HashSet<GridCell> cells)
        {
            if (_reachableVisualizer != null && _reachableVisualizer.enabled)
                _reachableVisualizer.UpdateReachableCells(cells);
        }

        public void ClearReachableCells() => _reachableVisualizer?.ClearReachableCells();
    }
}

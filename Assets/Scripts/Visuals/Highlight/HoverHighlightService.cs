using UnityEngine;

namespace GameCore.Visuals.Highlight
{
    /// <summary>Tracks one highlighted object and delegates visuals to a presenter.</summary>
    public sealed class HoverHighlightService
    {
        private readonly IEntityHighlightPresenter _presenter;
        private Transform _highlightedRoot;

        public HoverHighlightService(IEntityHighlightPresenter presenter)
        {
            _presenter = presenter ?? throw new System.ArgumentNullException(nameof(presenter));
        }

        public Transform HighlightedRoot => _highlightedRoot;

        public void UpdateHover(Transform hoveredRoot)
        {
            if (_highlightedRoot == hoveredRoot)
                return;

            if (_highlightedRoot != null)
                _presenter.SetHighlighted(_highlightedRoot, false);

            _highlightedRoot = hoveredRoot;

            if (_highlightedRoot != null)
                _presenter.SetHighlighted(_highlightedRoot, true);
        }

        public void Clear()
        {
            if (_highlightedRoot == null)
                return;

            _presenter.SetHighlighted(_highlightedRoot, false);
            _highlightedRoot = null;
        }
    }
}

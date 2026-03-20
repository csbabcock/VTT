using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu.Scrollbars
{
    /// <summary>
    /// Binds USS-driven track/thumb siblings to character-creation <see cref="ScrollView"/>s (native v-scroller hidden in UXML).
    /// </summary>
    public sealed class CharacterCreationPaneScrollBarBinder : ICustomPaneScrollBarBinder
    {
        private const string ScrollRowClass = "character-creation-scroll-with-custom-bar";
        private const string TrackClass = "character-creation-custom-vscroll-track";
        private const string ThumbClass = "character-creation-custom-vscroll-thumb";
        private const string BoundMarker = "cc-vbar";
        private const int LayoutSyncIntervalMs = 16;
        private const float PageScrollViewportFraction = 0.9f;
        private const int PrimaryPointerButtonId = 0;

        public void BindTree(VisualElement root)
        {
            if (root == null)
                return;
            root.Query<ScrollView>().ForEach(TryAttach);
        }

        private static void TryAttach(ScrollView scrollView)
        {
            var parent = scrollView.parent;
            if (parent == null || !parent.ClassListContains(ScrollRowClass))
                return;

            var track = parent.Q<VisualElement>(className: TrackClass);
            var thumb = track?.Q<VisualElement>(className: ThumbClass);
            if (track == null || thumb == null)
                return;

            if (scrollView.userData as string == BoundMarker)
                return;

            var binding = new Binding(scrollView, track, thumb);
            binding.Attach(BoundMarker, LayoutSyncIntervalMs);
        }

        private sealed class Binding
        {
            private readonly ScrollView _scrollView;
            private readonly VisualElement _track;
            private readonly VisualElement _thumb;
            private float _dragOffsetY;

            public Binding(ScrollView scrollView, VisualElement track, VisualElement thumb)
            {
                _scrollView = scrollView;
                _track = track;
                _thumb = thumb;
            }

            public void Attach(string boundMarker, int layoutSyncIntervalMs)
            {
                _scrollView.userData = boundMarker;

                EventCallback<GeometryChangedEvent> onGeometryChanged = _ => Sync();
                _scrollView.contentContainer.RegisterCallback(onGeometryChanged);
                _scrollView.RegisterCallback(onGeometryChanged);
                _track.RegisterCallback(onGeometryChanged);
                _scrollView.schedule.Execute(Sync).Every(layoutSyncIntervalMs);

                _thumb.RegisterCallback<PointerDownEvent>(OnThumbPointerDown);
                _thumb.RegisterCallback<PointerMoveEvent>(OnThumbPointerMove);
                _thumb.RegisterCallback<PointerUpEvent>(OnThumbPointerUp);
                _track.RegisterCallback<PointerDownEvent>(OnTrackPointerDown);

                Sync();
            }

            private void Sync()
            {
                if (_scrollView.parent == null || _scrollView.panel == null)
                    return;

                if (!PaneScrollBarMetrics.TryCompute(_scrollView, _track, out var m))
                {
                    _track.style.display = DisplayStyle.None;
                    return;
                }

                _track.style.display = DisplayStyle.Flex;

                float scrollY = Mathf.Clamp(_scrollView.scrollOffset.y, 0f, m.MaxScroll);
                float ratio = m.MaxScroll > 0.001f ? scrollY / m.MaxScroll : 0f;
                float thumbTop = m.TrackPaddingTop + m.ThumbTravel * ratio;

                _thumb.style.position = Position.Absolute;
                _thumb.style.top = new Length(thumbTop, LengthUnit.Pixel);
                _thumb.style.height = new Length(m.ThumbHeight, LengthUnit.Pixel);
            }

            private void OnThumbPointerDown(PointerDownEvent evt)
            {
                if (evt.button != PrimaryPointerButtonId)
                    return;
                evt.StopPropagation();
                if (!PaneScrollBarMetrics.TryCompute(_scrollView, _track, out var m))
                    return;

                _thumb.CapturePointer(evt.pointerId);
                float scrollY = Mathf.Clamp(_scrollView.scrollOffset.y, 0f, m.MaxScroll);
                float ratio = m.MaxScroll > 0.001f ? scrollY / m.MaxScroll : 0f;
                float thumbTop = m.TrackPaddingTop + m.ThumbTravel * ratio;
                float ptrY = _track.WorldToLocal(evt.position).y;
                _dragOffsetY = ptrY - thumbTop;
            }

            private void OnThumbPointerMove(PointerMoveEvent evt)
            {
                if (!_thumb.HasPointerCapture(evt.pointerId))
                    return;
                if (!PaneScrollBarMetrics.TryCompute(_scrollView, _track, out var m))
                    return;

                float ptrY = _track.WorldToLocal(evt.position).y;
                float desiredThumbTop = Mathf.Clamp(ptrY - _dragOffsetY, m.TrackPaddingTop, m.TrackPaddingTop + m.ThumbTravel);
                float r = m.ThumbTravel > 0.001f ? (desiredThumbTop - m.TrackPaddingTop) / m.ThumbTravel : 0f;
                _scrollView.scrollOffset = new Vector2(_scrollView.scrollOffset.x, r * m.MaxScroll);
                Sync();
            }

            private void OnThumbPointerUp(PointerUpEvent evt)
            {
                if (_thumb.HasPointerCapture(evt.pointerId))
                    _thumb.ReleasePointer(evt.pointerId);
            }

            private void OnTrackPointerDown(PointerDownEvent evt)
            {
                if (evt.button != PrimaryPointerButtonId)
                    return;

                if (ReferenceEquals(evt.target, _thumb))
                    return;

                evt.StopPropagation();
                if (!PaneScrollBarMetrics.TryCompute(_scrollView, _track, out var m))
                    return;

                float localY = _track.WorldToLocal(evt.position).y;
                float scrollY = Mathf.Clamp(_scrollView.scrollOffset.y, 0f, m.MaxScroll);
                float ratio = m.MaxScroll > 0.001f ? scrollY / m.MaxScroll : 0f;
                float thumbTop = m.TrackPaddingTop + m.ThumbTravel * ratio;
                float thumbBottom = thumbTop + m.ThumbHeight;

                float page = m.ViewportHeight * PageScrollViewportFraction;
                if (localY < thumbTop)
                    _scrollView.scrollOffset = new Vector2(_scrollView.scrollOffset.x, Mathf.Max(0f, scrollY - page));
                else if (localY > thumbBottom)
                    _scrollView.scrollOffset = new Vector2(_scrollView.scrollOffset.x, Mathf.Min(m.MaxScroll, scrollY + page));

                Sync();
            }
        }
    }
}

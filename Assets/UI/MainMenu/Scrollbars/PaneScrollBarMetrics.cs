using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu.Scrollbars
{
    /// <summary>
    /// Pure layout math for a ScrollView + external track; no input, no side effects (SRP).
    /// </summary>
    internal readonly struct PaneScrollBarMetrics
    {
        internal const float ThumbMinHeightPx = 28f;
        /// <summary>Matches ScrollerVisibility.Auto: hide when there is effectively nothing to scroll.</summary>
        internal const float AutoHideMaxScrollEpsilon = 0.5f;
        internal const float MinLayoutDimensionPx = 1f;

        public float MaxScroll { get; }
        public float TrackPaddingTop { get; }
        public float TrackInnerHeight { get; }
        public float ThumbHeight { get; }
        public float ThumbTravel { get; }
        public float ViewportHeight { get; }

        private PaneScrollBarMetrics(
            float viewportHeight,
            float maxScroll,
            float trackPaddingTop,
            float trackInnerHeight,
            float thumbHeight,
            float thumbTravel)
        {
            ViewportHeight = viewportHeight;
            MaxScroll = maxScroll;
            TrackPaddingTop = trackPaddingTop;
            TrackInnerHeight = trackInnerHeight;
            ThumbHeight = thumbHeight;
            ThumbTravel = thumbTravel;
        }

        public static bool TryCompute(ScrollView scrollView, VisualElement track, out PaneScrollBarMetrics metrics)
        {
            metrics = default;

            var viewport = scrollView.contentViewport;
            float viewH = viewport.layout.height;
            float contentH = scrollView.contentContainer.layout.height;
            if (viewH <= MinLayoutDimensionPx || contentH <= MinLayoutDimensionPx)
                return false;

            float maxScroll = Mathf.Max(0f, contentH - viewH);
            if (maxScroll <= AutoHideMaxScrollEpsilon)
                return false;

            if (track.panel == null)
                return false;

            float stretchH = scrollView.layout.height;
            if (stretchH <= MinLayoutDimensionPx && scrollView.parent != null && scrollView.parent.layout.height > MinLayoutDimensionPx)
                stretchH = scrollView.parent.layout.height;
            if (stretchH <= MinLayoutDimensionPx)
                return false;

            float padT = track.resolvedStyle.paddingTop;
            float padB = track.resolvedStyle.paddingBottom;
            float marginT = track.resolvedStyle.marginTop;
            float marginB = track.resolvedStyle.marginBottom;
            float trackInnerH = Mathf.Max(0f, stretchH - marginT - marginB - padT - padB); /* symmetric vertical padding */

            float thumbH = Mathf.Clamp(viewH / contentH * trackInnerH, ThumbMinHeightPx, trackInnerH);
            float travel = Mathf.Max(0f, trackInnerH - thumbH);

            metrics = new PaneScrollBarMetrics(viewH, maxScroll, padT, trackInnerH, thumbH, travel);
            return true;
        }
    }
}

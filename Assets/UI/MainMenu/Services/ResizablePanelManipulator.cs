using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu.Services
{
    [Flags]
    public enum ResizeDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8
    }

    public readonly struct PanelResizeChangedEvent
    {
        public VisualElement Panel { get; }
        public float Width { get; }
        public float Height { get; }

        public PanelResizeChangedEvent(VisualElement panel, float width, float height)
        {
            Panel = panel;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Pointer-capturing resize behavior for UI Toolkit panels. Attach it to a resize handle while
    /// passing the panel that should receive style width/height/flex-basis updates.
    /// </summary>
    public sealed class ResizablePanelManipulator : Manipulator
    {
        private const int PrimaryPointerButtonId = 0;

        private readonly VisualElement _panel;
        private readonly ResizeDirection _direction;
        private readonly VisualElement _boundsContainer;
        private readonly Action<PanelResizeChangedEvent> _onResizeChanged;

        private int _pointerId = -1;
        private bool _isResizing;
        private Vector3 _startPointerPosition;
        private float _startWidth;
        private float _startHeight;
        private float _startLeft;
        private float _startTop;

        public float MinWidth { get; }
        public float MaxWidth { get; }
        public float MinHeight { get; }
        public float MaxHeight { get; }

        public ResizablePanelManipulator(
            VisualElement panel,
            ResizeDirection direction,
            float minWidth = 0f,
            float maxWidth = float.PositiveInfinity,
            float minHeight = 0f,
            float maxHeight = float.PositiveInfinity,
            VisualElement boundsContainer = null,
            Action<PanelResizeChangedEvent> onResizeChanged = null)
        {
            _panel = panel;
            _direction = direction;
            MinWidth = minWidth;
            MaxWidth = maxWidth;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
            _boundsContainer = boundsContainer;
            _onResizeChanged = onResizeChanged;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != PrimaryPointerButtonId || _panel == null || _direction == ResizeDirection.None)
                return;

            evt.StopImmediatePropagation();
            _pointerId = evt.pointerId;
            _isResizing = true;
            _startPointerPosition = evt.position;
            _startWidth = Mathf.Max(0f, _panel.resolvedStyle.width);
            _startHeight = Mathf.Max(0f, _panel.resolvedStyle.height);
            _startLeft = _panel.resolvedStyle.left;
            _startTop = _panel.resolvedStyle.top;

            target.CapturePointer(_pointerId);
            target.AddToClassList("resize-handle--active");
            _panel.AddToClassList("panel--resizing");
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isResizing || !target.HasPointerCapture(evt.pointerId))
                return;

            evt.StopImmediatePropagation();
            Vector3 delta = evt.position - _startPointerPosition;
            float width = _startWidth;
            float height = _startHeight;

            if ((_direction & ResizeDirection.Right) != 0)
                width = _startWidth + delta.x;
            else if ((_direction & ResizeDirection.Left) != 0)
                width = _startWidth - delta.x;

            if ((_direction & ResizeDirection.Bottom) != 0)
                height = _startHeight + delta.y;
            else if ((_direction & ResizeDirection.Top) != 0)
                height = _startHeight - delta.y;

            width = ClampWidth(width);
            height = ClampHeight(height);
            ApplySize(width, height);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            EndResize(evt.pointerId);
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            EndResize(evt.pointerId);
        }

        private void EndResize(int pointerId)
        {
            if (!_isResizing || pointerId != _pointerId)
                return;

            if (target.HasPointerCapture(pointerId))
                target.ReleasePointer(pointerId);

            target.RemoveFromClassList("resize-handle--active");
            _panel.RemoveFromClassList("panel--resizing");
            _pointerId = -1;
            _isResizing = false;
        }

        private float ClampWidth(float width)
        {
            float max = MaxWidth;
            if (_boundsContainer != null && _boundsContainer.resolvedStyle.width > 0f)
                max = Mathf.Min(max, _boundsContainer.resolvedStyle.width);

            return Mathf.Clamp(width, MinWidth, max);
        }

        private float ClampHeight(float height)
        {
            float max = MaxHeight;
            if (_boundsContainer != null && _boundsContainer.resolvedStyle.height > 0f)
                max = Mathf.Min(max, _boundsContainer.resolvedStyle.height);

            return Mathf.Clamp(height, MinHeight, max);
        }

        private void ApplySize(float width, float height)
        {
            bool absolute = _panel.resolvedStyle.position == Position.Absolute;

            if ((_direction & (ResizeDirection.Left | ResizeDirection.Right)) != 0)
            {
                _panel.style.width = width;
                _panel.style.flexBasis = width;
                if (absolute && (_direction & ResizeDirection.Left) != 0)
                    _panel.style.left = _startLeft + (_startWidth - width);
            }

            if ((_direction & (ResizeDirection.Top | ResizeDirection.Bottom)) != 0)
            {
                _panel.style.height = height;
                _panel.style.flexBasis = height;
                if (absolute && (_direction & ResizeDirection.Top) != 0)
                    _panel.style.top = _startTop + (_startHeight - height);
            }

            _onResizeChanged?.Invoke(new PanelResizeChangedEvent(_panel, width, height));
        }
    }
}

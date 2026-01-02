using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Handles tab carousel navigation and display.
    /// Follows Single Responsibility Principle - only handles tab carousel functionality.
    /// </summary>
    public class TabCarouselView
    {
        #region Constants
        private const int VISIBLE_TAB_COUNT = 4;
        private const float DRAG_THRESHOLD = 10f;
        private const float TAB_WIDTH = 100f;
        private const int DRAG_CLICK_DELAY_MS = 50;
        #endregion

        #region Private Fields
        private VisualElement _root;
        private Button[] _tabButtons;
        private VisualElement _tabsContainer;
        private VisualElement _tabsWrapper;
        private Button _tabNavLeft;
        private Button _tabNavRight;
        private int _currentTabOffset = 0;
        
        // Drag functionality
        private bool _isDragging = false;
        private float _dragStartX = 0f;
        private float _dragStartOffset = 0f;
        #endregion

        #region Events
        /// <summary>
        /// Fired when a tab button is clicked. Parameter is the tab index.
        /// </summary>
#pragma warning disable CS0067 // Event is never used - subscribed to from InGameUIView
        public event System.Action<int> TabClicked;
#pragma warning restore CS0067
        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the tab carousel view.
        /// </summary>
        public void Initialize(VisualElement root, Button[] tabButtons)
        {
            _root = root;
            _tabButtons = tabButtons;

            // Wire up carousel navigation
            _tabsContainer = _root.Q<VisualElement>("charsheet-tabs-container");
            _tabsWrapper = _root.Q<VisualElement>("charsheet-tabs-wrapper");
            _tabNavLeft = _root.Q<Button>("tab-nav-left");
            _tabNavRight = _root.Q<Button>("tab-nav-right");

            if (_tabNavLeft != null)
            {
                _tabNavLeft.clicked += OnTabNavLeftClicked;
                _tabNavLeft.pickingMode = PickingMode.Position;
            }

            if (_tabNavRight != null)
            {
                _tabNavRight.clicked += OnTabNavRightClicked;
                _tabNavRight.pickingMode = PickingMode.Position;
            }

            // Wire up drag functionality for carousel
            if (_tabsWrapper != null)
            {
                _tabsWrapper.RegisterCallback<PointerDownEvent>(OnTabsPointerDown);
                _tabsWrapper.RegisterCallback<PointerMoveEvent>(OnTabsPointerMove);
                _tabsWrapper.RegisterCallback<PointerUpEvent>(OnTabsPointerUp);
                _tabsWrapper.RegisterCallback<PointerLeaveEvent>(OnTabsPointerLeave);
                _tabsWrapper.pickingMode = PickingMode.Position;
            }

            UpdateTabCarousel();
        }

        /// <summary>
        /// Ensures the specified tab is visible in the carousel by adjusting the offset.
        /// </summary>
        public void EnsureTabVisible(int tabIndex)
        {
            if (tabIndex < _currentTabOffset)
            {
                _currentTabOffset = tabIndex;
            }
            else if (tabIndex >= _currentTabOffset + VISIBLE_TAB_COUNT)
            {
                _currentTabOffset = Mathf.Max(0, tabIndex - VISIBLE_TAB_COUNT + 1);
            }

            UpdateTabCarousel();
        }

        /// <summary>
        /// Gets whether dragging is currently active (to prevent accidental tab clicks).
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public void Cleanup()
        {
            if (_tabNavLeft != null)
            {
                _tabNavLeft.clicked -= OnTabNavLeftClicked;
            }

            if (_tabNavRight != null)
            {
                _tabNavRight.clicked -= OnTabNavRightClicked;
            }

            if (_tabsWrapper != null)
            {
                _tabsWrapper.UnregisterCallback<PointerDownEvent>(OnTabsPointerDown);
                _tabsWrapper.UnregisterCallback<PointerMoveEvent>(OnTabsPointerMove);
                _tabsWrapper.UnregisterCallback<PointerUpEvent>(OnTabsPointerUp);
                _tabsWrapper.UnregisterCallback<PointerLeaveEvent>(OnTabsPointerLeave);
            }
        }

        #endregion

        #region Private Methods

        private void UpdateTabCarousel()
        {
            if (_tabButtons == null || _tabNavLeft == null || _tabNavRight == null)
                return;

            int totalTabs = _tabButtons.Length;
            
            // If we have fewer tabs than visible, show all and disable navigation
            if (totalTabs <= VISIBLE_TAB_COUNT)
            {
                _tabNavLeft.SetEnabled(false);
                _tabNavRight.SetEnabled(false);
                
                for (int i = 0; i < totalTabs; i++)
                {
                    if (_tabButtons[i] != null)
                    {
                        _tabButtons[i].style.display = DisplayStyle.Flex;
                    }
                }
                return;
            }

            // Update navigation button states (wrap around)
            _tabNavLeft.SetEnabled(true);
            _tabNavRight.SetEnabled(true);

            // Show visible tabs - wrap around if needed
            for (int i = 0; i < totalTabs; i++)
            {
                if (_tabButtons[i] != null)
                {
                    bool isVisible = false;
                    
                    if (_currentTabOffset + VISIBLE_TAB_COUNT <= totalTabs)
                    {
                        isVisible = i >= _currentTabOffset && i < _currentTabOffset + VISIBLE_TAB_COUNT;
                    }
                    else
                    {
                        int overflow = (_currentTabOffset + VISIBLE_TAB_COUNT) - totalTabs;
                        isVisible = (i >= _currentTabOffset) || (i < overflow);
                    }
                    
                    _tabButtons[i].style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void OnTabNavLeftClicked()
        {
            if (_tabButtons == null || _tabButtons.Length == 0)
                return;
                
            _currentTabOffset--;
            
            int maxOffset = Mathf.Max(0, _tabButtons.Length - VISIBLE_TAB_COUNT);
            if (_currentTabOffset < 0)
            {
                _currentTabOffset = maxOffset;
            }
            
            UpdateTabCarousel();
        }

        private void OnTabNavRightClicked()
        {
            if (_tabButtons == null || _tabButtons.Length == 0)
                return;
                
            _currentTabOffset++;
            
            int maxOffset = Mathf.Max(0, _tabButtons.Length - VISIBLE_TAB_COUNT);
            if (_currentTabOffset > maxOffset)
            {
                _currentTabOffset = 0;
            }
            
            UpdateTabCarousel();
        }

        private void OnTabsPointerDown(PointerDownEvent evt)
        {
            _isDragging = true;
            _dragStartX = evt.position.x;
            _dragStartOffset = _currentTabOffset;
            _tabsWrapper.CapturePointer(evt.pointerId);
        }

        private void OnTabsPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging)
                return;

            float deltaX = evt.position.x - _dragStartX;
            float dragDistance = Mathf.Abs(deltaX);

            if (dragDistance > DRAG_THRESHOLD)
            {
                int tabsToScroll = Mathf.RoundToInt(deltaX / TAB_WIDTH);
                int newOffset = Mathf.RoundToInt(_dragStartOffset - tabsToScroll);
                newOffset = Mathf.Clamp(newOffset, 0, Mathf.Max(0, _tabButtons.Length - VISIBLE_TAB_COUNT));

                if (newOffset != _currentTabOffset)
                {
                    _currentTabOffset = newOffset;
                    UpdateTabCarousel();
                }
            }
        }

        private void OnTabsPointerUp(PointerUpEvent evt)
        {
            if (_isDragging)
            {
                _tabsWrapper.schedule.Execute(() =>
                {
                    _isDragging = false;
                }).ExecuteLater(DRAG_CLICK_DELAY_MS);
                _tabsWrapper.ReleasePointer(evt.pointerId);
            }
        }

        private void OnTabsPointerLeave(PointerLeaveEvent evt)
        {
            if (_isDragging)
            {
                _isDragging = false;
            }
        }

        #endregion
    }
}


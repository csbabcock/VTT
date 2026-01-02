using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Handles button navigation and selection for keyboard navigation.
    /// Follows Single Responsibility Principle - only handles button navigation.
    /// </summary>
    public class ButtonNavigationView
    {
        #region Private Fields
        private VisualElement _root;
        private UIDocument _uiDocument;
        private VisualElement[] _characterSheetTabs;
        private ScrollView _characterSheetScrollView;
        private Button[] _tabButtons;
        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the button navigation view.
        /// </summary>
        public void Initialize(UIDocument uiDocument, VisualElement root, VisualElement[] characterSheetTabs, 
            ScrollView characterSheetScrollView, Button[] tabButtons)
        {
            _uiDocument = uiDocument;
            _root = root;
            _characterSheetTabs = characterSheetTabs;
            _characterSheetScrollView = characterSheetScrollView;
            _tabButtons = tabButtons;
        }

        /// <summary>
        /// Gets all focusable buttons in the specified tab content.
        /// </summary>
        public List<Button> GetButtonsInTab(int tabIndex)
        {
            var buttons = new List<Button>();
            
            if (_characterSheetTabs == null)
                return buttons;

            if (tabIndex < 0 || tabIndex >= _characterSheetTabs.Length)
                return buttons;

            var currentTab = _characterSheetTabs[tabIndex];
            if (currentTab == null)
                return buttons;

            FindButtonsRecursive(currentTab, buttons);
            
            return buttons;
        }

        /// <summary>
        /// Gets the index of the button currently under the mouse cursor.
        /// Returns -1 if no button is hovered.
        /// </summary>
        public int GetHoveredButtonIndex(int tabIndex)
        {
            if (_uiDocument == null)
                return -1;

            var buttons = GetButtonsInTab(tabIndex);
            if (buttons.Count == 0)
                return -1;

            Vector2? panelSpacePos = GetMousePositionInPanelSpace();
            if (!panelSpacePos.HasValue)
                return -1;

            return FindButtonIndexAtPosition(buttons, panelSpacePos.Value);
        }

        /// <summary>
        /// Sets the selected button index and updates visual feedback.
        /// </summary>
        public void SetSelectedButtonIndex(int tabIndex, int buttonIndex)
        {
            var buttons = GetButtonsInTab(tabIndex);
            if (buttons.Count == 0)
                return;

            buttonIndex = Mathf.Clamp(buttonIndex, 0, buttons.Count - 1);

            // Remove highlight from all buttons in this tab
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.RemoveFromClassList("keyboard-selected");
                }
            }

            // Add highlight to selected button
            if (buttonIndex >= 0 && buttonIndex < buttons.Count && buttons[buttonIndex] != null)
            {
                buttons[buttonIndex].AddToClassList("keyboard-selected");
                ScrollButtonIntoView(buttons[buttonIndex]);
            }
        }

        /// <summary>
        /// Gets the currently selected button and triggers its click event.
        /// </summary>
        public bool ActivateSelectedButton(int tabIndex, int buttonIndex)
        {
            var buttons = GetButtonsInTab(tabIndex);
            if (buttonIndex < 0 || buttonIndex >= buttons.Count)
                return false;

            var button = buttons[buttonIndex];
            if (button != null && button.enabledInHierarchy)
            {
                if (!button.focusable)
                {
                    button.focusable = true;
                }
                
                button.Focus();
                
                using (var submitEvent = NavigationSubmitEvent.GetPooled())
                {
                    submitEvent.target = button;
                    button.SendEvent(submitEvent);
                }
                
                using (var clickEvent = ClickEvent.GetPooled())
                {
                    clickEvent.target = button;
                    button.SendEvent(clickEvent);
                }
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears the keyboard selection highlight from all buttons in the specified tab.
        /// </summary>
        public void ClearButtonSelection(int tabIndex)
        {
            var buttons = GetButtonsInTab(tabIndex);
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.RemoveFromClassList("keyboard-selected");
                }
            }
        }

        /// <summary>
        /// Scrolls the character sheet tab content up or down.
        /// </summary>
        public void ScrollTabContent(float scrollAmount)
        {
            if (_characterSheetScrollView == null)
                return;

            Vector2 currentOffset = _characterSheetScrollView.scrollOffset;
            float newY = currentOffset.y + scrollAmount;
            
            float maxScroll = _characterSheetScrollView.contentContainer.layout.height - _characterSheetScrollView.contentViewport.layout.height;
            newY = Mathf.Clamp(newY, 0f, Mathf.Max(0f, maxScroll));
            
            _characterSheetScrollView.scrollOffset = new Vector2(currentOffset.x, newY);
        }

        #endregion

        #region Private Methods

        private Vector2? GetMousePositionInPanelSpace()
        {
            Vector2? screenPosition = GetMouseScreenPosition();
            if (!screenPosition.HasValue)
                return null;

            float screenHeight = Screen.height;
            return new Vector2(
                screenPosition.Value.x,
                screenHeight - screenPosition.Value.y
            );
        }

        private Vector2? GetMouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null)
                return null;
            return mouse.position.ReadValue();
#else
            return Input.mousePosition;
#endif
        }

        private int FindButtonIndexAtPosition(List<Button> buttons, Vector2 panelSpacePos)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (!IsButtonHoverable(button))
                    continue;

                if (IsPositionOverButton(button, panelSpacePos))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsButtonHoverable(Button button)
        {
            if (button == null || !button.enabledInHierarchy)
                return false;

            if (button.resolvedStyle.display == DisplayStyle.None ||
                button.resolvedStyle.visibility == Visibility.Hidden)
                return false;

            return true;
        }

        private bool IsPositionOverButton(Button button, Vector2 panelSpacePos)
        {
            Rect buttonRect = button.worldBound;
            return buttonRect.Contains(panelSpacePos);
        }

        private void FindButtonsRecursive(VisualElement element, List<Button> buttons)
        {
            if (element == null)
                return;

            if (element.resolvedStyle.display == DisplayStyle.None || 
                !element.enabledInHierarchy ||
                element.resolvedStyle.visibility == Visibility.Hidden)
            {
                return;
            }

            if (element is Button button)
            {
                if (button != null && 
                    !IsTabButton(button) &&
                    button.enabledInHierarchy &&
                    element.resolvedStyle.display == DisplayStyle.Flex)
                {
                    buttons.Add(button);
                }
            }

            foreach (var child in element.Children())
            {
                FindButtonsRecursive(child, buttons);
            }
        }

        private bool IsTabButton(Button button)
        {
            if (_tabButtons == null)
                return false;

            foreach (var tabButton in _tabButtons)
            {
                if (tabButton == button)
                    return true;
            }
            return false;
        }

        private void ScrollButtonIntoView(Button button)
        {
            if (_characterSheetScrollView == null || button == null)
                return;

            Rect buttonRect = button.worldBound;
            Rect scrollViewRect = _characterSheetScrollView.worldBound;

            if (buttonRect.yMin < scrollViewRect.yMin)
            {
                float scrollAmount = scrollViewRect.yMin - buttonRect.yMin + 10f;
                ScrollTabContent(-scrollAmount);
            }
            else if (buttonRect.yMax > scrollViewRect.yMax)
            {
                float scrollAmount = buttonRect.yMax - scrollViewRect.yMax + 10f;
                ScrollTabContent(scrollAmount);
            }
        }

        #endregion
    }
}


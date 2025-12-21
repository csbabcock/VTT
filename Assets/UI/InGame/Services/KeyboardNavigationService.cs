using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameCore.UI.InGame.Services
{
    /// <summary>
    /// Service responsible for handling keyboard navigation in the UI.
    /// Follows Single Responsibility Principle by isolating keyboard navigation logic.
    /// </summary>
    public class KeyboardNavigationService
    {
        #region Constants
        private const float KEY_REPEAT_DELAY = 0.15f; // Initial delay before repeat starts
        private const float KEY_REPEAT_INTERVAL = 0.05f; // Interval between repeats
        private const float MOUSE_MOVEMENT_THRESHOLD = 2f; // pixels
        #endregion

        #region Private Fields
        private int _selectedButtonIndex = -1; // -1 means no selection
        private float _keyRepeatTimer = 0f;
        private bool _isKeyHeld = false;
        private bool _wasKeyPressedThisFrame = false;
        
        // Mouse tracking for clearing keyboard selection
        private Vector2 _lastMousePosition;
        private bool _hasInitializedMousePosition = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the currently selected button index. Returns -1 if no button is selected.
        /// </summary>
        public int SelectedButtonIndex => _selectedButtonIndex;

        /// <summary>
        /// Gets whether a button is currently selected.
        /// </summary>
        public bool HasSelection => _selectedButtonIndex >= 0;
        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the keyboard navigation state.
        /// </summary>
        public void Reset()
        {
            _selectedButtonIndex = -1;
            _keyRepeatTimer = 0f;
            _isKeyHeld = false;
            _wasKeyPressedThisFrame = false;
            _hasInitializedMousePosition = false;
        }

        /// <summary>
        /// Handles mouse movement to clear keyboard selection when mouse is used.
        /// </summary>
        /// <param name="isCharacterSheetOpen">Whether the character sheet is currently open.</param>
        /// <param name="isMouseOverUI">Whether the mouse is currently over the UI.</param>
        /// <returns>True if keyboard selection was cleared due to mouse movement.</returns>
        public bool HandleMouseMovement(bool isCharacterSheetOpen, bool isMouseOverUI)
        {
            if (!isCharacterSheetOpen)
            {
                _hasInitializedMousePosition = false;
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            Vector2 currentMousePosition = mouse.position.ReadValue();

            // Initialize mouse position on first frame
            if (!_hasInitializedMousePosition)
            {
                _lastMousePosition = currentMousePosition;
                _hasInitializedMousePosition = true;
                return false;
            }

            // Check if mouse has moved (with a small threshold to avoid clearing on tiny movements)
            float mouseMovement = Vector2.Distance(currentMousePosition, _lastMousePosition);
            
            if (mouseMovement > MOUSE_MOVEMENT_THRESHOLD && isMouseOverUI)
            {
                // Mouse has moved over UI - clear keyboard selection
                if (_selectedButtonIndex >= 0)
                {
                    _selectedButtonIndex = -1;
                    _lastMousePosition = currentMousePosition;
                    return true;
                }
            }

            _lastMousePosition = currentMousePosition;
#endif
            return false;
        }

        /// <summary>
        /// Handles keyboard input for button navigation.
        /// </summary>
        /// <param name="buttonCount">The number of buttons available for navigation.</param>
        /// <param name="navigateUp">Whether the up navigation key is pressed.</param>
        /// <param name="navigateDown">Whether the down navigation key is pressed.</param>
        /// <returns>The new selected button index, or -1 if no change occurred.</returns>
        public int HandleButtonNavigation(int buttonCount, bool navigateUp, bool navigateDown)
        {
            if (buttonCount <= 0)
                return -1;

            bool shouldProcess = false;

            if (navigateUp || navigateDown)
            {
                if (!_isKeyHeld)
                {
                    // First press - process immediately
                    _wasKeyPressedThisFrame = true;
                    _isKeyHeld = true;
                    _keyRepeatTimer = 0f;
                    shouldProcess = true;
                }
                else
                {
                    // Key is held - check if we should repeat
                    _keyRepeatTimer += Time.deltaTime;
                    
                    if (_wasKeyPressedThisFrame)
                    {
                        // After initial press, wait for repeat delay
                        if (_keyRepeatTimer >= KEY_REPEAT_DELAY)
                        {
                            _wasKeyPressedThisFrame = false;
                            _keyRepeatTimer = 0f;
                            shouldProcess = true;
                        }
                    }
                    else
                    {
                        // After repeat delay, repeat at interval
                        if (_keyRepeatTimer >= KEY_REPEAT_INTERVAL)
                        {
                            _keyRepeatTimer = 0f;
                            shouldProcess = true;
                        }
                    }
                }
                
                if (shouldProcess)
                {
                    int previousIndex = _selectedButtonIndex;
                    
                    if (navigateUp)
                    {
                        // Move selection up
                        if (_selectedButtonIndex < 0)
                            _selectedButtonIndex = buttonCount - 1; // Start from last button
                        else
                            _selectedButtonIndex = (_selectedButtonIndex - 1 + buttonCount) % buttonCount;
                    }
                    else if (navigateDown)
                    {
                        // Move selection down
                        if (_selectedButtonIndex < 0)
                            _selectedButtonIndex = 0; // Start from first button
                        else
                            _selectedButtonIndex = (_selectedButtonIndex + 1) % buttonCount;
                    }

                    // Return new index if it changed
                    return (_selectedButtonIndex != previousIndex) ? _selectedButtonIndex : -1;
                }
            }
            else
            {
                // Key released
                _isKeyHeld = false;
                _wasKeyPressedThisFrame = false;
                _keyRepeatTimer = 0f;
            }

            return -1; // No change
        }

        /// <summary>
        /// Sets the selected button index.
        /// </summary>
        /// <param name="index">The button index to select. Use -1 to clear selection.</param>
        public void SetSelectedButtonIndex(int index)
        {
            _selectedButtonIndex = index;
        }

        /// <summary>
        /// Resets the key repeat state (useful when character sheet closes).
        /// </summary>
        public void ResetKeyState()
        {
            _isKeyHeld = false;
            _wasKeyPressedThisFrame = false;
            _keyRepeatTimer = 0f;
        }
        #endregion
    }
}


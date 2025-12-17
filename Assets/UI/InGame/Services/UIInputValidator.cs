using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace GameCore.UI.InGame.Services
{
    /// <summary>
    /// Validates and configures UI input system components.
    /// Follows Single Responsibility Principle by isolating input validation logic.
    /// </summary>
    public static class UIInputValidator
    {
        /// <summary>
        /// Validates that the EventSystem and InputSystemUIInputModule are properly configured.
        /// Ensures the UI action map exists and is enabled.
        /// </summary>
        /// <returns>True if input system is properly configured, false otherwise.</returns>
        public static bool ValidateInputSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("UIInputValidator: No EventSystem found in scene! UI Toolkit requires an EventSystem with InputSystemUIInputModule to receive input.");
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                Debug.LogWarning("UIInputValidator: InputSystemUIInputModule not found on EventSystem! UI Toolkit may not receive input. Add it to enable mouse/pointer input.");
                return false;
            }

            var actionsAsset = inputModule.actionsAsset;
            if (actionsAsset == null)
            {
                Debug.LogError("UIInputValidator: InputSystemUIInputModule has no Actions Asset assigned! UI input won't work. " +
                    "Please assign an Input Actions asset with a 'UI' action map to the Actions Asset field in the Inspector.");
                return false;
            }

            var uiActionMap = actionsAsset.FindActionMap("UI");
            if (uiActionMap == null)
            {
                Debug.LogError($"UIInputValidator: UI action map not found in Actions Asset '{actionsAsset.name}'! " +
                    $"InputSystemUIInputModule won't work. The Actions Asset must contain a 'UI' action map with 'Point', 'Click', 'Navigate', etc.");
                return false;
            }

            // Verify UI action map is enabled
            // NOTE: You should enable the UI action map in the Input Actions asset (PlayerInput.inputactions) 
            // in the Unity Inspector. Select the asset, find the "UI" action map, and ensure it's enabled.
            // This ensures UI Toolkit can receive mouse/pointer input.
            if (!uiActionMap.enabled)
            {
                Debug.LogWarning($"UIInputValidator: UI action map in '{actionsAsset.name}' is disabled! " +
                    "Please enable it in the Input Actions asset Inspector. " +
                    "Select 'PlayerInput.inputactions' → Find 'UI' action map → Enable it. " +
                    "Enabling it now as a fallback, but you should enable it in the asset to avoid this warning.");
                uiActionMap.Enable();
            }

            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Validates that a UIDocument has proper Panel Settings configured.
        /// </summary>
        /// <param name="uiDocument">The UIDocument to validate.</param>
        /// <returns>True if Panel Settings are configured, false otherwise.</returns>
        public static bool ValidateUIDocument(UnityEngine.UIElements.UIDocument uiDocument)
        {
            if (uiDocument == null)
            {
                Debug.LogError("UIInputValidator: UIDocument is null!");
                return false;
            }

            if (uiDocument.panelSettings == null)
            {
                Debug.LogWarning("UIInputValidator: UIDocument has no Panel Settings assigned. UI input may not work.");
                return false;
            }

            return true;
        }
    }
}


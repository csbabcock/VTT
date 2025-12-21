using UnityEngine;
using GameCore.UI.InGame;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Service for checking UI interaction state.
    /// Centralizes UI blocking logic to prevent code duplication (DRY principle).
    /// </summary>
    public class UIInteractionService
    {
        private static UIInteractionService _instance;
        private InGameUIView _inGameUIView;

        /// <summary>
        /// Singleton instance for easy access throughout the codebase.
        /// </summary>
        public static UIInteractionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UIInteractionService();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initializes the service with the UI view reference.
        /// Should be called during initialization.
        /// </summary>
        public void Initialize(InGameUIView uiView)
        {
            _inGameUIView = uiView;
        }

        /// <summary>
        /// Checks if the mouse is currently over the character sheet UI.
        /// Returns false if UI view is not initialized or character sheet is not open.
        /// </summary>
        public bool IsMouseOverCharacterSheet()
        {
            if (_inGameUIView == null)
                return false;

            return _inGameUIView.IsMouseOverCharacterSheet();
        }

        /// <summary>
        /// Checks if the character sheet is currently open.
        /// </summary>
        public bool IsCharacterSheetOpen()
        {
            if (_inGameUIView == null)
                return false;

            return _inGameUIView.IsCharacterSheetOpen();
        }

        /// <summary>
        /// Checks if grid input should be blocked (mouse over UI).
        /// </summary>
        public bool ShouldBlockGridInput()
        {
            return IsMouseOverCharacterSheet();
        }

        /// <summary>
        /// Checks if camera input should be blocked (mouse over UI).
        /// </summary>
        public bool ShouldBlockCameraInput()
        {
            return IsMouseOverCharacterSheet();
        }
    }
}


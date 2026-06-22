using GameCore.UI.InGame;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Default <see cref="IUIInputGate"/> backed by the in-game UI view. Centralizes UI
    /// blocking logic (DRY). Registered with <see cref="UIInputGateLocator"/> by the
    /// in-game UI presenter instead of being accessed as a global singleton.
    /// </summary>
    public class UIInteractionService : IUIInputGate
    {
        private InGameUIView _inGameUIView;

        public UIInteractionService(InGameUIView uiView = null)
        {
            _inGameUIView = uiView;
        }

        /// <summary>Sets or updates the UI view reference.</summary>
        public void Initialize(InGameUIView uiView)
        {
            _inGameUIView = uiView;
        }

        public bool IsMouseOverCharacterSheet()
        {
            return _inGameUIView != null && _inGameUIView.IsMouseOverCharacterSheet();
        }

        public bool IsCharacterSheetOpen()
        {
            return _inGameUIView != null && _inGameUIView.IsCharacterSheetOpen();
        }

        public bool ShouldBlockInput()
        {
            return IsMouseOverCharacterSheet();
        }
    }
}

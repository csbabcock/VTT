using GameCore.UI.InGame;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Adapts encounter movement state to the in-game UI (speed display, movement button,
    /// character sheet visibility). Keeps UI null-guards and lookups out of the manager.
    /// </summary>
    public sealed class EncounterUIAdapter
    {
        private readonly InGameUIPresenter _presenter;

        public EncounterUIAdapter(InGameUIPresenter presenter)
        {
            _presenter = presenter;
        }

        public void UpdateSpeedDisplay(int remainingFeet, int maxFeet)
        {
            if (_presenter?.View != null)
                _presenter.View.UpdateSpeedDisplay(remainingFeet, maxFeet);
        }

        public void UpdateMovementButtonState(bool isMovementModeActive)
        {
            _presenter?.View?.UpdateMovementButtonState(isMovementModeActive);
        }

        public void SetCharacterSheetOpen(bool open)
        {
            _presenter?.Model?.SetCharacterSheetOpen(open);
        }
    }
}

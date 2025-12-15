using System;
using GameCore.UI;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Immutable snapshot of the main menu state.
    /// Extend this with more fields as the menu grows (selected option, submenus, etc.).
    /// </summary>
    public struct MainMenuState
    {
        public bool IsInteractable;
    }

    /// <summary>
    /// Model for the main menu. Holds simple state such as whether the menu is interactable
    /// and raises a single StateChanged event whenever the state updates.
    /// </summary>
    public class MainMenuModel : IUIModel<MainMenuState>
    {
        /// <summary>
        /// Current state snapshot.
        /// </summary>
        public MainMenuState State { get; private set; }

        /// <summary>
        /// Raised whenever the model state changes.
        /// </summary>
        public event Action<MainMenuState> StateChanged;

        public bool IsInteractable => State.IsInteractable;

        public MainMenuModel()
        {
            State = new MainMenuState
            {
                IsInteractable = true
            };
        }

        public void SetInteractable(bool value)
        {
            if (IsInteractable == value)
                return;

            State = new MainMenuState
            {
                IsInteractable = value
            };

            StateChanged?.Invoke(State);
        }
    }
}



using System;
using GameCore.UI;
using UnityEngine;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Immutable snapshot of the in-game UI state.
    /// Extend this with additional HUD data (health, stamina, inventory, etc.)
    /// as the UI grows.
    /// </summary>
    public struct InGameUIState
    {
        public bool IsCharacterSheetOpen;
        public int CharacterSheetTabIndex; // 0 = Overview, 1 = Skills, 2 = Actions, 3 = Spells, 4 = Inventory, 5 = Features, 6 = Rest
    }

    /// <summary>
    /// Model for in-game UI. Holds UI-related state and raises a single
    /// StateChanged event whenever its state is updated.
    /// </summary>
    public class InGameUIModel : IUIModel<InGameUIState>
    {
        #region Constants
        /// <summary>
        /// Maximum tab index (0-based). Tabs: 0 = Overview, 1 = Skills, 2 = Actions, 3 = Spells, 4 = Inventory, 5 = Features, 6 = Rest
        /// </summary>
        public const int MAX_TAB_INDEX = 6;
        #endregion

        #region Properties
        /// <summary>
        /// Current state snapshot.
        /// </summary>
        public InGameUIState State { get; private set; }

        /// <summary>
        /// Raised whenever the model state changes.
        /// </summary>
        public event Action<InGameUIState> StateChanged;

        public bool IsCharacterSheetOpen => State.IsCharacterSheetOpen;
        #endregion

        public InGameUIModel()
        {
            // Initialize with default state.
            State = new InGameUIState
            {
                IsCharacterSheetOpen = false,
                CharacterSheetTabIndex = 0 // Start on Overview tab
            };
        }

        public void ToggleCharacterSheet()
        {
            SetCharacterSheetOpen(!IsCharacterSheetOpen);
        }

        public void SetCharacterSheetOpen(bool isOpen)
        {
            if (IsCharacterSheetOpen == isOpen)
                return;

            State = new InGameUIState
            {
                IsCharacterSheetOpen = isOpen,
                CharacterSheetTabIndex = State.CharacterSheetTabIndex
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Navigate to the next character sheet tab.
        /// </summary>
        public void NextTab()
        {
            int newTab = Mathf.Min(State.CharacterSheetTabIndex + 1, MAX_TAB_INDEX);
            SetTab(newTab);
        }

        /// <summary>
        /// Navigate to the previous character sheet tab.
        /// </summary>
        public void PreviousTab()
        {
            int newTab = Mathf.Max(State.CharacterSheetTabIndex - 1, 0);
            SetTab(newTab);
        }

        /// <summary>
        /// Set the current character sheet tab index.
        /// </summary>
        public void SetTab(int tabIndex)
        {
            if (State.CharacterSheetTabIndex == tabIndex)
                return;

            State = new InGameUIState
            {
                IsCharacterSheetOpen = State.IsCharacterSheetOpen,
                CharacterSheetTabIndex = tabIndex
            };

            StateChanged?.Invoke(State);
        }
    }
}



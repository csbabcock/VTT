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
        public int CharacterSheetPageIndex; // 0 = character info, 1 = ability scores, 2 = skills, 3 = actions
    }

    /// <summary>
    /// Model for in-game UI. Holds UI-related state and raises a single
    /// StateChanged event whenever its state is updated.
    /// </summary>
    public class InGameUIModel : IUIModel<InGameUIState>
    {
        /// <summary>
        /// Current state snapshot.
        /// </summary>
        public InGameUIState State { get; private set; }

        /// <summary>
        /// Raised whenever the model state changes.
        /// </summary>
        public event Action<InGameUIState> StateChanged;

        public bool IsCharacterSheetOpen => State.IsCharacterSheetOpen;

        public InGameUIModel()
        {
            // Initialize with default state.
            State = new InGameUIState
            {
                IsCharacterSheetOpen = false,
                CharacterSheetPageIndex = 0
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
                CharacterSheetPageIndex = State.CharacterSheetPageIndex
            };

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Navigate to the next character sheet page.
        /// </summary>
        public void NextPage()
        {
            // Pages: 0 = character info, 1 = ability scores, 2 = skills, 3 = actions
            int maxPage = 3;
            int newPage = Mathf.Min(State.CharacterSheetPageIndex + 1, maxPage);
            SetPage(newPage);
        }

        /// <summary>
        /// Navigate to the previous character sheet page.
        /// </summary>
        public void PreviousPage()
        {
            int newPage = Mathf.Max(State.CharacterSheetPageIndex - 1, 0);
            SetPage(newPage);
        }

        /// <summary>
        /// Set the current character sheet page index.
        /// </summary>
        public void SetPage(int pageIndex)
        {
            if (State.CharacterSheetPageIndex == pageIndex)
                return;

            State = new InGameUIState
            {
                IsCharacterSheetOpen = State.IsCharacterSheetOpen,
                CharacterSheetPageIndex = pageIndex
            };

            StateChanged?.Invoke(State);
        }
    }
}



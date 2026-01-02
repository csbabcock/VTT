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
        public string SelectedSceneName;
        public string[] AvailableScenes;
        public string CurrentSection;
        
        // Character selection for Join Session
        public string SelectedCharacterFileName;
        public string[] AvailableCharacterFileNames;
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
                IsInteractable = true,
                SelectedSceneName = string.Empty,
                AvailableScenes = new string[0],
                CurrentSection = "host",
                SelectedCharacterFileName = string.Empty,
                AvailableCharacterFileNames = new string[0]
            };
        }

        public void SetInteractable(bool value)
        {
            if (IsInteractable == value)
                return;

            State = new MainMenuState
            {
                IsInteractable = value,
                SelectedSceneName = State.SelectedSceneName,
                AvailableScenes = State.AvailableScenes,
                CurrentSection = State.CurrentSection,
                SelectedCharacterFileName = State.SelectedCharacterFileName,
                AvailableCharacterFileNames = State.AvailableCharacterFileNames
            };

            StateChanged?.Invoke(State);
        }

        public void SetAvailableScenes(string[] scenes)
        {
            State = new MainMenuState
            {
                IsInteractable = State.IsInteractable,
                SelectedSceneName = State.SelectedSceneName,
                AvailableScenes = scenes ?? new string[0],
                CurrentSection = State.CurrentSection,
                SelectedCharacterFileName = State.SelectedCharacterFileName,
                AvailableCharacterFileNames = State.AvailableCharacterFileNames
            };

            StateChanged?.Invoke(State);
        }

        public void SetSelectedScene(string sceneName)
        {
            if (State.SelectedSceneName == sceneName)
                return;

            State = new MainMenuState
            {
                IsInteractable = State.IsInteractable,
                SelectedSceneName = sceneName ?? string.Empty,
                AvailableScenes = State.AvailableScenes,
                CurrentSection = State.CurrentSection,
                SelectedCharacterFileName = State.SelectedCharacterFileName,
                AvailableCharacterFileNames = State.AvailableCharacterFileNames
            };

            StateChanged?.Invoke(State);
        }

        public void SetCurrentSection(string section)
        {
            if (State.CurrentSection == section)
                return;

            State = new MainMenuState
            {
                IsInteractable = State.IsInteractable,
                SelectedSceneName = State.SelectedSceneName,
                AvailableScenes = State.AvailableScenes,
                CurrentSection = section ?? "map-selection",
                SelectedCharacterFileName = State.SelectedCharacterFileName,
                AvailableCharacterFileNames = State.AvailableCharacterFileNames
            };

            StateChanged?.Invoke(State);
        }

        public void SetAvailableCharacters(string[] characterFileNames)
        {
            State = new MainMenuState
            {
                IsInteractable = State.IsInteractable,
                SelectedSceneName = State.SelectedSceneName,
                AvailableScenes = State.AvailableScenes,
                CurrentSection = State.CurrentSection,
                SelectedCharacterFileName = State.SelectedCharacterFileName,
                AvailableCharacterFileNames = characterFileNames ?? new string[0]
            };

            StateChanged?.Invoke(State);
        }

        public void SetSelectedCharacter(string characterFileName)
        {
            if (State.SelectedCharacterFileName == characterFileName)
                return;

            State = new MainMenuState
            {
                IsInteractable = State.IsInteractable,
                SelectedSceneName = State.SelectedSceneName,
                AvailableScenes = State.AvailableScenes,
                CurrentSection = State.CurrentSection,
                SelectedCharacterFileName = characterFileName ?? string.Empty,
                AvailableCharacterFileNames = State.AvailableCharacterFileNames
            };

            StateChanged?.Invoke(State);
        }
    }
}



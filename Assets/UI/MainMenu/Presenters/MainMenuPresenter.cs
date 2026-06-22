using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.UI;
using GameCore.PlayerData;
using GameCore.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Presenter for the main menu. Connects MainMenuModel and MainMenuView,
    /// and delegates scene loading to SceneLoader.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuPresenter : MonoBehaviour, IUIPresenter<MainMenuModel, MainMenuView>
    {
        [Header("References")]
        [SerializeField] private MainMenuView _view;
        [SerializeField] private CharacterCreationPresenter _characterCreationPresenter;

        [Header("Scenes")]
        [Tooltip("List of available scenes to select from. Leave empty to auto-populate from build settings.")]
        [SerializeField] private string[] _availableScenes = new string[] { "Playground" };

        public MainMenuModel Model { get; private set; }
        public MainMenuView View => _view;

        private bool _initialized;

        private void Awake()
        {
            EnsureMenuCursorState();

            if (_view == null)
            {
                _view = GetComponent<MainMenuView>();
            }

            // Try to find CharacterCreationPresenter if not assigned
            if (_characterCreationPresenter == null)
            {
                _characterCreationPresenter = FindAnyObjectByType<CharacterCreationPresenter>();
                if (_characterCreationPresenter == null)
                {
                    Debug.LogWarning("MainMenuPresenter: CharacterCreationPresenter not found. Make sure a GameObject with CharacterCreationPresenter component exists in the scene.");
                }
            }

            Model = new MainMenuModel();
        }

        private void OnEnable()
        {
            EnsureMenuCursorState();

            if (!_initialized)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            EnsureMenuCursorState();

            if (_view == null)
            {
                Debug.LogError("MainMenuPresenter: View reference is missing.");
                return;
            }

            _view.Initialize();

            _view.CharacterDisplayProvider = BuildCharacterCardDisplay;

            _view.SceneSelected += HandleSceneSelected;
            _view.LoadSceneClicked += HandleLoadSceneClicked;
            _view.NavigationChanged += HandleNavigationChanged;
            _view.QuitClicked += HandleQuitClicked;
            _view.CharacterSelected += HandleCharacterSelected;
            _view.CreateCharacterClicked += HandleCreateCharacterClicked;
            _view.JoinSessionClicked += HandleJoinSessionClicked;

            Model.StateChanged += HandleModelStateChanged;

            // Initialize available scenes
            InitializeAvailableScenes();

            _view.Show();
            // Ensure the view starts in sync with the model.
            _view.UpdateView(Model.State);

            _initialized = true;
        }

        private static void EnsureMenuCursorState()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            if (_view != null)
            {
                _view.SceneSelected -= HandleSceneSelected;
                _view.LoadSceneClicked -= HandleLoadSceneClicked;
                _view.NavigationChanged -= HandleNavigationChanged;
                _view.QuitClicked -= HandleQuitClicked;
                _view.CharacterSelected -= HandleCharacterSelected;
                _view.CreateCharacterClicked -= HandleCreateCharacterClicked;
                _view.JoinSessionClicked -= HandleJoinSessionClicked;
            }

            if (Model != null)
            {
                Model.StateChanged -= HandleModelStateChanged;
            }

            _initialized = false;
        }

        private void InitializeAvailableScenes()
        {
            if (_availableScenes == null || _availableScenes.Length == 0)
            {
                // Auto-populate from build settings
                List<string> sceneNames = new List<string>();
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    // Exclude the main menu scene itself
                    if (sceneName != "MainMenu")
                    {
                        sceneNames.Add(sceneName);
                    }
                }
                Model.SetAvailableScenes(sceneNames.ToArray());
            }
            else
            {
                Model.SetAvailableScenes(_availableScenes);
            }
        }

        private void HandleSceneSelected(string sceneName)
        {
            Model.SetSelectedScene(sceneName);
        }

        private void HandleLoadSceneClicked()
        {
            string selectedScene = Model.State.SelectedSceneName;
            
            if (string.IsNullOrWhiteSpace(selectedScene))
            {
                Debug.LogWarning("MainMenuPresenter: No scene selected.");
                return;
            }

            Model.SetInteractable(false);

            var launcher = SessionLauncherLocator.Current;
            if (launcher != null)
            {
                // Host the session; NGO replicates the gameplay scene load to clients.
                if (!launcher.StartHost(selectedScene))
                {
                    Debug.LogError("MainMenuPresenter: Failed to start host.");
                    Model.SetInteractable(true);
                }
            }
            else
            {
                // No networking wired (offline / not yet set up): load locally.
                SceneLoader.LoadScene(selectedScene);
            }
        }

        private void HandleNavigationChanged(string section)
        {
            Model.SetCurrentSection(section);
            
            // When switching to join section, load available characters
            if (section == "join")
            {
                InitializeAvailableCharacters();
            }
        }

        private void HandleQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleModelStateChanged(MainMenuState state)
        {
            _view.UpdateView(state);
        }

        private void InitializeAvailableCharacters()
        {
            try
            {
                var characterFiles = CharacterFileService.GetAllCharacterFiles();
                string[] fileNames = characterFiles.Select(f => f.FileName).ToArray();
                Model.SetAvailableCharacters(fileNames);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"MainMenuPresenter: Error loading characters: {ex.Message}");
                Model.SetAvailableCharacters(new string[0]);
            }
        }

        private CharacterCardDisplay BuildCharacterCardDisplay(string fileName)
        {
            var fileInfo = CharacterFileService.GetCharacterFile(fileName);
            if (fileInfo == null)
            {
                return new CharacterCardDisplay(fileName, string.Empty);
            }

            return new CharacterCardDisplay(
                CharacterFileService.GetCharacterDisplayName(fileInfo),
                CharacterFileService.GetCharacterCardSubtitle(fileInfo));
        }

        private void HandleCharacterSelected(string characterFileName)
        {
            Model.SetSelectedCharacter(characterFileName);
        }

        private void HandleCreateCharacterClicked()
        {
            // Show character creation UI
            if (_characterCreationPresenter != null)
            {
                _characterCreationPresenter.Show();
                Debug.Log("MainMenuPresenter: Showing character creation UI");
            }
            else
            {
                Debug.LogWarning("MainMenuPresenter: CharacterCreationPresenter reference is missing. Cannot show character creation UI.");
                Debug.LogWarning("MainMenuPresenter: Make sure to assign the CharacterCreationPresenter reference in the inspector.");
            }
        }

        private void HandleJoinSessionClicked()
        {
            string selectedCharacter = Model.State.SelectedCharacterFileName;
            
            if (string.IsNullOrWhiteSpace(selectedCharacter))
            {
                Debug.LogWarning("MainMenuPresenter: No character selected for join.");
                return;
            }

            // Load the character data and initialize the player data service
            var fileInfo = CharacterFileService.GetCharacterFile(selectedCharacter);
            if (fileInfo == null)
            {
                Debug.LogError($"MainMenuPresenter: Character file not found: {selectedCharacter}");
                return;
            }

            // Initialize the player data service with the selected character
            // Use the relative path format that JsonPlayerDataService expects
            string relativePath = $"Characters/{fileInfo.FileName}";
            var playerDataService = new JsonPlayerDataService(relativePath);
            PlayerDataServiceLocator.Service = playerDataService;

            Debug.Log($"MainMenuPresenter: Loaded character {fileInfo.CharacterData?.characterName ?? selectedCharacter} for join session");

            Model.SetInteractable(false);

            var launcher = SessionLauncherLocator.Current;
            if (launcher != null)
            {
                // Connect to the host by IP; the server replicates its loaded scene to us.
                launcher.Address = _view != null ? _view.JoinIpAddress : launcher.Address;
                if (!launcher.StartClient())
                {
                    Debug.LogError($"MainMenuPresenter: Failed to start client (host {launcher.Address}:{launcher.Port}).");
                    Model.SetInteractable(true);
                }
            }
            else
            {
                // No networking wired (offline / not yet set up): load locally.
                string defaultScene = Model.State.AvailableScenes?.FirstOrDefault() ?? "Playground";
                SceneLoader.LoadScene(defaultScene);
            }
        }
    }
}


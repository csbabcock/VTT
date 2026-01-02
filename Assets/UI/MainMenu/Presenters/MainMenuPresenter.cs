using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.UI;
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

        [Header("Scenes")]
        [Tooltip("List of available scenes to select from. Leave empty to auto-populate from build settings.")]
        [SerializeField] private string[] _availableScenes = new string[] { "Playground" };

        public MainMenuModel Model { get; private set; }
        public MainMenuView View => _view;

        private bool _initialized;

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<MainMenuView>();
            }

            Model = new MainMenuModel();
        }

        private void OnEnable()
        {
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

            if (_view == null)
            {
                Debug.LogError("MainMenuPresenter: View reference is missing.");
                return;
            }

            _view.Initialize();

            _view.SceneSelected += HandleSceneSelected;
            _view.LoadSceneClicked += HandleLoadSceneClicked;
            _view.NavigationChanged += HandleNavigationChanged;
            _view.QuitClicked += HandleQuitClicked;

            Model.StateChanged += HandleModelStateChanged;

            // Initialize available scenes
            InitializeAvailableScenes();

            _view.Show();
            // Ensure the view starts in sync with the model.
            _view.UpdateView(Model.State);

            _initialized = true;
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
            SceneLoader.LoadScene(selectedScene);
        }

        private void HandleNavigationChanged(string section)
        {
            Model.SetCurrentSection(section);
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
    }
}


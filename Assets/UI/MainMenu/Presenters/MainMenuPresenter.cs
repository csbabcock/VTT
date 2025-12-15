using System;
using GameCore.UI;
using UnityEngine;

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
        [Tooltip("Name of the level scene to load when Start Game is pressed.")]
        [SerializeField] private string _levelSceneName = "Playground";

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

            _view.StartClicked += HandleStartClicked;
            _view.SettingsClicked += HandleSettingsClicked;
            _view.QuitClicked += HandleQuitClicked;

            Model.StateChanged += HandleModelStateChanged;

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
                _view.StartClicked -= HandleStartClicked;
                _view.SettingsClicked -= HandleSettingsClicked;
                _view.QuitClicked -= HandleQuitClicked;
            }

            if (Model != null)
            {
                Model.StateChanged -= HandleModelStateChanged;
            }

            _initialized = false;
        }

        private void HandleStartClicked()
        {
            Model.SetInteractable(false);

            if (string.IsNullOrWhiteSpace(_levelSceneName))
            {
                Debug.LogError("MainMenuPresenter: Level scene name is not set.");
                return;
            }

            SceneLoader.LoadScene(_levelSceneName);
        }

        private void HandleSettingsClicked()
        {
            // Placeholder for future settings implementation.
            Debug.Log("MainMenuPresenter: Settings clicked (not implemented yet).");
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


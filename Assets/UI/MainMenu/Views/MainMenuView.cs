using System;
using GameCore.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// UI Toolkit view for the main menu.
    /// Wraps a UIDocument and exposes strongly-typed events for the presenter.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuView : MonoBehaviour, IUIView<MainMenuState>
    {
        [Header("Assets")]
        [Tooltip("Optional: USS stylesheet for this view. If not assigned, it will still work if referenced from the UXML.")]
        [SerializeField] private StyleSheet _mainMenuStyleSheet;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private Button _startButton;
        private Button _settingsButton;
        private Button _quitButton;

        public event Action StartClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;

        public VisualElement Root => _root;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_root == null)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("MainMenuView: UIDocument has no rootVisualElement.");
                return;
            }

            if (_mainMenuStyleSheet != null && !_root.styleSheets.Contains(_mainMenuStyleSheet))
            {
                _root.styleSheets.Add(_mainMenuStyleSheet);
            }

            _startButton = _root.Q<Button>("start-button");
            _settingsButton = _root.Q<Button>("settings-button");
            _quitButton = _root.Q<Button>("quit-button");

            if (_startButton != null)
                _startButton.clicked += OnStartClicked;

            if (_settingsButton != null)
                _settingsButton.clicked += OnSettingsClicked;

            if (_quitButton != null)
                _quitButton.clicked += OnQuitClickedInternal;
        }

        private void OnDisable()
        {
            if (_startButton != null)
                _startButton.clicked -= OnStartClicked;

            if (_settingsButton != null)
                _settingsButton.clicked -= OnSettingsClicked;

            if (_quitButton != null)
                _quitButton.clicked -= OnQuitClickedInternal;
        }

        public void Show()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                _root.SetEnabled(true);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                _root.SetEnabled(false);
            }
        }

        /// <summary>
        /// Update the view based on the latest main menu state.
        /// </summary>
        public void UpdateView(MainMenuState state)
        {
            SetInteractable(state.IsInteractable);
        }

        public void SetInteractable(bool value)
        {
            if (_root != null)
            {
                _root.SetEnabled(value);
            }
        }

        private void OnStartClicked()
        {
            StartClicked?.Invoke();
        }

        private void OnSettingsClicked()
        {
            SettingsClicked?.Invoke();
        }

        private void OnQuitClickedInternal()
        {
            QuitClicked?.Invoke();
        }
    }
}


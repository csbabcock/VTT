using System;
using System.Linq;
using GameCore.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Main Menu Controller - SpacetimeDB-Inspired Design
    /// Handles UI interactions, hover effects, and audio feedback
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuView : MonoBehaviour, IUIView<MainMenuState>
    {
        [Header("Assets")]
        [Tooltip("Optional: USS stylesheet for this view. If not assigned, it will still work if referenced from the UXML.")]
        [SerializeField] private StyleSheet _mainMenuStyleSheet;
        
        [Header("Fonts")]
        [Tooltip("Roboto Regular font")]
        [SerializeField] private Font _robotoRegular;
        [Tooltip("Roboto Medium font")]
        [SerializeField] private Font _robotoMedium;
        [Tooltip("Roboto Bold font")]
        [SerializeField] private Font _robotoBold;
        [Tooltip("Roboto SemiBold font")]
        [SerializeField] private Font _robotoSemiBold;
        
        [Header("Audio")]
        [Tooltip("Optional: Audio clip for button hover sound")]
        [SerializeField] private AudioClip _hoverSound;
        
        [Tooltip("Optional: Audio clip for button click sound")]
        [SerializeField] private AudioClip _clickSound;

        // UI Document Reference
        private UIDocument _uiDocument;
        private VisualElement _root;
        
        // Sidebar Navigation Items
        private Button _navHost;
        private Button _navJoin;
        private Button _navSettings;
        
        // Title Label and Version Label
        private Label _titleLabel;
        private Label _versionLabel;
        
        // Content Panels
        private VisualElement _hostContent;
        private VisualElement _joinContent;
        private VisualElement _settingsContent;
        
        // Map Selection Elements (in Host Content)
        private ScrollView _sceneGridScroll;
        private VisualElement _sceneGridContainer;
        private Label _selectedSceneNameLabel;
        private Button _loadSceneButton;
        
        // Character Selection Elements (in Join Content)
        private ScrollView _characterGridScroll;
        private VisualElement _characterGridContainer;
        private Label _selectedCharacterNameLabel;
        private Button _createCharacterButton;
        private Button _joinSessionButton;
        
        // Exit Button and Confirmation Dialog
        private Button _exitButton;
        private VisualElement _exitConfirmationDialog;
        private Button _dialogCancelButton;
        private Button _dialogConfirmButton;
        
        // Audio Source (optional)
        private AudioSource _audioSource;
        
        // Event handlers for navigation (stored for proper unregistration)
        private System.Action _onNavHostClicked;
        private System.Action _onNavJoinClicked;
        private System.Action _onNavSettingsClicked;

        // Events
        public event Action<string> SceneSelected;
        public event Action LoadSceneClicked;
        public event Action QuitClicked;
        public event Action<string> NavigationChanged;
        
        // Character Selection Events
        public event Action<string> CharacterSelected;
        public event Action CreateCharacterClicked;
        public event Action JoinSessionClicked;

        public VisualElement Root => _root;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            
            // Setup audio source if audio clips are assigned
            if (_hoverSound != null || _clickSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.volume = 0.5f;
            }
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

            // Add stylesheet if assigned
            if (_mainMenuStyleSheet != null && !_root.styleSheets.Contains(_mainMenuStyleSheet))
            {
                _root.styleSheets.Add(_mainMenuStyleSheet);
            }

            // Query UI Elements
            QueryUIElements();
            
            // Apply Fonts
            ApplyFonts();
            
            // Setup Event Handlers
            SetupEventHandlers();
            
            // Setup Hover Effects
            SetupHoverEffects();
        }
        
        private void ApplyFonts()
        {
            if (_root == null) return;
            
            ApplyFontToElement(_root, _robotoRegular);
            ApplyFontToElement(_root.Q<Label>("title-label"), _robotoBold);
            ApplyFontToElement(_versionLabel, _robotoRegular);
            ApplyFontToElement(_root.Q<Label>("selected-scene-name"), _robotoSemiBold);
            ApplyFontToElement(_root.Q<Label>(className: "menu-dialog-title"), _robotoBold);
            ApplyFontToElement(_root.Q<Label>(className: "menu-dialog-message"), _robotoRegular);
            
            // Apply fonts to header labels
            ApplyFontToElements(_root.Query<Label>(className: "menu-content-header-title"), _robotoBold);
            ApplyFontToElements(_root.Query<Label>(className: "menu-content-header-subtitle"), _robotoRegular);
            
            ApplyFontToElements(_root.Query<Button>(className: "menu-nav-item"), _robotoMedium);
            ApplyFontToElements(_root.Query<Button>(className: "menu-button-primary"), _robotoMedium);
        }
        
        private void ApplyFontToElement(VisualElement element, Font font)
        {
            if (element != null && font != null)
            {
                element.style.unityFont = new StyleFont(font);
            }
        }
        
        private void ApplyFontToElements(UQueryBuilder<Button> query, Font font)
        {
            if (font == null) return;
            
            foreach (var button in query.ToList())
            {
                button.style.unityFont = new StyleFont(font);
            }
        }
        
        private void ApplyFontToElements(UQueryBuilder<Label> query, Font font)
        {
            if (font == null) return;
            
            foreach (var label in query.ToList())
            {
                label.style.unityFont = new StyleFont(font);
            }
        }

        private void QueryUIElements()
        {
            // Title Label and Version Label
            _titleLabel = _root.Q<Label>("title-label");
            _versionLabel = _root.Q<Label>("version-label");
            
            // Sidebar Navigation Items
            _navHost = _root.Q<Button>("nav-host");
            _navJoin = _root.Q<Button>("nav-join");
            _navSettings = _root.Q<Button>("nav-settings");
            
            // Content Panels
            _hostContent = _root.Q<VisualElement>("host-content");
            _joinContent = _root.Q<VisualElement>("join-content");
            _settingsContent = _root.Q<VisualElement>("settings-content");
            
            // Map Selection (in Host Content)
            _sceneGridScroll = _root.Q<ScrollView>("scene-grid-scroll");
            _sceneGridContainer = _root.Q<VisualElement>("scene-grid-container");
            _selectedSceneNameLabel = _root.Q<Label>("selected-scene-name");
            _loadSceneButton = _root.Q<Button>("load-scene-button");
            
            // Character Selection (in Join Content)
            _characterGridScroll = _root.Q<ScrollView>("character-grid-scroll");
            _characterGridContainer = _root.Q<VisualElement>("character-grid-container");
            _selectedCharacterNameLabel = _root.Q<Label>("selected-character-name");
            _createCharacterButton = _root.Q<Button>("create-character-button");
            _joinSessionButton = _root.Q<Button>("join-session-button");
            
            // Exit Button and Confirmation Dialog
            _exitButton = _root.Q<Button>("exit-button");
            _exitConfirmationDialog = _root.Q<VisualElement>("exit-confirmation-dialog");
            _dialogCancelButton = _root.Q<Button>("dialog-no");
            _dialogConfirmButton = _root.Q<Button>("dialog-yes");
            
            // Setup version label text
            SetupVersionLabel();
        }
        
        private void SetupVersionLabel()
        {
            if (_versionLabel == null) return;
            
            // Get the version from Application.version (Unity's build settings)
            string version = Application.version;
            
            // If version is empty, use a default
            if (string.IsNullOrEmpty(version))
            {
                version = "1.0.0";
            }
            
            _versionLabel.text = version;
        }

        private void SetupEventHandlers()
        {
            // Sidebar Navigation Items - store handlers for proper unregistration
            _onNavHostClicked = () => OnNavigationClicked("host");
            _onNavJoinClicked = () => OnNavigationClicked("join");
            _onNavSettingsClicked = () => OnNavigationClicked("settings");
            
            if (_navHost != null)
                _navHost.clicked += _onNavHostClicked;
            
            if (_navJoin != null)
                _navJoin.clicked += _onNavJoinClicked;
            
            if (_navSettings != null)
                _navSettings.clicked += _onNavSettingsClicked;
            
            // Map Selection
            if (_loadSceneButton != null)
                _loadSceneButton.clicked += OnLoadSceneClicked;
            
            // Character Selection
            if (_createCharacterButton != null)
                _createCharacterButton.clicked += OnCreateCharacterClicked;
            
            if (_joinSessionButton != null)
                _joinSessionButton.clicked += OnJoinSessionClicked;
            
            // Exit Button - shows confirmation dialog
            if (_exitButton != null)
                _exitButton.clicked += OnExitButtonClicked;
            
            // Dialog Buttons
            if (_dialogCancelButton != null)
                _dialogCancelButton.clicked += OnDialogCancelClicked;
            
            if (_dialogConfirmButton != null)
                _dialogConfirmButton.clicked += OnDialogConfirmClicked;
        }

        private void SetupHoverEffects()
        {
            RegisterHoverEffect(_navHost);
            RegisterHoverEffect(_navJoin);
            RegisterHoverEffect(_navSettings);
            RegisterHoverEffect(_loadSceneButton);
            RegisterHoverEffect(_createCharacterButton);
            RegisterHoverEffect(_joinSessionButton);
            RegisterHoverEffect(_exitButton);
            RegisterHoverEffect(_dialogCancelButton);
            RegisterHoverEffect(_dialogConfirmButton);
        }
        
        private void RegisterHoverEffect(Button button)
        {
            if (button != null)
            {
                button.RegisterCallback<MouseEnterEvent>(OnButtonHover);
            }
        }
        
        private void UnregisterHoverEffect(Button button)
        {
            if (button != null)
            {
                button.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
            }
        }

        private void OnDisable()
        {
            UnregisterEventHandlers();
            ClearSceneCards();
            ClearCharacterCards();
        }
        
        private void UnregisterEventHandlers()
        {
            UnregisterClickHandler(_navHost, _onNavHostClicked);
            UnregisterClickHandler(_navJoin, _onNavJoinClicked);
            UnregisterClickHandler(_navSettings, _onNavSettingsClicked);
            
            if (_loadSceneButton != null)
            {
                _loadSceneButton.clicked -= OnLoadSceneClicked;
                UnregisterHoverEffect(_loadSceneButton);
            }
            
            if (_createCharacterButton != null)
            {
                _createCharacterButton.clicked -= OnCreateCharacterClicked;
                UnregisterHoverEffect(_createCharacterButton);
            }
            
            if (_joinSessionButton != null)
            {
                _joinSessionButton.clicked -= OnJoinSessionClicked;
                UnregisterHoverEffect(_joinSessionButton);
            }
            
            if (_exitButton != null)
            {
                _exitButton.clicked -= OnExitButtonClicked;
                UnregisterHoverEffect(_exitButton);
            }
            
            if (_dialogCancelButton != null)
            {
                _dialogCancelButton.clicked -= OnDialogCancelClicked;
                UnregisterHoverEffect(_dialogCancelButton);
            }
            
            if (_dialogConfirmButton != null)
            {
                _dialogConfirmButton.clicked -= OnDialogConfirmClicked;
                UnregisterHoverEffect(_dialogConfirmButton);
            }
        }
        
        private void UnregisterClickHandler(Button button, Action handler)
        {
            if (button != null && handler != null)
            {
                button.clicked -= handler;
                UnregisterHoverEffect(button);
            }
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
            UpdateNavigation(state.CurrentSection);
            UpdateSceneList(state.AvailableScenes, state.SelectedSceneName);
            UpdateLoadButton(state.SelectedSceneName);
            UpdateSelectedSceneDisplay(state.SelectedSceneName);
            UpdateCharacterList(state.AvailableCharacterFileNames, state.SelectedCharacterFileName);
            UpdateJoinButton(state.SelectedCharacterFileName);
            UpdateSelectedCharacterDisplay(state.SelectedCharacterFileName);
        }

        private void UpdateNavigation(string currentSection)
        {
            // Update navigation button states
            UpdateNavButtonState(_navHost, currentSection == "host");
            UpdateNavButtonState(_navJoin, currentSection == "join");
            UpdateNavButtonState(_navSettings, currentSection == "settings");
            
            // Show/hide content panels
            if (_hostContent != null)
                _hostContent.style.display = currentSection == "host" ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (_joinContent != null)
                _joinContent.style.display = currentSection == "join" ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (_settingsContent != null)
                _settingsContent.style.display = currentSection == "settings" ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateNavButtonState(Button button, bool isSelected)
        {
            if (button == null) return;
            
            if (isSelected)
                button.AddToClassList("selected");
            else
                button.RemoveFromClassList("selected");
        }

        private void OnNavigationClicked(string section)
        {
            PlayClickSound();
            NavigationChanged?.Invoke(section);
        }

        private void OnExitButtonClicked()
        {
            PlayClickSound();
            ShowExitConfirmationDialog();
        }

        private void OnDialogCancelClicked()
        {
            PlayClickSound();
            HideExitConfirmationDialog();
        }

        private void OnDialogConfirmClicked()
        {
            PlayClickSound();
            HideExitConfirmationDialog();
            QuitClicked?.Invoke();
        }

        private void ShowExitConfirmationDialog()
        {
            if (_exitConfirmationDialog != null)
            {
                _exitConfirmationDialog.style.display = DisplayStyle.Flex;
            }
        }

        private void HideExitConfirmationDialog()
        {
            if (_exitConfirmationDialog != null)
            {
                _exitConfirmationDialog.style.display = DisplayStyle.None;
            }
        }

        private void UpdateSceneList(string[] scenes, string selectedScene)
        {
            if (_sceneGridContainer == null)
                return;

            bool needsRebuild = _sceneGridContainer.childCount != (scenes?.Length ?? 0);
            
            if (needsRebuild)
            {
                RebuildSceneCards(scenes);
            }

            UpdateSceneCardSelection(scenes, selectedScene);
        }
        
        private void RebuildSceneCards(string[] scenes)
        {
            ClearSceneCards();

            if (scenes == null || scenes.Length == 0)
                return;

            foreach (string sceneName in scenes)
            {
                VisualElement sceneCard = CreateSceneCard(sceneName);
                _sceneGridContainer.Add(sceneCard);
            }
        }
        
        private VisualElement CreateSceneCard(string sceneName)
        {
            VisualElement sceneCard = new VisualElement();
            sceneCard.AddToClassList("menu-scene-card");
            sceneCard.name = $"scene-card-{sceneName}";
            
            sceneCard.Add(CreateSceneCardLabel(sceneName.ToUpper(), "menu-scene-card-title", _robotoSemiBold));
            sceneCard.Add(CreateSceneCardLabel("Click to select", "menu-scene-card-subtitle", _robotoRegular));

            sceneCard.RegisterCallback<ClickEvent>(evt => OnSceneCardClicked(sceneName));
            sceneCard.RegisterCallback<MouseEnterEvent>(OnButtonHover);
            
            return sceneCard;
        }
        
        private void UpdateSceneCardSelection(string[] scenes, string selectedScene)
        {
            if (scenes == null || _sceneGridContainer == null)
                return;

            foreach (string sceneName in scenes)
            {
                VisualElement card = _sceneGridContainer.Q<VisualElement>($"scene-card-{sceneName}");
                if (card != null)
                {
                    if (sceneName == selectedScene)
                        card.AddToClassList("selected");
                    else
                        card.RemoveFromClassList("selected");
                }
            }
        }

        private void ClearSceneCards()
        {
            if (_sceneGridContainer == null)
                return;

            _sceneGridContainer.Clear();
        }

        private void UpdateLoadButton(string selectedScene)
        {
            if (_loadSceneButton != null)
            {
                _loadSceneButton.SetEnabled(!string.IsNullOrEmpty(selectedScene));
            }
        }

        private void UpdateSelectedSceneDisplay(string selectedScene)
        {
            if (_selectedSceneNameLabel != null)
            {
                _selectedSceneNameLabel.text = string.IsNullOrEmpty(selectedScene) ? "None" : selectedScene.ToUpper();
            }
        }

        public void SetInteractable(bool value)
        {
            if (_root != null)
            {
                _root.SetEnabled(value);
            }
        }

        private void OnButtonHover(MouseEnterEvent evt)
        {
            PlayHoverSound();
        }

        private void OnLoadSceneClicked()
        {
            PlayClickSound();
            LoadSceneClicked?.Invoke();
        }

        private void OnSceneCardClicked(string sceneName)
        {
            PlayClickSound();
            SceneSelected?.Invoke(sceneName);
        }

        private void OnCreateCharacterClicked()
        {
            PlayClickSound();
            CreateCharacterClicked?.Invoke();
        }

        private void OnJoinSessionClicked()
        {
            PlayClickSound();
            JoinSessionClicked?.Invoke();
        }

        private void OnCharacterCardClicked(string characterFileName)
        {
            PlayClickSound();
            CharacterSelected?.Invoke(characterFileName);
        }
        
        private Label CreateSceneCardLabel(string text, string className, Font font)
        {
            Label label = new Label();
            label.text = text;
            label.AddToClassList(className);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            if (font != null)
            {
                label.style.unityFont = new StyleFont(font);
            }
            
            return label;
        }

        private void PlayHoverSound()
        {
            if (_audioSource != null && _hoverSound != null)
            {
                _audioSource.PlayOneShot(_hoverSound);
            }
        }

        private void PlayClickSound()
        {
            if (_audioSource != null && _clickSound != null)
            {
                _audioSource.PlayOneShot(_clickSound);
            }
        }

        // ============================================
        // CHARACTER SELECTION METHODS
        // ============================================

        private void UpdateCharacterList(string[] characterFileNames, string selectedCharacterFileName)
        {
            if (_characterGridContainer == null)
                return;

            bool needsRebuild = _characterGridContainer.childCount != (characterFileNames?.Length ?? 0);
            
            if (needsRebuild)
            {
                RebuildCharacterCards(characterFileNames);
            }

            UpdateCharacterCardSelection(characterFileNames, selectedCharacterFileName);
        }

        private void RebuildCharacterCards(string[] characterFileNames)
        {
            ClearCharacterCards();

            if (characterFileNames == null || characterFileNames.Length == 0)
                return;

            // Load character data for display
            foreach (string fileName in characterFileNames)
            {
                var fileInfo = GameCore.PlayerData.CharacterFileService.GetCharacterFile(fileName);
                if (fileInfo != null)
                {
                    VisualElement characterCard = CreateCharacterCard(fileInfo);
                    _characterGridContainer.Add(characterCard);
                }
            }
        }

        private VisualElement CreateCharacterCard(GameCore.PlayerData.CharacterFileService.CharacterFileInfo fileInfo)
        {
            VisualElement characterCard = new VisualElement();
            characterCard.AddToClassList("menu-scene-card");
            characterCard.name = $"character-card-{fileInfo.FileName}";
            
            string title = GameCore.PlayerData.CharacterFileService.GetCharacterDisplayName(fileInfo);
            string subtitle = GameCore.PlayerData.CharacterFileService.GetCharacterCardSubtitle(fileInfo);
            
            characterCard.Add(CreateSceneCardLabel(title, "menu-scene-card-title", _robotoSemiBold));
            characterCard.Add(CreateSceneCardLabel(subtitle, "menu-scene-card-subtitle", _robotoRegular));

            characterCard.RegisterCallback<ClickEvent>(evt => OnCharacterCardClicked(fileInfo.FileName));
            characterCard.RegisterCallback<MouseEnterEvent>(OnButtonHover);
            
            return characterCard;
        }

        private void UpdateCharacterCardSelection(string[] characterFileNames, string selectedCharacterFileName)
        {
            if (characterFileNames == null || _characterGridContainer == null)
                return;

            foreach (string fileName in characterFileNames)
            {
                VisualElement card = _characterGridContainer.Q<VisualElement>($"character-card-{fileName}");
                if (card != null)
                {
                    if (fileName == selectedCharacterFileName)
                        card.AddToClassList("selected");
                    else
                        card.RemoveFromClassList("selected");
                }
            }
        }

        private void ClearCharacterCards()
        {
            if (_characterGridContainer == null)
                return;

            _characterGridContainer.Clear();
        }

        private void UpdateJoinButton(string selectedCharacterFileName)
        {
            if (_joinSessionButton != null)
            {
                _joinSessionButton.SetEnabled(!string.IsNullOrEmpty(selectedCharacterFileName));
            }
        }

        private void UpdateSelectedCharacterDisplay(string selectedCharacterFileName)
        {
            if (_selectedCharacterNameLabel == null)
                return;

            if (string.IsNullOrEmpty(selectedCharacterFileName))
            {
                _selectedCharacterNameLabel.text = "No character selected";
                return;
            }

            var fileInfo = GameCore.PlayerData.CharacterFileService.GetCharacterFile(selectedCharacterFileName);
            if (fileInfo != null)
            {
                string displayName = GameCore.PlayerData.CharacterFileService.GetCharacterDisplayName(fileInfo);
                _selectedCharacterNameLabel.text = displayName.ToUpper();
            }
            else
            {
                _selectedCharacterNameLabel.text = "Unknown character";
            }
        }

    }
}

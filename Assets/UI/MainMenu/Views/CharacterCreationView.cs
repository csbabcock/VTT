using System;
using System.Collections;
using System.Collections.Generic;
using GameCore.UI;
using GameCore.UI.MainMenu.Scrollbars;
using GameCore.UI.MainMenu.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// One titled block for the character creation detail panel (e.g. D&amp;D Beyond–style class sections).
    /// </summary>
    public readonly struct CharacterDetailSection
    {
        public string Heading { get; }
        public string Body { get; }
        /// <summary>True when heading or body contains live ability modifier hints (rich text).</summary>
        public bool HasLiveAbilityHints { get; }

        public CharacterDetailSection(string heading, string body, bool hasLiveAbilityHints = false)
        {
            Heading = heading ?? string.Empty;
            Body = body ?? string.Empty;
            HasLiveAbilityHints = hasLiveAbilityHints;
        }
    }

    /// <summary>
    /// View for character creation UI.
    /// Follows MVP: coordinates binding and events; ability stat tiles are built by <see cref="AbilityStatRowViewFactory"/>.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public partial class CharacterCreationView : MonoBehaviour, IUIView<CharacterCreationState>
    {
        private static readonly string[] AbilityNamesShort = { "str", "dex", "con", "int", "wis", "cha" };
        private static readonly string[] AbilityNamesDisplay = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

        private const int PrimaryRowAbilityCount = 3;
        private const string AbilityScoresRowPrimaryName = "ability-scores-row-primary";
        private const string AbilityScoresRowSecondaryName = "ability-scores-row-secondary";

        [Header("Assets")]
        [Tooltip("Optional: USS stylesheet for this view. If not assigned, it will still work if referenced from the UXML.")]
        [SerializeField] private StyleSheet _characterCreationStyleSheet;

        /// <summary>Custom pane scrollbars (USS + track/thumb in UXML); swappable for tests.</summary>
        private ICustomPaneScrollBarBinder _paneScrollBarBinder = new CharacterCreationPaneScrollBarBinder();

        private PanelRenderer _panelRenderer;
        private bool _panelReloadRegistered;
        private Coroutine _deferredBindCoroutine;
        private bool _visualTreeBound;
        private bool _isVisible;
        private bool _hasLastState;
        private CharacterCreationState _lastState;
        private List<(string id, string displayName)> _lastClassOptions;
        private List<(string id, string displayName)> _lastRaceOptions;
        private List<(string id, string displayName)> _lastBackgroundOptions;
        private VisualElement _root;

        // Tab buttons
        private Button[] _tabButtons;
        private VisualElement[] _tabContents;

        // Option button containers
        private VisualElement _classButtonsContainer;
        private VisualElement _raceButtonsContainer;
        private VisualElement _backgroundButtonsContainer;

        // Ability score inputs (using Labels for display since they're read-only)
        private Label[] _abilityScoreLabels;

        // Detail panel
        private Label _detailName;
        private Label _detailType;
        private VisualElement _detailSectionsHost;
        private Label _detailContent;
        private VisualElement _featuresSection;
        private Label _featuresSectionTitle;
        private Label _characterLevelTotalLabel;
        private VisualElement _detailClassLevelRow;
        private Label _detailClassLevelHint;
        private Button _detailClassLevelMinus;
        private Button _detailClassLevelPlus;
        private IntegerField _detailClassLevelField;
        private bool _detailClassLevelControlsHooked;
        private int _detailClassLevelMaxCached = CharacterCreationModel.MaxCharacterLevel;

        // Stats panel
        private VisualElement _abilityScoresGrid;
        private VisualElement _characterStatsGrid;
        private VisualElement _spellcastingStatsGrid;
        private VisualElement _physicalTraitsGrid;
        private VisualElement _proficiencyListHost;
        private VisualElement _rolledScoresPool;
        private VisualElement _rolledScoresContainer;

        // Action buttons
        private Button _cancelButton;
        private Button _createButton;
        private Button _rollButton;
        private Button _standardArrayButton;
        private Button _manualButton;
        private Button _pointBuyButton;
        private Label _pointBuyPointsLabel;
        private Button _confirmScoresButton;
        private VisualElement _scoreMethodButtonsContainer;

        /// <summary>Cached once when ability rows are created; routes tile controls to view events.</summary>
        private AbilityStatRowUiBinding _abilityStatRowBinding;

        // Drag and drop visual state (UI only - no business logic)
        private VisualElement _dragPreview; // Visual preview of dragged score
        private int _pendingManualDragIndex = -1;
        private int _pendingManualDragValue;
        private Vector2 _pendingManualDragPosition;
        private bool _suppressManualAbilityEntryEvents;

        // Events - View only raises events, delegates all logic to Presenter
        public event System.Action<string> ClassSelected;
        public event System.Action<string> RaceSelected;
        public event System.Action<string> BackgroundSelected;
        public event System.Action RollAbilitiesClicked;
        public event System.Action StandardArrayClicked;
        public event System.Action ManualClicked;
        public event System.Action PointBuyClicked;
        public event System.Action<int> PointBuyIncrementClicked;
        public event System.Action<int> PointBuyDecrementClicked;
        public event System.Action<int, string> ManualScoreChanged; // rolledScoreIndex, text
        public event System.Action<int, string> ManualAbilityEntryChanged; // abilityIndex, text (direct Manual mode)
        public event System.Action<int, int> DragStartedFromRolledScore; // rolledScoreIndex, scoreValue
        public event System.Action<int> DragStartedFromAbility; // abilityIndex
        public event System.Action<Vector2> DropOccurred; // position
        public event System.Action ConfirmScoresClicked;
        public event System.Action CancelClicked;
        public event System.Action CreateCharacterClicked;
        /// <summary>Invoked when the user changes the selected class level (1–20).</summary>
        public event System.Action<int> SelectedClassLevelChanged;
        public event System.Action VisualTreeBound;

        public VisualElement Root => _root;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            if (_panelRenderer == null)
            {
                _panelRenderer = GetComponent<PanelRenderer>();
            }

            EnsurePanelReloadSubscription();
            if (_panelRenderer != null)
            {
                ((IPanelComponent)_panelRenderer).PerformUpdate();
            }

            TrySyncRootFromPanel();
            TryBindVisualTree();
            ScheduleDeferredBindIfNeeded();
        }

        private void OnDisable()
        {
            if (_deferredBindCoroutine != null)
            {
                StopCoroutine(_deferredBindCoroutine);
                _deferredBindCoroutine = null;
            }

            ReleasePanelReloadSubscription();
            _visualTreeBound = false;
            _detailClassLevelControlsHooked = false;
            _root = null;
        }

        private void OnPanelUiReload(PanelRenderer _, VisualElement root)
        {
            _visualTreeBound = false;
            _detailClassLevelControlsHooked = false;
            _root = root;
            TryBindVisualTree();
        }

        private void EnsurePanelReloadSubscription()
        {
            if (_panelRenderer == null || _panelReloadRegistered)
            {
                return;
            }

            _panelRenderer.RegisterUIReloadCallback(OnPanelUiReload);
            _panelReloadRegistered = true;
        }

        private void ReleasePanelReloadSubscription()
        {
            if (_panelRenderer == null || !_panelReloadRegistered)
            {
                return;
            }

            _panelRenderer.UnregisterUIReloadCallback(OnPanelUiReload);
            _panelReloadRegistered = false;
        }

        private void TrySyncRootFromPanel()
        {
            if (_root != null || _panelRenderer == null)
            {
                return;
            }

            _root = PanelRendererUtility.TryGetRootVisualElement(_panelRenderer);
        }

        private void ScheduleDeferredBindIfNeeded()
        {
            if (_visualTreeBound || !isActiveAndEnabled)
            {
                return;
            }

            if (_deferredBindCoroutine != null)
            {
                StopCoroutine(_deferredBindCoroutine);
            }

            _deferredBindCoroutine = StartCoroutine(CoDeferredBindPanelTree());
        }

        private IEnumerator CoDeferredBindPanelTree()
        {
            try
            {
                for (int i = 0; i < 24; i++)
                {
                    if (_visualTreeBound)
                    {
                        yield break;
                    }

                    TrySyncRootFromPanel();
                    TryBindVisualTree();
                    if (_visualTreeBound)
                    {
                        yield break;
                    }

                    yield return null;
                }
            }
            finally
            {
                _deferredBindCoroutine = null;
            }
        }

        private void TryBindVisualTree()
        {
            if (_visualTreeBound || _root == null)
            {
                return;
            }

            _visualTreeBound = true;

            // Add stylesheet if assigned
            if (_characterCreationStyleSheet != null && !_root.styleSheets.Contains(_characterCreationStyleSheet))
            {
                _root.styleSheets.Add(_characterCreationStyleSheet);
            }

            QueryUIElements();
            SetupEventHandlers();
            InitializeUIElements();
            _paneScrollBarBinder.BindTree(_root);

            RebindCachedOptionLists();
            if (_hasLastState)
            {
                UpdateView(_lastState);
            }

            if (!_isVisible)
            {
                Hide();
            }

            VisualTreeBound?.Invoke();
        }

        public void Initialize()
        {
            if (_panelRenderer == null)
            {
                _panelRenderer = GetComponent<PanelRenderer>();
            }

            if (_panelRenderer == null)
            {
                Debug.LogError("CharacterCreationView: PanelRenderer is missing.");
                return;
            }

            EnsurePanelReloadSubscription();
            ((IPanelComponent)_panelRenderer).PerformUpdate();
            TrySyncRootFromPanel();
            TryBindVisualTree();
            ScheduleDeferredBindIfNeeded();
        }

        private void QueryUIElements()
        {
            // Tab buttons and content
            _tabButtons = new Button[3]
            {
                _root.Q<Button>("tab-class"),
                _root.Q<Button>("tab-race"),
                _root.Q<Button>("tab-background")
            };

            _tabContents = new VisualElement[3]
            {
                _root.Q<VisualElement>("tab-class-content"),
                _root.Q<VisualElement>("tab-race-content"),
                _root.Q<VisualElement>("tab-background-content")
            };

            // Option button containers
            _classButtonsContainer = _root.Q<VisualElement>("class-buttons-container");
            _raceButtonsContainer = _root.Q<VisualElement>("race-buttons-container");
            _backgroundButtonsContainer = _root.Q<VisualElement>("background-buttons-container");

            // Ability score labels will be queried after they are created in InitializeAbilityStatRows
            _abilityScoreLabels = new Label[6];

            // Detail panel
            _detailName = _root.Q<Label>("detail-name");
            _detailType = _root.Q<Label>("detail-type");
            _detailSectionsHost = _root.Q<VisualElement>("detail-sections-host");
            _detailContent = _root.Q<Label>("detail-content");
            _featuresSection = _root.Q<VisualElement>("features-section");
            _featuresSectionTitle = _root.Q<Label>("features-section-title");
            _characterLevelTotalLabel = _root.Q<Label>("character-level-total");
            _detailClassLevelRow = _root.Q<VisualElement>("detail-class-level-row");
            _detailClassLevelHint = _root.Q<Label>("detail-class-level-hint");
            _detailClassLevelMinus = _root.Q<Button>("detail-class-level-minus");
            _detailClassLevelPlus = _root.Q<Button>("detail-class-level-plus");
            _detailClassLevelField = _root.Q<IntegerField>("detail-class-level-field");

            // Stats panel
            _abilityScoresGrid = _root.Q<VisualElement>("ability-scores-grid");
            _characterStatsGrid = _root.Q<VisualElement>("character-stats-grid");
            _spellcastingStatsGrid = _root.Q<VisualElement>("spellcasting-stats-grid");
            _physicalTraitsGrid = _root.Q<VisualElement>("physical-traits-grid");
            _proficiencyListHost = _root.Q<VisualElement>("proficiency-list-host");
            _rolledScoresPool = _root.Q<VisualElement>("rolled-scores-pool");
            _rolledScoresContainer = _root.Q<VisualElement>("rolled-scores-container");

            // Action buttons
            _cancelButton = _root.Q<Button>("cancel-button");
            _createButton = _root.Q<Button>("create-button");
            _rollButton = _root.Q<Button>("roll-abilities-button");
            _standardArrayButton = _root.Q<Button>("standard-array-button");
            _manualButton = _root.Q<Button>("manual-button");
            _pointBuyButton = _root.Q<Button>("point-buy-button");
            _pointBuyPointsLabel = _root.Q<Label>("point-buy-points-label");
            _confirmScoresButton = _root.Q<Button>("confirm-scores-button");
            _scoreMethodButtonsContainer = _root.Q<VisualElement>("score-method-buttons");
        }

        private void SetupEventHandlers()
        {
            // Tab buttons
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].clicked += () => SwitchTab(tabIndex);
                }
            }

            // Ability score input event handlers are set up in InitializeAbilityStatRows
            // after the inputs are dynamically created

            // Action buttons
            if (_cancelButton != null)
                _cancelButton.clicked += () => CancelClicked?.Invoke();

            if (_createButton != null)
                _createButton.clicked += () => CreateCharacterClicked?.Invoke();

            if (_rollButton != null)
                _rollButton.clicked += () => RollAbilitiesClicked?.Invoke();
            if (_standardArrayButton != null)
                _standardArrayButton.clicked += () => StandardArrayClicked?.Invoke();
            if (_manualButton != null)
                _manualButton.clicked += () => ManualClicked?.Invoke();
            if (_pointBuyButton != null)
                _pointBuyButton.clicked += () => PointBuyClicked?.Invoke();
            if (_confirmScoresButton != null)
                _confirmScoresButton.clicked += () => ConfirmScoresClicked?.Invoke();

            SetupDetailClassLevelControls();
        }

        private void SetupDetailClassLevelControls()
        {
            if (_detailClassLevelControlsHooked)
                return;
            _detailClassLevelControlsHooked = true;

            if (_detailClassLevelField != null)
            {
                _detailClassLevelField.label = string.Empty;
                _detailClassLevelField.RegisterValueChangedCallback(evt =>
                {
                    int cap = Mathf.Max(_detailClassLevelMaxCached, CharacterCreationModel.MinCharacterLevel);
                    int v = Mathf.Clamp(evt.newValue, CharacterCreationModel.MinCharacterLevel, cap);
                    if (v != evt.newValue)
                        _detailClassLevelField.SetValueWithoutNotify(v);
                    SelectedClassLevelChanged?.Invoke(v);
                });
            }

            if (_detailClassLevelMinus != null)
            {
                _detailClassLevelMinus.clicked += () =>
                {
                    int v = _detailClassLevelField != null
                        ? _detailClassLevelField.value
                        : CharacterCreationModel.MinCharacterLevel;
                    int next = Mathf.Max(CharacterCreationModel.MinCharacterLevel, v - 1);
                    _detailClassLevelField?.SetValueWithoutNotify(next);
                    SelectedClassLevelChanged?.Invoke(next);
                };
            }

            if (_detailClassLevelPlus != null)
            {
                _detailClassLevelPlus.clicked += () =>
                {
                    int v = _detailClassLevelField != null
                        ? _detailClassLevelField.value
                        : CharacterCreationModel.MinCharacterLevel;
                    int cap = Mathf.Max(_detailClassLevelMaxCached, CharacterCreationModel.MinCharacterLevel);
                    int next = Mathf.Min(cap, v + 1);
                    _detailClassLevelField?.SetValueWithoutNotify(next);
                    SelectedClassLevelChanged?.Invoke(next);
                };
            }
        }

        private void SwitchTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabButtons.Length || tabIndex >= _tabContents.Length)
                return;

            // Update tab buttons
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                {
                    if (i == tabIndex)
                        _tabButtons[i].AddToClassList("active");
                    else
                        _tabButtons[i].RemoveFromClassList("active");
                }
            }

            // Update tab content
            for (int i = 0; i < _tabContents.Length; i++)
            {
                if (_tabContents[i] != null)
                {
                    if (i == tabIndex)
                    {
                        _tabContents[i].AddToClassList("active");
                        _tabContents[i].style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        _tabContents[i].RemoveFromClassList("active");
                        _tabContents[i].style.display = DisplayStyle.None;
                    }
                }
            }
        }

        private void InitializeUIElements()
        {
            // Class / race / background option buttons: populated via BindRaceClassBackgroundOptions from the presenter.

            // Initialize stat display rows (created in UXML, just need to query labels)
            InitializeAbilityStatRows();
            InitializeCharacterStatItems();
            EnsureHitDiceStatRow();
            SetupDragAndDrop();
            
            // Hide rolled scores pool by default
            if (_rolledScoresPool != null)
            {
                _rolledScoresPool.style.display = DisplayStyle.None;
            }

            if (_detailClassLevelRow != null)
                _detailClassLevelRow.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Binds class, race, and background lists from ruleset content (stable ids; labels are display names).
        /// </summary>
        public void BindRaceClassBackgroundOptions(
            System.Collections.Generic.IReadOnlyList<(string id, string displayName)> classes,
            System.Collections.Generic.IReadOnlyList<(string id, string displayName)> races,
            System.Collections.Generic.IReadOnlyList<(string id, string displayName)> backgrounds)
        {
            _lastClassOptions = classes != null ? new List<(string id, string displayName)>(classes) : null;
            _lastRaceOptions = races != null ? new List<(string id, string displayName)>(races) : null;
            _lastBackgroundOptions = backgrounds != null ? new List<(string id, string displayName)>(backgrounds) : null;

            RebindCachedOptionLists();
        }

        private void RebindCachedOptionLists()
        {
            if (!_visualTreeBound)
                return;

            _classButtonsContainer?.Clear();
            _raceButtonsContainer?.Clear();
            _backgroundButtonsContainer?.Clear();
            BindOptionList(_classButtonsContainer, _lastClassOptions, id => ClassSelected?.Invoke(id));
            BindOptionList(_raceButtonsContainer, _lastRaceOptions, id => RaceSelected?.Invoke(id));
            BindOptionList(_backgroundButtonsContainer, _lastBackgroundOptions, id => BackgroundSelected?.Invoke(id));
        }

        private void BindOptionList(
            VisualElement container,
            System.Collections.Generic.IReadOnlyList<(string id, string displayName)> options,
            System.Action<string> onPick)
        {
            if (container == null || options == null)
                return;
            foreach ((string id, string displayName) in options)
            {
                string capturedId = id;
                CreateOptionButton(container, capturedId, displayName, () => onPick?.Invoke(capturedId));
            }
        }

        private static string SanitizeIdForElementName(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "empty";
            return id.Replace(".", "-").Replace(" ", "-").ToLowerInvariant();
        }

        private void CreateOptionButton(VisualElement parent, string id, string displayName, System.Action onClick)
        {
            if (parent == null)
                return;

            Button button = new Button();
            button.AddToClassList("character-creation-option-button");
            button.name = $"option-id-{SanitizeIdForElementName(id)}";
            button.text = displayName;
            button.userData = id;
            button.clicked += () => onClick?.Invoke();

            parent.Add(button);
        }

        private void InitializeAbilityStatRows()
        {
            if (_abilityScoresGrid == null) return;

            _abilityStatRowBinding = CreateAbilityStatRowBinding();

            VisualElement rowPrimary = _root.Q<VisualElement>(AbilityScoresRowPrimaryName);
            VisualElement rowSecondary = _root.Q<VisualElement>(AbilityScoresRowSecondaryName);

            if (rowPrimary != null && rowSecondary != null)
            {
                PopulateSplitAbilityRowsIfEmpty(rowPrimary, rowSecondary);
            }
            else if (_abilityScoresGrid.childCount == 0)
            {
                for (int i = 0; i < AbilityNamesDisplay.Length; i++)
                    _abilityScoresGrid.Add(CreateAbilityStatRow(AbilityNamesDisplay[i], i));
            }

            for (int i = 0; i < AbilityNamesShort.Length; i++)
                _abilityScoreLabels[i] = _root.Q<Label>($"ability-{AbilityNamesShort[i]}-score-label");
        }

        /// <summary>
        /// Split UXML: two rows of three tiles (STR–CON, INT–CHA).
        /// </summary>
        private void PopulateSplitAbilityRowsIfEmpty(VisualElement rowPrimary, VisualElement rowSecondary)
        {
            if (rowPrimary.childCount != 0 || rowSecondary.childCount != 0)
                return;

            for (int i = 0; i < PrimaryRowAbilityCount; i++)
            {
                VisualElement tile = CreateAbilityStatRow(AbilityNamesDisplay[i], i);
                if (i == PrimaryRowAbilityCount - 1)
                    tile.AddToClassList("character-creation-ability-scores-tile--row-end");
                rowPrimary.Add(tile);
            }

            for (int i = PrimaryRowAbilityCount; i < AbilityNamesDisplay.Length; i++)
            {
                VisualElement tile = CreateAbilityStatRow(AbilityNamesDisplay[i], i);
                if (i == AbilityNamesDisplay.Length - 1)
                    tile.AddToClassList("character-creation-ability-scores-tile--row-end");
                rowSecondary.Add(tile);
            }
        }

        private AbilityStatRowUiBinding CreateAbilityStatRowBinding()
        {
            return new AbilityStatRowUiBinding(
                i => PointBuyDecrementClicked?.Invoke(i),
                i => PointBuyIncrementClicked?.Invoke(i),
                (i, s) => ManualAbilityEntryChanged?.Invoke(i, s),
                () => _suppressManualAbilityEntryEvents);
        }

        private VisualElement CreateAbilityStatRow(string abilityName, int abilityIndex)
        {
            return AbilityStatRowViewFactory.CreateRow(abilityName, abilityIndex, _abilityStatRowBinding);
        }

        private void InitializeCharacterStatItems()
        {
            // Create character stat items if they don't exist (default to dash until stats are assigned)
            if (_characterStatsGrid != null && _characterStatsGrid.childCount == 0)
            {
                CreateCharacterStatItem(_characterStatsGrid, "Hit Points", "—", "hp-value", isModifier: false);
                CreateCharacterStatItem(_characterStatsGrid, "Hit Dice", "—", "hit-dice-value", isModifier: false);
                CreateCharacterStatItem(_characterStatsGrid, "Armor Class", "—", "ac-value", isModifier: false);
                CreateCharacterStatItem(_characterStatsGrid, "Initiative", "—", "initiative-value", isModifier: true);
                CreateCharacterStatItem(_characterStatsGrid, "Proficiency", "—", "proficiency-value", isModifier: true);
            }

            if (_spellcastingStatsGrid != null && _spellcastingStatsGrid.childCount == 0)
            {
                CreateCharacterStatItem(_spellcastingStatsGrid, "Spell Save DC", "—", "spell-save-dc-value", isModifier: false);
                CreateCharacterStatItem(_spellcastingStatsGrid, "Spell Attack", "—", "spell-attack-value", isModifier: true);
            }

            if (_physicalTraitsGrid != null && _physicalTraitsGrid.childCount == 0)
            {
                CreateCharacterStatItem(_physicalTraitsGrid, "Size", "Medium", "size-value");
                CreateCharacterStatItem(_physicalTraitsGrid, "Speed", "25 ft", "speed-value");
                CreateCharacterStatItem(_physicalTraitsGrid, "Darkvision", "60 ft", "darkvision-value");
            }
        }

        private void CreateCharacterStatItem(VisualElement parent, string label, string value, string valueName, bool isModifier = false)
        {
            if (parent == null) return;
            parent.Add(BuildCharacterStatRow(label, value, valueName, isModifier));
        }

        private static VisualElement BuildCharacterStatRow(string label, string value, string valueName, bool isModifier = false)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("character-creation-char-stat-item");

            Label labelElement = new Label(label);
            labelElement.AddToClassList("character-creation-char-stat-label");
            item.Add(labelElement);

            Label valueElement = new Label(value);
            valueElement.AddToClassList("character-creation-char-stat-value");
            if (isModifier)
                valueElement.AddToClassList("character-creation-char-stat-modifier");
            valueElement.name = valueName;
            item.Add(valueElement);

            return item;
        }

        /// <summary>
        /// Older layouts without a Hit Dice row get one inserted after Hit Points.
        /// </summary>
        private void EnsureHitDiceStatRow()
        {
            if (_characterStatsGrid == null || _root == null)
                return;
            if (_root.Q<Label>("hit-dice-value") != null)
                return;
            if (_characterStatsGrid.childCount == 0)
                return;
            VisualElement row = BuildCharacterStatRow("Hit Dice", "—", "hit-dice-value", isModifier: false);
            _characterStatsGrid.Insert(1, row);
        }

        public void Show()
        {
            _isVisible = true;

            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                _root.SetEnabled(true);
            }

            _paneScrollBarBinder.BindTree(_root);
        }

        public void Hide()
        {
            _isVisible = false;

            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                _root.SetEnabled(false);
            }
        }

        public void UpdateView(CharacterCreationState state)
        {
            _lastState = state;
            _hasLastState = true;

            if (_root == null || !_visualTreeBound) return;

            // Update visibility
            if (state.IsVisible)
                Show();
            else
                Hide();

            // Update selected options (content ids from ruleset JSON)
            UpdateOptionSelection(_classButtonsContainer, state.SelectedClassId);
            UpdateOptionSelection(_raceButtonsContainer, state.SelectedRaceId);
            UpdateOptionSelection(_backgroundButtonsContainer, state.SelectedBackgroundId);

            if (_characterLevelTotalLabel != null)
                _characterLevelTotalLabel.text = state.CharacterLevel.ToString();

            // When locked: hide pool, method buttons, and confirm button; show only final ability scores
            if (state.AbilityScoresLocked)
            {
                if (_rolledScoresPool != null) _rolledScoresPool.style.display = DisplayStyle.None;
                if (_scoreMethodButtonsContainer != null) _scoreMethodButtonsContainer.style.display = DisplayStyle.None;
                if (_confirmScoresButton != null) _confirmScoresButton.style.display = DisplayStyle.None;
                SetPointBuyControlsVisible(false);
                SetAbilityScoreDirectEntryVisible(false);
            }
            else
            {
                bool useDirectManualEntry = state.SelectedScoreMethod == "Manual" && state.IsManualMode;

                if (_scoreMethodButtonsContainer != null) _scoreMethodButtonsContainer.style.display = DisplayStyle.Flex;
                if (_confirmScoresButton != null) _confirmScoresButton.style.display = DisplayStyle.Flex;
                UpdateScoreMethodSelection(state.SelectedScoreMethod);
                if (state.SelectedScoreMethod == "PointBuy")
                {
                    if (_rolledScoresPool != null) _rolledScoresPool.style.display = DisplayStyle.Flex;
                    ShowPointBuyPool();
                    SetPointBuyControlsVisible(true);
                    SetAbilityScoreDirectEntryVisible(false);
                }
                else if (useDirectManualEntry)
                {
                    if (_rolledScoresPool != null) _rolledScoresPool.style.display = DisplayStyle.None;
                    HidePointBuyPool();
                    SetPointBuyControlsVisible(false);
                    SetAbilityScoreDirectEntryVisible(true);
                }
                else
                {
                    if (_rolledScoresPool != null) _rolledScoresPool.style.display = DisplayStyle.Flex;
                    HidePointBuyPool();
                    SetPointBuyControlsVisible(false);
                    SetAbilityScoreDirectEntryVisible(false);
                    UpdateRolledScores(state.RolledScores, state.AssignedRolledScoreIndices, state.IsManualMode, state.RolledDiceBreakdown, state.RolledDroppedIndices);
                }
            }

            bool syncDirectManualFields = !state.AbilityScoresLocked && state.SelectedScoreMethod == "Manual" && state.IsManualMode;

            // Update ability scores without triggering change events
            if (state.AbilityScores != null && state.AbilityScores.Length == 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    int score = state.AbilityScores[i];
                    VisualElement row = _root.Q<VisualElement>($"ability-stat-{AbilityNamesShort[i]}");

                    if (syncDirectManualFields)
                    {
                        TextField entryField = _root.Q<TextField>($"ability-{AbilityNamesShort[i]}-score-entry");
                        if (entryField != null)
                        {
                            string t = score < 0 ? "" : score.ToString();
                            if (entryField.value != t)
                            {
                                _suppressManualAbilityEntryEvents = true;
                                entryField.SetValueWithoutNotify(t);
                                _suppressManualAbilityEntryEvents = false;
                            }
                        }
                    }
                    else if (_abilityScoreLabels[i] != null)
                    {
                        if (score < 0)
                            _abilityScoreLabels[i].text = "";
                        else
                            _abilityScoreLabels[i].text = score.ToString();
                    }

                    if (row != null)
                    {
                        if (score < 0)
                        {
                            row.AddToClassList("unassigned");
                            row.RemoveFromClassList("assigned");
                        }
                        else
                        {
                            row.RemoveFromClassList("unassigned");
                            row.AddToClassList("assigned");
                        }
                    }
                }
            }
        }

        private void SetAbilityScoreDirectEntryVisible(bool visible)
        {
            if (_root == null) return;
            foreach (string shortName in AbilityNamesShort)
            {
                VisualElement dropZone = _root.Q<VisualElement>($"ability-{shortName}-drop-zone");
                TextField entry = _root.Q<TextField>($"ability-{shortName}-score-entry");
                if (dropZone != null)
                {
                    dropZone.style.display = DisplayStyle.Flex;
                    dropZone.style.visibility = visible ? Visibility.Hidden : Visibility.Visible;
                    dropZone.pickingMode = visible ? PickingMode.Ignore : PickingMode.Position;
                }
                if (entry != null)
                {
                    entry.style.display = DisplayStyle.Flex;
                    entry.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
                    entry.SetEnabled(visible);
                }
            }
        }

        private void UpdateOptionSelection(VisualElement container, string selectedId)
        {
            if (container == null || string.IsNullOrEmpty(selectedId))
                return;

            foreach (VisualElement element in container.Children())
            {
                if (element is Button button && button.userData is string uid)
                {
                    if (uid == selectedId)
                        button.AddToClassList("selected");
                    else
                        button.RemoveFromClassList("selected");
                }
            }
        }

        private void UpdateScoreMethodSelection(string selectedScoreMethod)
        {
            if (_rollButton != null)
            {
                if (selectedScoreMethod == "Roll")
                    _rollButton.AddToClassList("selected");
                else
                    _rollButton.RemoveFromClassList("selected");
            }
            if (_standardArrayButton != null)
            {
                if (selectedScoreMethod == "StandardArray")
                    _standardArrayButton.AddToClassList("selected");
                else
                    _standardArrayButton.RemoveFromClassList("selected");
            }
            if (_manualButton != null)
            {
                if (selectedScoreMethod == "Manual")
                    _manualButton.AddToClassList("selected");
                else
                    _manualButton.RemoveFromClassList("selected");
            }
            if (_pointBuyButton != null)
            {
                if (selectedScoreMethod == "PointBuy")
                    _pointBuyButton.AddToClassList("selected");
                else
                    _pointBuyButton.RemoveFromClassList("selected");
            }
        }

        private void ShowPointBuyPool()
        {
            if (_rolledScoresPool != null)
                _rolledScoresPool.style.display = DisplayStyle.Flex;
            if (_pointBuyPointsLabel != null)
                _pointBuyPointsLabel.style.display = DisplayStyle.Flex;
            if (_rolledScoresContainer != null)
                _rolledScoresContainer.style.display = DisplayStyle.None;
        }

        private void HidePointBuyPool()
        {
            if (_pointBuyPointsLabel != null)
                _pointBuyPointsLabel.style.display = DisplayStyle.None;
            if (_rolledScoresContainer != null)
                _rolledScoresContainer.style.display = DisplayStyle.Flex;
        }

        private void SetPointBuyControlsVisible(bool visible)
        {
            if (_abilityScoresGrid == null || _root == null) return;
            foreach (string name in AbilityNamesShort)
            {
                VisualElement minusContainer = _root.Q<VisualElement>($"ability-{name}-point-buy-minus");
                VisualElement plusContainer = _root.Q<VisualElement>($"ability-{name}-point-buy-plus");
                if (minusContainer != null)
                {
                    minusContainer.style.display = DisplayStyle.Flex;
                    minusContainer.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
                }
                if (plusContainer != null)
                {
                    plusContainer.style.display = DisplayStyle.Flex;
                    plusContainer.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        /// <summary>
        /// Updates the Point Buy pool label with remaining points. Called by Presenter with computed value.
        /// </summary>
        public void UpdatePointBuyPointsRemaining(int pointsRemaining)
        {
            if (_pointBuyPointsLabel != null)
                _pointBuyPointsLabel.text = pointsRemaining.ToString();
        }

        /// <summary>
        /// Updates Point Buy +/- button enabled states. Called by Presenter with computed values.
        /// </summary>
        public void UpdatePointBuyButtonStates(bool[] minusEnabled, bool[] plusEnabled)
        {
            if (minusEnabled == null || plusEnabled == null || _root == null) return;
            int count = Math.Min(minusEnabled.Length, Math.Min(plusEnabled.Length, AbilityNamesShort.Length));
            for (int i = 0; i < count; i++)
            {
                VisualElement minusContainer = _root.Q<VisualElement>($"ability-{AbilityNamesShort[i]}-point-buy-minus");
                VisualElement plusContainer = _root.Q<VisualElement>($"ability-{AbilityNamesShort[i]}-point-buy-plus");
                Button minusBtn = minusContainer?.Q<Button>();
                Button plusBtn = plusContainer?.Q<Button>();
                if (minusBtn != null) minusBtn.SetEnabled(minusEnabled[i]);
                if (plusBtn != null) plusBtn.SetEnabled(plusEnabled[i]);
            }
        }

        /// <summary>
        /// Updates the detail panel with provided information.
        /// Called by Presenter with calculated data.
        /// </summary>
        public void UpdateDetailPanel(
            string name,
            string type,
            string description,
            List<FeatureData> features,
            IReadOnlyList<CharacterDetailSection> detailSections = null,
            string featuresSectionHeading = null,
            bool descriptionHasLiveAbilityHints = false,
            bool showDetailClassLevel = false,
            int detailClassLevel = 1,
            int detailClassLevelMax = CharacterCreationModel.MaxCharacterLevel)
        {
            if (_detailName != null) _detailName.text = name ?? string.Empty;
            if (_detailType != null) _detailType.text = type ?? string.Empty;

            _detailClassLevelMaxCached = Mathf.Max(detailClassLevelMax, CharacterCreationModel.MinCharacterLevel);
            if (_detailClassLevelRow != null)
            {
                if (showDetailClassLevel)
                {
                    _detailClassLevelRow.style.display = DisplayStyle.Flex;
                    if (_detailClassLevelHint != null)
                        _detailClassLevelHint.text =
                            $"{CharacterCreationModel.MinCharacterLevel}–{_detailClassLevelMaxCached}";
                    if (_detailClassLevelField != null &&
                        _detailClassLevelField.value != detailClassLevel)
                        _detailClassLevelField.SetValueWithoutNotify(detailClassLevel);
                    if (_detailClassLevelMinus != null)
                        _detailClassLevelMinus.SetEnabled(detailClassLevel > CharacterCreationModel.MinCharacterLevel);
                    if (_detailClassLevelPlus != null)
                        _detailClassLevelPlus.SetEnabled(detailClassLevel < _detailClassLevelMaxCached);
                }
                else
                    _detailClassLevelRow.style.display = DisplayStyle.None;
            }
            if (_featuresSectionTitle != null)
                _featuresSectionTitle.text = string.IsNullOrEmpty(featuresSectionHeading)
                    ? "Special Features"
                    : featuresSectionHeading;

            bool useSections = detailSections != null && detailSections.Count > 0;
            if (_detailSectionsHost != null)
            {
                _detailSectionsHost.Clear();
                if (useSections)
                {
                    CharacterCreationDetailPanelBinder.PopulateSectionsHost(
                        _detailSectionsHost,
                        detailSections,
                        ConfigureRulesRichTextLabel);

                    if (_detailContent != null)
                    {
                        _detailContent.style.display = DisplayStyle.None;
                        _detailContent.RemoveFromClassList("character-creation-live-ability-hint");
                    }
                }
                else
                {
                    if (_detailContent != null)
                    {
                        _detailContent.style.display = DisplayStyle.Flex;
                        ApplyDetailContentDescription(description ?? string.Empty, descriptionHasLiveAbilityHints);
                    }
                }
            }
            else if (_detailContent != null)
            {
                ApplyDetailContentDescription(description ?? string.Empty, descriptionHasLiveAbilityHints);
            }

            ClearFeatures();
            if (features != null)
            {
                foreach (var feature in features)
                {
                    AddFeature(feature.Name, feature.Description, feature.HasLiveAbilityHints);
                }
            }
        }

        /// <summary>
        /// Enables rich text for rules content and optionally adds a highlighted block style when
        /// live ability substitutions are present.
        /// </summary>
        private static void ConfigureRulesRichTextLabel(Label label, bool emphasizeBlock)
        {
            if (label == null) return;
            label.enableRichText = true;
            if (emphasizeBlock)
                label.AddToClassList("character-creation-live-ability-hint");
        }

        private void ApplyDetailContentDescription(string description, bool emphasizeLiveAbility)
        {
            if (_detailContent == null) return;
            _detailContent.enableRichText = true;
            _detailContent.text = description;
            if (emphasizeLiveAbility)
                _detailContent.AddToClassList("character-creation-live-ability-hint");
            else
                _detailContent.RemoveFromClassList("character-creation-live-ability-hint");
        }

        /// <summary>
        /// Updates ability score displays with calculated values.
        /// Called by Presenter with calculated data.
        /// </summary>
        public void UpdateAbilityScoreDisplay(int index, int score, int modifier)
        {
            if (index < 0 || index >= AbilityNamesShort.Length) return;

            string abilityName = AbilityNamesShort[index];

            // Update score label value
            if (_abilityScoreLabels[index] != null)
            {
                VisualElement row = _root.Q<VisualElement>($"ability-stat-{abilityName}");
                
                if (score < 0)
                {
                    _abilityScoreLabels[index].text = ""; // Blank when unassigned
                    if (row != null)
                    {
                        row.AddToClassList("unassigned");
                        row.RemoveFromClassList("assigned");
                    }
                }
                else
                {
                    _abilityScoreLabels[index].text = score.ToString();
                    if (row != null)
                    {
                        row.RemoveFromClassList("unassigned");
                        row.AddToClassList("assigned");
                    }
                }
            }

            // Update modifier display (dash = neutral grey; positive = green; zero = grey; negative = red)
            Label modLabel = _root.Q<Label>($"ability-mod-{abilityName}");
            if (modLabel != null)
            {
                modLabel.RemoveFromClassList("character-creation-ability-mod-positive");
                modLabel.RemoveFromClassList("character-creation-ability-mod-neutral");
                modLabel.RemoveFromClassList("negative");
                if (score < 0)
                {
                    modLabel.text = "—";
                    modLabel.AddToClassList("character-creation-ability-mod-neutral");
                }
                else
                {
                    modLabel.text = modifier >= 0 ? $"+{modifier}" : modifier.ToString();
                    if (modifier > 0)
                        modLabel.AddToClassList("character-creation-ability-mod-positive");
                    else if (modifier < 0)
                        modLabel.AddToClassList("negative");
                    else
                        modLabel.AddToClassList("character-creation-ability-mod-neutral");
                }
            }
        }

        /// <summary>
        /// Updates derived character stats display.
        /// Called by Presenter with calculated data. Null values show "—" (dash).
        /// </summary>
        public void UpdateDerivedStats(int? hitPoints, int? armorClass, int? initiative, int? proficiencyBonus,
            int? spellSaveDC = null, int? spellAttack = null, string hitDiceDisplay = null)
        {
            UpdateStatLabel("hp-value", hitPoints.HasValue ? hitPoints.Value.ToString() : "—", isModifier: false);
            UpdateStatLabel("hit-dice-value", string.IsNullOrEmpty(hitDiceDisplay) ? "—" : hitDiceDisplay, isModifier: false);
            UpdateStatLabel("ac-value", armorClass.HasValue ? armorClass.Value.ToString() : "—", isModifier: false);
            UpdateStatLabel("initiative-value", initiative.HasValue ? (initiative.Value >= 0 ? $"+{initiative.Value}" : initiative.Value.ToString()) : "—", isModifier: true, modifierValue: initiative);
            UpdateStatLabel("proficiency-value", proficiencyBonus.HasValue ? (proficiencyBonus.Value >= 0 ? $"+{proficiencyBonus.Value}" : proficiencyBonus.Value.ToString()) : "—", isModifier: true, modifierValue: proficiencyBonus);

            UpdateStatLabel("spell-save-dc-value", spellSaveDC.HasValue ? spellSaveDC.Value.ToString() : "—", isModifier: false);
            UpdateStatLabel("spell-attack-value", spellAttack.HasValue ? (spellAttack.Value >= 0 ? $"+{spellAttack.Value}" : spellAttack.Value.ToString()) : "—", isModifier: true, modifierValue: spellAttack);
        }

        /// <summary>
        /// Renders class / background proficiencies as grouped tags. Pass null or empty for a placeholder.
        /// </summary>
        public void UpdateProficiencyPanel(IReadOnlyList<CharacterProficiencySection> sections)
        {
            if (_proficiencyListHost == null)
                return;

            _proficiencyListHost.Clear();
            if (sections == null || sections.Count == 0)
            {
                var hint = new Label("Select a class or background to see proficiencies.");
                hint.AddToClassList("character-creation-proficiency-placeholder");
                _proficiencyListHost.Add(hint);
                return;
            }

            foreach (CharacterProficiencySection section in sections)
            {
                if (string.IsNullOrEmpty(section.CategoryTitle))
                    continue;

                var cat = new VisualElement();
                cat.AddToClassList("character-creation-proficiency-category");

                var title = new Label(section.CategoryTitle);
                title.AddToClassList("character-creation-proficiency-category-title");
                cat.Add(title);

                var list = new VisualElement();
                list.AddToClassList("character-creation-proficiency-list");

                if (section.Items != null)
                {
                    foreach (string item in section.Items)
                    {
                        if (string.IsNullOrEmpty(item))
                            continue;
                        var tag = new VisualElement();
                        tag.AddToClassList("character-creation-proficiency-tag");
                        tag.Add(new Label(item));
                        list.Add(tag);
                    }
                }

                cat.Add(list);
                _proficiencyListHost.Add(cat);
            }
        }

        /// <summary>
        /// Updates size, speed, and darkvision labels (e.g. from loaded race definition).
        /// </summary>
        public void UpdatePhysicalTraits(string size, string speed, string darkvision)
        {
            UpdateStatLabel("size-value", string.IsNullOrEmpty(size) ? "—" : size, isModifier: false);
            UpdateStatLabel("speed-value", string.IsNullOrEmpty(speed) ? "—" : speed, isModifier: false);
            UpdateStatLabel("darkvision-value", string.IsNullOrEmpty(darkvision) ? "—" : darkvision, isModifier: false);
        }

        private void UpdateStatLabel(string labelName, string value, bool isModifier = false, int? modifierValue = null)
        {
            Label label = _root.Q<Label>(labelName);
            if (label == null) return;

            label.text = value;

            if (!isModifier) return;

            label.RemoveFromClassList("character-creation-char-stat-positive");
            label.RemoveFromClassList("character-creation-char-stat-neutral");
            label.RemoveFromClassList("character-creation-char-stat-negative");
            if (!modifierValue.HasValue || value == "—")
                label.AddToClassList("character-creation-char-stat-neutral");
            else if (modifierValue.Value > 0)
                label.AddToClassList("character-creation-char-stat-positive");
            else if (modifierValue.Value < 0)
                label.AddToClassList("character-creation-char-stat-negative");
            else
                label.AddToClassList("character-creation-char-stat-neutral");
        }

        private void AddFeature(string name, string description, bool hasLiveAbilityHints = false)
        {
            if (_featuresSection == null) return;

            VisualElement feature = new VisualElement();
            feature.AddToClassList("character-creation-feature-item");
            if (hasLiveAbilityHints)
                feature.AddToClassList("character-creation-feature-item--live-stats");

            Label nameLabel = new Label(name);
            nameLabel.AddToClassList("character-creation-feature-name");
            feature.Add(nameLabel);

            Label descLabel = new Label(description);
            descLabel.AddToClassList("character-creation-feature-description");
            ConfigureRulesRichTextLabel(descLabel, hasLiveAbilityHints);
            feature.Add(descLabel);

            _featuresSection.Add(feature);
        }

        private void ClearFeatures()
        {
            if (_featuresSection == null) return;
            _featuresSection.Clear();
        }

        private void SetupDragAndDrop()
        {
            // Setup ability rows to raise drag events
            if (_abilityScoresGrid != null)
            {
                for (int i = 0; i < AbilityNamesShort.Length; i++)
                {
                    VisualElement row = _root.Q<VisualElement>($"ability-stat-{AbilityNamesShort[i]}");
                    if (row != null)
                    {
                        int abilityIndex = i; // Capture for closure
                        SetupDropZone(row, abilityIndex);
                    }
                }
            }

            // Register global pointer move and up to notify Presenter
            if (_root != null)
            {
                _root.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
                _root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
            }
        }


        private void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            // If we have a pending manual drag and pointer moved enough, start the drag
            if (_pendingManualDragIndex >= 0)
            {
                float dx = evt.position.x - _pendingManualDragPosition.x;
                float dy = evt.position.y - _pendingManualDragPosition.y;
                if (dx * dx + dy * dy > 64f) // 8px threshold
                {
                    DragStartedFromRolledScore?.Invoke(_pendingManualDragIndex, _pendingManualDragValue);
                    _pendingManualDragIndex = -1;
                }
            }

            // Update drag preview position if it exists
            if (_dragPreview != null && _root != null)
            {
                Vector2 screenPos = evt.position;
                Rect rootRect = _root.worldBound;
                Vector2 localPos = new Vector2(screenPos.x - rootRect.x, screenPos.y - rootRect.y);
                _dragPreview.style.left = localPos.x - 25;
                _dragPreview.style.top = localPos.y - 25;
            }
        }

        private void OnGlobalPointerUp(PointerUpEvent evt)
        {
            _pendingManualDragIndex = -1;
            DropOccurred?.Invoke(evt.position);
        }


        private void RemoveDragPreview()
        {
            if (_dragPreview != null && _root != null)
            {
                _root.Remove(_dragPreview);
                _dragPreview = null;
            }
        }

        // ========== Public UI Update Methods (Called by Presenter) ==========

        /// <summary>
        /// Shows a drag preview element with the given score value.
        /// Called by Presenter when drag starts.
        /// </summary>
        public void ShowDragPreview(int scoreValue)
        {
            if (_root == null) return;
            RemoveDragPreview();

            _dragPreview = new VisualElement();
            _dragPreview.AddToClassList("character-creation-rolled-score-item");
            _dragPreview.AddToClassList("drag-preview");
            _dragPreview.style.position = Position.Absolute;
            _dragPreview.style.left = 0;
            _dragPreview.style.top = 0;
            _dragPreview.pickingMode = PickingMode.Ignore;
            _dragPreview.focusable = false;

            Label valueLabel = new Label(scoreValue.ToString());
            valueLabel.AddToClassList("character-creation-rolled-score-value");
            valueLabel.pickingMode = PickingMode.Ignore;
            _dragPreview.Add(valueLabel);

            _root.Add(_dragPreview);
            _dragPreview.BringToFront();
        }

        /// <summary>
        /// Updates the drag preview position to follow the cursor.
        /// Called by Presenter during drag.
        /// </summary>
        public void UpdateDragPreviewPosition(Vector2 position)
        {
            if (_dragPreview == null || _root == null) return;

            Vector2 screenPos = position;
            Rect rootRect = _root.worldBound;
            Vector2 localPos = new Vector2(screenPos.x - rootRect.x, screenPos.y - rootRect.y);

            _dragPreview.style.left = localPos.x - 25;
            _dragPreview.style.top = localPos.y - 25;
        }

        /// <summary>
        /// Hides the drag preview.
        /// Called by Presenter when drag ends.
        /// </summary>
        public void HideDragPreview()
        {
            RemoveDragPreview();
        }

        /// <summary>
        /// Highlights a drop zone to indicate it can accept a drop.
        /// Called by Presenter during drag.
        /// </summary>
        public void HighlightDropZone(int abilityIndex)
        {
            if (_abilityScoresGrid == null) return;
            if (abilityIndex < 0 || abilityIndex >= AbilityNamesShort.Length) return;

            VisualElement row = _root.Q<VisualElement>($"ability-stat-{AbilityNamesShort[abilityIndex]}");
            if (row != null)
            {
                row.AddToClassList("drag-over");
            }
        }

        /// <summary>
        /// Highlights the rolled scores container as a drop zone.
        /// Called by Presenter during drag from ability.
        /// </summary>
        public void HighlightRolledScoresContainer()
        {
            if (_rolledScoresContainer != null)
            {
                _rolledScoresContainer.AddToClassList("drag-over");
            }
        }

        /// <summary>
        /// Clears all drop zone highlights.
        /// Called by Presenter when drag ends or moves away.
        /// </summary>
        public void ClearDropZoneHighlights()
        {
            AbilityScoresGridTraversal.ForEachStatRow(_abilityScoresGrid,
                row => row.RemoveFromClassList("drag-over"));

            if (_rolledScoresContainer != null)
                _rolledScoresContainer.RemoveFromClassList("drag-over");
        }

        /// <summary>
        /// Marks a visual element as being dragged (adds dragging class).
        /// Called by Presenter when drag starts.
        /// </summary>
        public void MarkElementAsDragging(VisualElement element)
        {
            if (element != null)
            {
                element.AddToClassList("dragging");
            }
        }

        /// <summary>
        /// Unmarks a visual element as being dragged (removes dragging class).
        /// Called by Presenter when drag ends.
        /// </summary>
        public void UnmarkElementAsDragging(VisualElement element)
        {
            if (element != null)
            {
                element.RemoveFromClassList("dragging");
            }
        }

        private void SetupDropZone(VisualElement dropZone, int abilityIndex)
        {
            // Allow dragging from ability row - just raise event, Presenter handles logic
            Label scoreLabel = dropZone.Q<Label>($"ability-{dropZone.userData.ToString().ToLower()}-score-label");
            if (scoreLabel != null)
            {
                scoreLabel.RegisterCallback<PointerDownEvent>(evt =>
                {
                    // Check if this ability has an assigned score (label text is not empty)
                    if (!string.IsNullOrEmpty(scoreLabel.text) && evt.button == 0)
                    {
                        DragStartedFromAbility?.Invoke(abilityIndex);
                        evt.StopPropagation();
                    }
                });
            }
        }

    }
}

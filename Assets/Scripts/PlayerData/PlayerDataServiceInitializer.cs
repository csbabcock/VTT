using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// MonoBehaviour component that initializes the player data service.
    /// Supports both JSON files and ScriptableObjects.
    /// 
    /// Usage:
    /// 1. Create a JSON character file in StreamingAssets/Characters/ (recommended)
    ///    OR create a PlayerData ScriptableObject asset
    /// 2. Add this component to a GameObject in your scene
    /// 3. Configure the data source in the Inspector
    /// 4. The service will automatically use this data when the scene starts
    /// </summary>
    public class PlayerDataServiceInitializer : MonoBehaviour
    {
        public enum DataSourceType
        {
            JSON,
            ScriptableObject,
            Default
        }

        [Header("Data Source")]
        [Tooltip("Where to load character data from")]
        [SerializeField] private DataSourceType _dataSource = DataSourceType.JSON;

        [Header("JSON Configuration")]
        [Tooltip("Path to JSON file relative to StreamingAssets (e.g., 'Characters/MyCharacter.json')")]
        [SerializeField] private string _jsonFilePath = "Characters/ExampleCharacter.json";

        [Header("ScriptableObject Configuration")]
        [Tooltip("Character data asset to load. Only used if Data Source is ScriptableObject.")]
        [SerializeField] private PlayerDataAsset _playerDataAsset;

        [Header("Initialization")]
        [Tooltip("Initialize on Awake (before other scripts). Recommended: true")]
        [SerializeField] private bool _initializeOnAwake = true;

        [Tooltip("Initialize on Start (after Awake). Use if other scripts need to initialize first.")]
        [SerializeField] private bool _initializeOnStart = false;

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                InitializeService();
            }
        }

        private void Start()
        {
            if (_initializeOnStart)
            {
                InitializeService();
            }
        }

        /// <summary>
        /// Initializes the player data service based on the configured data source.
        /// Can be called manually if needed.
        /// </summary>
        public void InitializeService()
        {
            IPlayerDataService service = null;

            switch (_dataSource)
            {
                case DataSourceType.JSON:
                    if (!string.IsNullOrEmpty(_jsonFilePath))
                    {
                        service = new JsonPlayerDataService(_jsonFilePath);
                        Debug.Log($"PlayerDataService initialized from JSON: {_jsonFilePath}");
                    }
                    else
                    {
                        Debug.LogWarning("PlayerDataServiceInitializer: JSON file path is empty. Using default character data.");
                        service = new LocalPlayerDataService();
                    }
                    break;

                case DataSourceType.ScriptableObject:
                    service = new LocalPlayerDataService(_playerDataAsset);
                    Debug.Log($"PlayerDataService initialized from ScriptableObject: {( _playerDataAsset != null ? _playerDataAsset.name : "null")}");
                    break;

                case DataSourceType.Default:
                default:
                    service = new LocalPlayerDataService();
                    Debug.Log("PlayerDataService initialized with default values");
                    break;
            }

            // Set it as the service locator's service
            PlayerDataServiceLocator.Service = service;
        }

        /// <summary>
        /// Gets the currently configured JSON file path.
        /// </summary>
        public string GetJsonFilePath()
        {
            return _jsonFilePath;
        }

        /// <summary>
        /// Sets a new JSON file path and reinitializes the service.
        /// Useful for runtime character switching.
        /// </summary>
        public void SetJsonFilePath(string filePath)
        {
            _jsonFilePath = filePath;
            _dataSource = DataSourceType.JSON;
            InitializeService();
        }

        /// <summary>
        /// Gets the currently assigned player data asset.
        /// </summary>
        public PlayerDataAsset GetPlayerDataAsset()
        {
            return _playerDataAsset;
        }

        /// <summary>
        /// Sets a new player data asset and reinitializes the service.
        /// Useful for runtime character switching.
        /// </summary>
        public void SetPlayerDataAsset(PlayerDataAsset asset)
        {
            _playerDataAsset = asset;
            _dataSource = DataSourceType.ScriptableObject;
            InitializeService();
        }
    }
}


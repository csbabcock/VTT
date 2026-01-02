using UnityEngine;

namespace GameCore.PlayerData
{
    /// <summary>
    /// MonoBehaviour component that initializes the player data service.
    /// Loads character data from JSON files.
    /// 
    /// Usage:
    /// 1. Create a JSON character file in StreamingAssets/Characters/
    /// 2. Add this component to a GameObject in your scene
    /// 3. Enter the JSON file path (e.g., "Characters/MyCharacter.json")
    /// 4. The service will automatically initialize on Awake
    /// </summary>
    public class PlayerDataServiceInitializer : MonoBehaviour
    {
        [Header("JSON Configuration")]
        [Tooltip("Path to JSON file relative to StreamingAssets (e.g., 'Characters/MyCharacter.json'). Leave empty to use default character data.")]
        [SerializeField] private string _jsonFilePath = "Characters/ExampleCharacter.json";

        private void Awake()
        {
            InitializeService();
        }

        /// <summary>
        /// Initializes the player data service from JSON file or default values.
        /// Can be called manually if needed.
        /// </summary>
        public void InitializeService()
        {
            IPlayerDataService service;

            if (!string.IsNullOrEmpty(_jsonFilePath))
            {
                service = new JsonPlayerDataService(_jsonFilePath);
                Debug.Log($"PlayerDataService initialized from JSON: {_jsonFilePath}");
            }
            else
            {
                service = new LocalPlayerDataService();
                Debug.Log("PlayerDataService initialized with default values");
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
            InitializeService();
        }
    }
}


using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.Networking
{
    /// <summary>
    /// Server-authoritative player spawner that defers creating each client's player object until
    /// <b>after</b> that client has finished loading the gameplay scene.
    ///
    /// Why this exists: with NGO's automatic "Player Prefab" spawning, the host's player is created
    /// during <c>StartHost()</c> — before the gameplay scene (and its floor colliders) have loaded.
    /// The host therefore spawns into an empty world and falls through the not-yet-existent floor.
    /// By taking over spawning and gating it on the scene-load events, every participant (host/DM and
    /// joining players alike) only appears once their copy of the scene is ready.
    ///
    /// Place this on the same GameObject as the <see cref="NetworkManager"/> (the MainMenu scene).
    /// It reads the NetworkManager's assigned Player Prefab automatically and disables auto-spawn,
    /// so no extra wiring is required beyond adding this component.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkPlayerSpawner : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Player prefab to spawn per client. If left empty, the NetworkManager's assigned " +
                 "Player Prefab is used automatically.")]
        [SerializeField] private GameObject _playerPrefab;

        [Header("Scene")]
        [Tooltip("Players are spawned only once this scene has finished loading on their machine. " +
                 "Kept in sync automatically with the scene the session launcher loads.")]
        [SerializeField] private string _gameSceneName = "Playground";

        [Header("Spawn placement")]
        [Tooltip("Fallback spawn position used when the scene contains no PlayerSpawnPoint markers.")]
        [SerializeField] private Vector3 _defaultSpawnPosition = new Vector3(0f, 1f, 0f);

        private NetworkManager _networkManager;
        private readonly HashSet<ulong> _spawned = new HashSet<ulong>();
        private bool _prefabRegistered;
        private int _spawnIndex;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            if (_networkManager == null)
                return;

            // Fall back to whatever prefab is wired into the NetworkManager so the user doesn't
            // have to assign it twice.
            if (_playerPrefab == null && _networkManager.NetworkConfig != null)
                _playerPrefab = _networkManager.NetworkConfig.PlayerPrefab;

            // Take over spawning: clearing the auto Player Prefab stops NGO from creating the
            // host's player during StartHost (before the scene/floor exists). We register the prefab
            // ourselves below so it can still be spawned manually. This runs identically on the host
            // and every client (the component lives in the shared MainMenu scene), keeping the
            // registered prefab set in sync across peers.
            if (_networkManager.NetworkConfig != null)
                _networkManager.NetworkConfig.PlayerPrefab = null;

            RegisterPrefab();
        }

        private void OnEnable()
        {
            if (_networkManager == null)
                _networkManager = GetComponent<NetworkManager>();
            if (_networkManager == null)
                return;

            _networkManager.OnServerStarted += HandleServerStarted;
            _networkManager.OnServerStopped += HandleServerStopped;
        }

        private void OnDisable()
        {
            if (_networkManager == null)
                return;

            _networkManager.OnServerStarted -= HandleServerStarted;
            _networkManager.OnServerStopped -= HandleServerStopped;
        }

        /// <summary>
        /// Keeps the spawn-gating scene name aligned with the scene the session launcher actually loads.
        /// </summary>
        public void SetGameScene(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName))
                _gameSceneName = sceneName;
        }

        private void RegisterPrefab()
        {
            if (_prefabRegistered || _networkManager == null || _playerPrefab == null)
                return;

            if (_playerPrefab.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError("NetworkPlayerSpawner: Player prefab must have a NetworkObject component.");
                return;
            }

            // Only register if it isn't already a known network prefab (e.g. still present in a
            // NetworkPrefabsList), otherwise AddNetworkPrefab logs a duplicate error.
            if (_networkManager.NetworkConfig != null &&
                _networkManager.NetworkConfig.Prefabs != null &&
                !_networkManager.NetworkConfig.Prefabs.Contains(_playerPrefab))
            {
                _networkManager.AddNetworkPrefab(_playerPrefab);
            }

            _prefabRegistered = true;
        }

        private void HandleServerStarted()
        {
            if (_networkManager == null || !_networkManager.IsServer || _networkManager.SceneManager == null)
                return;

            _spawned.Clear();

            // Fires for the host and for any client that loads the scene via NGO scene management.
            _networkManager.SceneManager.OnLoadComplete += HandleLoadComplete;
            // Fires for late-joining clients once initial synchronization completes (never for the host).
            _networkManager.SceneManager.OnSynchronizeComplete += HandleSynchronizeComplete;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        private void HandleServerStopped(bool _)
        {
            if (_networkManager == null)
                return;

            if (_networkManager.SceneManager != null)
            {
                _networkManager.SceneManager.OnLoadComplete -= HandleLoadComplete;
                _networkManager.SceneManager.OnSynchronizeComplete -= HandleSynchronizeComplete;
            }
            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
            _spawned.Clear();
        }

        private void HandleLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName == _gameSceneName)
                SpawnPlayerFor(clientId);
        }

        private void HandleSynchronizeComplete(ulong clientId)
        {
            // By this point the late-joining client has loaded the active gameplay scene.
            SpawnPlayerFor(clientId);
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            _spawned.Remove(clientId);
        }

        private void SpawnPlayerFor(ulong clientId)
        {
            if (_networkManager == null || !_networkManager.IsServer)
                return;

            if (!NetworkPlayerSpawnPolicy.ShouldSpawnPlayerObject(clientId, NetworkManager.ServerClientId))
                return;

            if (_playerPrefab == null)
            {
                Debug.LogError("NetworkPlayerSpawner: No player prefab assigned (and none found on the " +
                               "NetworkManager); cannot spawn player.");
                return;
            }

            // Dedup: OnLoadComplete and OnSynchronizeComplete can both fire for the same client.
            if (!_spawned.Add(clientId))
                return;

            GetSpawnPose(out Vector3 position, out Quaternion rotation);
            GameObject instance = Instantiate(_playerPrefab, position, rotation);

            var networkObject = instance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError("NetworkPlayerSpawner: Spawned instance is missing a NetworkObject.");
                Destroy(instance);
                _spawned.Remove(clientId);
                return;
            }

            networkObject.SpawnAsPlayerObject(clientId);
        }

        private void GetSpawnPose(out Vector3 position, out Quaternion rotation)
        {
            var points = PlayerSpawnPoint.ActivePoints;
            if (points.Count > 0)
            {
                Transform point = points[_spawnIndex % points.Count].transform;
                _spawnIndex++;
                position = point.position;
                rotation = point.rotation;
                return;
            }

            position = _defaultSpawnPosition;
            rotation = Quaternion.identity;
        }
    }
}

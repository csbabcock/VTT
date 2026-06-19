using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.Networking
{
    /// <summary>
    /// Netcode for GameObjects implementation of <see cref="ISessionLauncher"/>.
    ///
    /// Place this on the same GameObject as the <see cref="NetworkManager"/> in the
    /// MainMenu scene. The NetworkManager persists across scene loads, so hosting
    /// transitions into the gameplay scene via NGO scene management (which replicates
    /// the load to connecting clients).
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkSessionLauncher : MonoBehaviour, ISessionLauncher
    {
        [Header("Defaults")]
        [SerializeField] private string _defaultGameSceneName = "Playground";
        [SerializeField] private string _defaultAddress = "127.0.0.1";
        [SerializeField] private ushort _defaultPort = 7777;

        private NetworkManager _networkManager;
        private string _pendingGameScene;

        public string Address { get; set; }
        public ushort Port { get; set; }

        public bool IsActive =>
            _networkManager != null && (_networkManager.IsServer || _networkManager.IsClient);

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            Address = _defaultAddress;
            Port = _defaultPort;
            SessionLauncherLocator.Current = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(SessionLauncherLocator.Current, this))
                SessionLauncherLocator.Current = null;
        }

        public bool StartHost(string gameSceneName = null)
        {
            if (_networkManager == null)
            {
                Debug.LogError("NetworkSessionLauncher: NetworkManager is missing.");
                return false;
            }

            ConfigureTransport(Address, Port);
            _pendingGameScene = string.IsNullOrEmpty(gameSceneName) ? _defaultGameSceneName : gameSceneName;

            // Keep the deferred player spawner gated on the same scene we're about to load so the
            // host/DM only spawns once the floor exists.
            var spawner = GetComponent<NetworkPlayerSpawner>();
            if (spawner != null)
                spawner.SetGameScene(_pendingGameScene);

            // The host acts as the Dungeon Master / game authority.
            SessionRoleLocator.LocalRole = SessionRole.DungeonMaster;

            _networkManager.OnServerStarted += HandleServerStarted;
            bool started = _networkManager.StartHost();
            if (!started)
                _networkManager.OnServerStarted -= HandleServerStarted;

            return started;
        }

        public bool StartClient(string address = null, ushort port = 0)
        {
            if (_networkManager == null)
            {
                Debug.LogError("NetworkSessionLauncher: NetworkManager is missing.");
                return false;
            }

            ConfigureTransport(string.IsNullOrEmpty(address) ? Address : address, port == 0 ? Port : port);

            // A joining client plays as a regular player.
            SessionRoleLocator.LocalRole = SessionRole.Player;

            return _networkManager.StartClient();
        }

        public void Shutdown()
        {
            if (_networkManager != null && IsActive)
                _networkManager.Shutdown();
        }

        private void HandleServerStarted()
        {
            _networkManager.OnServerStarted -= HandleServerStarted;

            // Use NGO scene management so clients that connect afterward are
            // synchronized into this scene automatically.
            if (_networkManager.SceneManager != null && !string.IsNullOrEmpty(_pendingGameScene))
            {
                _networkManager.SceneManager.LoadScene(_pendingGameScene, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("NetworkSessionLauncher: Scene management unavailable; " +
                                 "ensure 'Enable Scene Management' is on and the scene is in Build Settings.");
            }
        }

        private void ConfigureTransport(string address, ushort port)
        {
            var transport = _networkManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("NetworkSessionLauncher: UnityTransport component not found on the NetworkManager GameObject.");
                return;
            }

            // Safety net: if the transport wasn't wired into NetworkManager's config in the
            // Inspector (or the scene wasn't saved before launching a clone), assign it here
            // so StartHost/StartClient don't fail with "No transport has been selected!".
            if (_networkManager.NetworkConfig != null && _networkManager.NetworkConfig.NetworkTransport == null)
            {
                _networkManager.NetworkConfig.NetworkTransport = transport;
                Debug.Log("NetworkSessionLauncher: Transport was not assigned in NetworkConfig; auto-assigned UnityTransport at runtime.");
            }

            transport.SetConnectionData(address, port);
            Debug.Log($"NetworkSessionLauncher: Transport configured for {address}:{port} (transport set = {_networkManager.NetworkConfig?.NetworkTransport != null}).");
        }
    }
}

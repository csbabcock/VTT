using GameCore.PlayerData;
using UnityEngine;

namespace GameCore.Actors
{
    /// <summary>
    /// Thin MonoBehaviour that marks a GameObject as a player participant and binds it
    /// to an <see cref="IPlayerDataService"/> (its character sheet source).
    ///
    /// Add this component to the player GameObject (alongside PlayerController). It
    /// self-registers with <see cref="ActorRegistry"/> while enabled. Until networking
    /// is added it defaults to the local owner and the global
    /// <see cref="PlayerDataServiceLocator.Service"/>, preserving current behavior; a
    /// networked spawner can later call <see cref="SetOwner"/> / <see cref="SetDataService"/>
    /// so each client's actor carries its own sheet.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerActor : MonoBehaviour, IActor
    {
        [SerializeField]
        [Tooltip("Owner/seat id controlling this actor. 0 = local/host until networking assigns real ids.")]
        private int _ownerId = 0;

        [SerializeField]
        [Tooltip("Whether this actor is controlled by the local machine's player.")]
        private bool _isLocalPlayer = true;

        private IPlayerDataService _dataService;
        private string _displayNameOverride;
        private bool _ownershipResolved;
        private bool _attemptedOfflineOwnership;

        public int OwnerId => _ownerId;

        public bool IsLocalPlayer => _isLocalPlayer;

        /// <summary>True after <see cref="SetOwner"/> or offline ownership resolution.</summary>
        public bool IsOwnershipResolved => _ownershipResolved;

        /// <summary>
        /// The data service backing this actor's sheet. An explicitly assigned service
        /// (e.g. injected server-side from the client's transmitted character) always
        /// wins. Otherwise only the resolved local player's actor falls back to the global
        /// <see cref="PlayerDataServiceLocator.Service"/>; remote actors return null so
        /// they never masquerade as the host's character before ownership is assigned.
        /// </summary>
        public IPlayerDataService DataService =>
            _dataService ?? (CanUseGlobalDataService ? PlayerDataServiceLocator.Service : null);

        public ICharacterSheet Sheet => DataService?.GetCharacterSheet();

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayNameOverride))
                    return _displayNameOverride;

                if (_dataService != null || CanUseGlobalDataService)
                {
                    string name = Sheet?.CharacterName;
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }

                if (_ownershipResolved)
                    return $"Player {_ownerId}";

                return gameObject.name;
            }
        }

        private bool CanUseGlobalDataService => _ownershipResolved && _isLocalPlayer;

        public Transform Transform => transform;

        /// <summary>Assigns the data service that backs this actor's sheet.</summary>
        public void SetDataService(IPlayerDataService service)
        {
            _dataService = service;
            ActorRegistry.NotifyActorUpdated(this);
        }

        /// <summary>
        /// Sets a display name independent of the sheet (used to show replicated names
        /// for remote players whose full sheet only exists on the server).
        /// </summary>
        public void SetDisplayName(string displayName)
        {
            _displayNameOverride = displayName;
            ActorRegistry.NotifyActorUpdated(this);
        }

        /// <summary>Assigns ownership for this actor (used by the networked spawner).</summary>
        public void SetOwner(int ownerId, bool isLocalPlayer)
        {
            _ownerId = ownerId;
            _isLocalPlayer = isLocalPlayer;
            _ownershipResolved = true;

            // Ownership is assigned after OnEnable registration, so let the registry
            // re-evaluate which actor is the local player.
            ActorRegistry.NotifyOwnershipChanged(this);
            ActorRegistry.NotifyActorUpdated(this);
        }

        private void OnEnable()
        {
            ActorRegistry.Register(this);
        }

        private void LateUpdate()
        {
            ResolveOfflineOwnershipIfNeeded();
        }

        /// <summary>
        /// Offline/direct-scene play has no network spawn; apply serialized defaults once.
        /// </summary>
        private void ResolveOfflineOwnershipIfNeeded()
        {
            if (_ownershipResolved || _attemptedOfflineOwnership)
                return;

            _attemptedOfflineOwnership = true;

            if (NetworkSessionProbe.IsNetworkListening())
                return;

            SetOwner(_ownerId, _isLocalPlayer);
            TryEnsureSpawnFullHealth();
        }

        private void TryEnsureSpawnFullHealth()
        {
            var sheet = DataService?.GetCharacterSheet() as DnD5eCharacterData;
            if (sheet == null)
                return;

            CharacterHitPoints.EnsureFullHealth(sheet);
            NotifyDataServiceChanged();
            ActorRegistry.NotifyActorUpdated(this);
        }

        private void NotifyDataServiceChanged()
        {
            switch (DataService)
            {
                case InMemoryPlayerDataService inMemory:
                    inMemory.NotifyChanged();
                    break;
                case JsonPlayerDataService json:
                    json.NotifyChanged();
                    break;
                case LocalPlayerDataService local:
                    local.NotifyChanged();
                    break;
            }
        }

        private void OnDisable()
        {
            ActorRegistry.Unregister(this);
        }
    }
}

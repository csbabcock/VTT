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

        public int OwnerId => _ownerId;

        public bool IsLocalPlayer => _isLocalPlayer;

        /// <summary>
        /// The data service backing this actor's sheet. An explicitly assigned service
        /// (e.g. injected server-side from the client's transmitted character) always
        /// wins. Otherwise only the local player's actor falls back to the global
        /// <see cref="PlayerDataServiceLocator.Service"/>; remote actors return null so
        /// they never masquerade as the local character.
        /// </summary>
        public IPlayerDataService DataService =>
            _dataService ?? (_isLocalPlayer ? PlayerDataServiceLocator.Service : null);

        public ICharacterSheet Sheet => DataService?.GetCharacterSheet();

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayNameOverride))
                    return _displayNameOverride;

                string name = Sheet?.CharacterName;
                if (!string.IsNullOrEmpty(name))
                    return name;

                return _isLocalPlayer ? gameObject.name : $"Player {_ownerId}";
            }
        }

        public Transform Transform => transform;

        /// <summary>Assigns the data service that backs this actor's sheet.</summary>
        public void SetDataService(IPlayerDataService service)
        {
            _dataService = service;
        }

        /// <summary>
        /// Sets a display name independent of the sheet (used to show replicated names
        /// for remote players whose full sheet only exists on the server).
        /// </summary>
        public void SetDisplayName(string displayName)
        {
            _displayNameOverride = displayName;
        }

        /// <summary>Assigns ownership for this actor (used by the networked spawner).</summary>
        public void SetOwner(int ownerId, bool isLocalPlayer)
        {
            _ownerId = ownerId;
            _isLocalPlayer = isLocalPlayer;

            // Ownership is assigned after OnEnable registration, so let the registry
            // re-evaluate which actor is the local player.
            ActorRegistry.NotifyOwnershipChanged(this);
        }

        private void OnEnable()
        {
            ActorRegistry.Register(this);
        }

        private void OnDisable()
        {
            ActorRegistry.Unregister(this);
        }
    }
}

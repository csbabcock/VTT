using GameCore.Actors;
using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Owner-awareness for a networked player. Add to the player prefab alongside
    /// <see cref="PlayerController"/>, <see cref="PlayerActor"/>, a NetworkObject, and a
    /// NetworkTransform.
    ///
    /// On spawn it assigns actor ownership and enables input/control only on the owning
    /// client. Remote instances keep <see cref="PlayerController"/> disabled and are
    /// driven purely by NetworkTransform replication, so every client sees everyone
    /// move while only controlling their own character.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [Header("Local-control wiring")]
        [Tooltip("The PlayerController to enable only for the owning client. Auto-found if left empty.")]
        [SerializeField] private PlayerController _playerController;

        [Tooltip("The PlayerActor on this prefab. Auto-found if left empty.")]
        [SerializeField] private PlayerActor _playerActor;

        [Tooltip("Behaviours (PlayerInput, camera controllers, etc.) that should run only on the owning client.")]
        [SerializeField] private Behaviour[] _ownerOnlyBehaviours;

        [Tooltip("Disable the CharacterController on remote instances so NetworkTransform can position them.")]
        [SerializeField] private bool _disableCharacterControllerOnRemote = true;

        public override void OnNetworkSpawn()
        {
            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();
            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            bool isOwner = IsOwner;

            // Tag the actor with its owning client so the registry and DM tools can
            // distinguish participants. OwnerClientId is the NGO client id.
            if (_playerActor != null)
                _playerActor.SetOwner((int)OwnerClientId, isOwner);

            // Only the owning client reads input and drives the camera/movement.
            if (_playerController != null)
                _playerController.enabled = isOwner;

            if (_ownerOnlyBehaviours != null)
            {
                foreach (var behaviour in _ownerOnlyBehaviours)
                {
                    if (behaviour != null)
                        behaviour.enabled = isOwner;
                }
            }

            if (!isOwner && _disableCharacterControllerOnRemote)
            {
                var characterController = GetComponent<CharacterController>();
                if (characterController != null)
                    characterController.enabled = false;
            }

            gameObject.name = isOwner
                ? $"Player (Local, client {OwnerClientId})"
                : $"Player (Remote, client {OwnerClientId})";
        }
    }
}

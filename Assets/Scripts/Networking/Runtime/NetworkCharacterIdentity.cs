using GameCore.Actors;
using GameCore.PlayerData;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Transmits the owning client's selected character to the server on spawn and
    /// replicates the player's display name to everyone.
    ///
    /// Add to the player prefab alongside <see cref="PlayerActor"/>,
    /// <see cref="NetworkPlayerController"/>, and a NetworkObject. Flow:
    /// <list type="bullet">
    /// <item>The owning client serializes its local <see cref="DnD5eCharacterData"/> and
    /// sends it via a ServerRpc.</item>
    /// <item>The server deserializes it, injects an <see cref="InMemoryPlayerDataService"/>
    /// into this object's <see cref="PlayerActor"/> (so DM tools can read the sheet), and
    /// writes the display name into a server-authoritative NetworkVariable.</item>
    /// <item>Every client applies the replicated name to its copy of the actor so remote
    /// players are labeled correctly even though only the server holds the full sheet.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkCharacterIdentity : NetworkBehaviour
    {
        [SerializeField] private PlayerActor _playerActor;

        private readonly NetworkVariable<FixedString128Bytes> _displayName =
            new NetworkVariable<FixedString128Bytes>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            _displayName.OnValueChanged += HandleDisplayNameChanged;
            ApplyDisplayName(_displayName.Value);

            if (IsOwner)
                SubmitLocalCharacter();
        }

        public override void OnNetworkDespawn()
        {
            _displayName.OnValueChanged -= HandleDisplayNameChanged;
        }

        private void SubmitLocalCharacter()
        {
            var sheet = PlayerDataServiceLocator.Service?.GetCharacterSheet() as DnD5eCharacterData;
            string json = sheet != null ? PlayerDataJsonLoader.ToJson(sheet, false) : string.Empty;
            string fallbackName = SessionRoleLocator.IsDungeonMaster ? "DM" : $"Player {OwnerClientId}";
            SubmitCharacterServerRpc(json, fallbackName);
        }

        [ServerRpc]
        private void SubmitCharacterServerRpc(string characterJson, string fallbackName)
        {
            DnD5eCharacterData data = null;
            if (!string.IsNullOrEmpty(characterJson))
                data = PlayerDataJsonLoader.LoadFromJson(characterJson);

            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            if (data != null && _playerActor != null)
            {
                // Inject the real sheet into the server-side actor so DM tools can read it.
                _playerActor.SetDataService(new InMemoryPlayerDataService(data));
            }

            string resolvedName = data != null && !string.IsNullOrEmpty(data.characterName)
                ? data.characterName
                : fallbackName;

            _displayName.Value = new FixedString128Bytes(Truncate(resolvedName, 60));
        }

        private void HandleDisplayNameChanged(FixedString128Bytes previous, FixedString128Bytes current)
        {
            ApplyDisplayName(current);
        }

        private void ApplyDisplayName(FixedString128Bytes value)
        {
            if (_playerActor == null)
                _playerActor = GetComponent<PlayerActor>();

            string name = value.ToString();
            if (!string.IsNullOrEmpty(name))
                _playerActor?.SetDisplayName(name);
        }

        private static string Truncate(string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
                return value ?? string.Empty;
            return value.Substring(0, maxChars);
        }
    }
}

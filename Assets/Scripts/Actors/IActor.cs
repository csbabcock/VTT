using GameCore.PlayerData;
using UnityEngine;

namespace GameCore.Actors
{
    /// <summary>
    /// A participant present in a gameplay scene (player character, NPC, or monster).
    /// An actor owns a <see cref="ICharacterSheet"/>, has a world position, and is
    /// associated with an owner (the client/seat that controls it).
    ///
    /// This is the seam that lets gameplay systems (encounter mode, in-game UI, the
    /// future DM tools) operate on "a participant" rather than the single local
    /// player. It is deliberately transport-agnostic: a networked implementation can
    /// add a NetworkBehaviour alongside it without changing consumers.
    /// </summary>
    public interface IActor
    {
        /// <summary>
        /// Identifier of the owner/seat controlling this actor.
        /// 0 is treated as the local/host owner until networking assigns real ids.
        /// </summary>
        int OwnerId { get; }

        /// <summary>Whether this actor is controlled by the local machine's player.</summary>
        bool IsLocalPlayer { get; }

        /// <summary>Display name for UI (falls back to the GameObject name).</summary>
        string DisplayName { get; }

        /// <summary>The actor's character sheet, or null if not yet assigned.</summary>
        ICharacterSheet Sheet { get; }

        /// <summary>The data service backing this actor's sheet (source of change events).</summary>
        IPlayerDataService DataService { get; }

        /// <summary>The actor's transform (world position / movement target).</summary>
        Transform Transform { get; }
    }
}

using System;
using System.Collections.Generic;

namespace GameCore.Actors
{
    /// <summary>
    /// Scene-wide registry of <see cref="IActor"/> instances.
    ///
    /// Mirrors the existing <c>PlayerDataServiceLocator</c> pattern (a simple static
    /// access point rather than a DI framework) so gameplay systems can find "the
    /// local player's actor" or enumerate "all actors" without using
    /// <c>FindObjectOfType</c>. When networking lands, the server becomes the source
    /// of truth and registers/unregisters actors as clients connect; consumers stay
    /// unchanged because they already depend on this registry.
    /// </summary>
    public static class ActorRegistry
    {
        private static readonly List<IActor> _actors = new List<IActor>();

        /// <summary>All currently registered actors.</summary>
        public static IReadOnlyList<IActor> Actors => _actors;

        /// <summary>The actor controlled by the local machine, or null if none registered.</summary>
        public static IActor LocalActor { get; private set; }

        /// <summary>Raised after an actor is registered.</summary>
        public static event Action<IActor> ActorRegistered;

        /// <summary>Raised after an actor is unregistered.</summary>
        public static event Action<IActor> ActorUnregistered;

        /// <summary>Raised when an actor's display name, ownership, or sheet data changes.</summary>
        public static event Action<IActor> ActorUpdated;

        public static void Register(IActor actor)
        {
            if (actor == null || _actors.Contains(actor))
                return;

            _actors.Add(actor);

            if (actor.IsLocalPlayer && LocalActor == null)
                LocalActor = actor;

            ActorRegistered?.Invoke(actor);
        }

        public static void Unregister(IActor actor)
        {
            if (actor == null || !_actors.Remove(actor))
                return;

            if (ReferenceEquals(LocalActor, actor))
                LocalActor = FindLocalActor();

            ActorUnregistered?.Invoke(actor);
        }

        /// <summary>
        /// Recomputes <see cref="LocalActor"/> after an already-registered actor's
        /// ownership changes. Networked actors register (OnEnable) before the spawner
        /// assigns real ownership (OnNetworkSpawn), so the spawner calls this once
        /// ownership is known to ensure the correct actor is treated as local.
        /// </summary>
        public static void NotifyOwnershipChanged(IActor actor)
        {
            if (actor == null || !_actors.Contains(actor))
                return;

            if (actor.IsLocalPlayer)
                LocalActor = actor;
            else if (ReferenceEquals(LocalActor, actor))
                LocalActor = FindLocalActor();
        }

        /// <summary>Notifies listeners that an actor's presentation data changed.</summary>
        public static void NotifyActorUpdated(IActor actor)
        {
            if (actor == null || !_actors.Contains(actor))
                return;

            ActorUpdated?.Invoke(actor);
        }

        /// <summary>Returns the first actor owned by <paramref name="ownerId"/>, or null.</summary>
        public static IActor GetByOwner(int ownerId)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i] != null && _actors[i].OwnerId == ownerId)
                    return _actors[i];
            }
            return null;
        }

        /// <summary>Clears all registrations. Intended for tests and scene teardown.</summary>
        public static void Clear()
        {
            _actors.Clear();
            LocalActor = null;
        }

        private static IActor FindLocalActor()
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (_actors[i] != null && _actors[i].IsLocalPlayer)
                    return _actors[i];
            }
            return null;
        }
    }
}

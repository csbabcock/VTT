using GameCore.Actors;

namespace GameCore.PlayerData
{
    /// <summary>
    /// Decides whether the local client may mutate another actor's combat state.
    /// Dungeon Masters may edit any actor; players may edit only their own actor.
    /// </summary>
    public static class CharacterCombatMutationPolicy
    {
        public static bool CanMutate(bool isDungeonMaster, bool isLocalOwner)
        {
            return isDungeonMaster || isLocalOwner;
        }

        public static bool IsLocalOwner(IActor actor)
        {
            if (actor == null)
                return false;

            var local = ActorRegistry.LocalActor;
            return local != null && local.OwnerId == actor.OwnerId;
        }
    }
}

using GameCore.Actors;
using GameCore.PlayerData;

namespace GameCore.Combat.Services
{
    /// <summary>Applies combat damage to character sheets with correct offline/network authority rules.</summary>
    public static class CombatSheetMutator
    {
        public static bool TryApplyDamage(IActor target, IActor attacker, int amount)
        {
            if (amount <= 0 || target == null || attacker == null || ReferenceEquals(target, attacker))
                return false;

            if (!attacker.IsLocalPlayer && !Networking.SessionRoleLocator.IsDungeonMaster)
                return false;

            if (target.Transform != null)
            {
                var receiver = FindCombatDamageReceiver(target.Transform);
                if (receiver != null)
                {
                    receiver.RequestDamageFromAttacker(amount, attacker.OwnerId);
                    return true;
                }
            }

            return TryApplyDamageOffline(target, amount);
        }

        private static ICombatDamageReceiver FindCombatDamageReceiver(UnityEngine.Transform transform)
        {
            var components = transform.GetComponents<UnityEngine.Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is ICombatDamageReceiver receiver)
                    return receiver;
            }

            return null;
        }

        private static bool TryApplyDamageOffline(IActor target, int amount)
        {
            var mutable = CharacterSheetAuthorityHelper.TryGetMutableAuthority(target);
            if (mutable != null)
            {
                mutable.RequestAdjustCurrentHitPoints(-amount);
                return true;
            }

            var data = target.DataService?.GetCharacterSheet() as DnD5eCharacterData;
            if (data == null)
                return false;

            int maxHp = CharacterHitPoints.GetDisplayMaxHp(data);
            data.currentHitPoints = CharacterHitPoints.ClampCurrent(data.currentHitPoints - amount, maxHp);
            NotifyServiceChanged(target.DataService);
            ActorRegistry.NotifyActorUpdated(target);
            return true;
        }

        private static void NotifyServiceChanged(IPlayerDataService service)
        {
            if (service == null)
                return;

            switch (service)
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
    }
}

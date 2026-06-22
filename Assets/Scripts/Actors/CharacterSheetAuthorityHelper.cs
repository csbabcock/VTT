using GameCore.PlayerData;
using UnityEngine;

namespace GameCore.Actors
{
    /// <summary>Resolves <see cref="ICharacterSheetAuthority"/> for an actor in-scene.</summary>
    public static class CharacterSheetAuthorityHelper
    {
        public static ICharacterSheetAuthority GetAuthority(IActor actor)
        {
            if (actor == null)
                return null;

            var transform = actor.Transform;
            if (transform != null)
            {
                var components = transform.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] is ICharacterSheetAuthority authority)
                        return authority;
                }
            }

            var data = actor.DataService?.GetCharacterSheet() as DnD5eCharacterData;
            if (data != null && actor.DataService != null)
                return new DirectCharacterSheetAuthority(data, actor.DataService);

            return null;
        }

        public static CharacterCombatState GetCombatState(IActor actor)
        {
            var authority = GetAuthority(actor);
            if (authority != null)
                return authority.CombatState;

            var data = actor?.DataService?.GetCharacterSheet() as DnD5eCharacterData;
            return data != null ? CharacterCombatState.FromSheet(data) : default;
        }

        public static int GetMaxHitPoints(IActor actor)
        {
            var authority = GetAuthority(actor);
            if (authority != null)
                return authority.MaxHitPoints;

            var data = actor?.DataService?.GetCharacterSheet() as DnD5eCharacterData;
            return CharacterHitPoints.GetDisplayMaxHp(data);
        }
    }
}

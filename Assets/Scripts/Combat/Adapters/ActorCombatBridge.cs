using GameCore.Actors;
using GameCore.PlayerData;

namespace GameCore.Combat.Adapters
{
    public static class ActorCombatBridge
    {
        public static int GetArmorClass(IActor actor)
        {
            if (actor?.Sheet is DnD5eCharacterData sheetData)
                return sheetData.armorClass;

            if (actor?.DataService?.GetCharacterSheet() is DnD5eCharacterData data)
                return data.armorClass;

            return 10;
        }

        public static IAttackParticipant CreateAttackParticipant(IActor actor)
        {
            if (actor?.Sheet == null)
                return null;

            string name = CharacterSheetAuthorityHelper.GetDisplayName(actor);
            return new SheetAttackParticipant(name, actor.Sheet);
        }

        public static IDamageable TryCreateDamageable(IActor actor, IActor attacker)
        {
            var readAuthority = CharacterSheetAuthorityHelper.GetAuthority(actor);
            if (readAuthority == null)
                return null;

            string displayName = CharacterSheetAuthorityHelper.GetDisplayName(actor);
            int armorClass = GetArmorClass(actor);

            return new CharacterDamageableAdapter(
                readAuthority,
                actor,
                attacker,
                () => armorClass,
                displayName);
        }
    }
}

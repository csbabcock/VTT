using System;
using GameCore.PlayerData;

namespace GameCore.Combat.Adapters
{
    /// <summary>Wraps a character sheet as an attack participant.</summary>
    public sealed class SheetAttackParticipant : IAttackParticipant
    {
        public SheetAttackParticipant(string displayName, ICharacterSheet sheet)
        {
            DisplayName = displayName ?? string.Empty;
            Sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
        }

        public string DisplayName { get; }
        public ICharacterSheet Sheet { get; }
    }
}

using GameCore.PlayerData;

namespace GameCore.Combat
{
    /// <summary>An entity that can perform attacks using a character sheet.</summary>
    public interface IAttackParticipant
    {
        string DisplayName { get; }
        ICharacterSheet Sheet { get; }
    }
}

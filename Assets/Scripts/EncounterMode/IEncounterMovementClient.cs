using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Transport-agnostic encounter movement requests from the local player.
    /// Implemented by <c>NetworkEncounterParticipant</c> in the networking assembly.
    /// </summary>
    public interface IEncounterMovementClient
    {
        int RemainingMovementFeet { get; }
        bool IsDashActive { get; }

        void RequestBeginMovePhase();
        void RequestMoveTo(GridCell targetCell, int elevation);
        void RequestDash();
    }
}

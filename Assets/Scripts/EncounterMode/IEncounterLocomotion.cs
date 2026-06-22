using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Applies an approved encounter grid move to a player's avatar. Implemented by the
    /// player controller and driven by the encounter coordinator so that move validation
    /// (local or server) and avatar locomotion flow through a single pipeline.
    /// </summary>
    public interface IEncounterLocomotion
    {
        /// <summary>Moves the avatar toward the given cell at the given elevation level.</summary>
        void ApplyMove(GridCell cell, int elevation);
    }
}

using GameCore.EncounterMode.Grid;

namespace GameCore.EncounterMode
{
    /// <summary>
    /// Contract for the encounter mode coordinator. Lets UI and player code depend on the
    /// encounter abstraction instead of the concrete MonoBehaviour.
    /// </summary>
    public interface IEncounterModeManager
    {
        bool IsEncounterModeActive { get; }
        bool IsMovementModeActive { get; }
        bool IsLocalTurnActive { get; }
        bool UsesNetworkEncounter { get; }

        void ToggleEncounterMode();
        void EnableGridSelection();
        void DisableMovementMode();
        void SetDashActive(bool isActive);
        bool IsCellReachable(GridCell cell);
        void RefreshMovementDisplay();
    }
}

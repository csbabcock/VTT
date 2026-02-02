namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Abstration for rolling ability scores (4d6 drop lowest).
    /// Enables Dependency Inversion: Presenter depends on this, not a concrete roller.
    /// </summary>
    public interface IAbilityScoreRoller
    {
        /// <summary>
        /// Rolls 4d6, drops the lowest die, returns the four dice, the sum of the three kept, and the index (0-3) of the dropped die.
        /// </summary>
        (int[] dice, int sum, int droppedIndex) Roll4d6DropLowest();
    }
}

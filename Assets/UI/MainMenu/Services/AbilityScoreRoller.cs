using UnityEngine;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Default implementation of 4d6 drop lowest for D&D 5e ability score rolling.
    /// Single responsibility: dice rolling only.
    /// </summary>
    public class AbilityScoreRoller : IAbilityScoreRoller
    {
        public (int[] dice, int sum, int droppedIndex) Roll4d6DropLowest()
        {
            int[] rolls = new int[4];
            for (int i = 0; i < 4; i++)
                rolls[i] = Random.Range(1, 7); // 1-6

            int lowest = rolls[0];
            int sum = rolls[0];
            for (int i = 1; i < 4; i++)
            {
                if (rolls[i] < lowest) lowest = rolls[i];
                sum += rolls[i];
            }

            int droppedIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                if (rolls[i] == lowest) { droppedIndex = i; break; }
            }
            return (rolls, sum - lowest, droppedIndex);
        }
    }
}

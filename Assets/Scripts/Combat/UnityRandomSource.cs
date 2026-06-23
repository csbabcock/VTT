using UnityEngine;

namespace GameCore.Combat
{
    /// <summary>Production random source backed by Unity's RNG.</summary>
    public sealed class UnityRandomSource : IRandomSource
    {
        public int RollDie(int sides) => Random.Range(1, sides + 1);
    }
}

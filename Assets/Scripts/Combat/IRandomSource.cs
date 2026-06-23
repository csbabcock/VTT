namespace GameCore.Combat
{
    /// <summary>Abstraction over random die rolls for testable combat resolution.</summary>
    public interface IRandomSource
    {
        /// <summary>Returns a value in [1, sides] inclusive.</summary>
        int RollDie(int sides);
    }
}

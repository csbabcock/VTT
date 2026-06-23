using System.Collections.Generic;

namespace GameCore.Combat
{
    /// <summary>Deterministic roller that returns a preset sequence of die results.</summary>
    public sealed class QueueRandomSource : IRandomSource
    {
        private readonly Queue<int> _values;

        public QueueRandomSource(params int[] values) => _values = new Queue<int>(values);

        public int RollDie(int sides)
        {
            if (_values.Count == 0)
                return 1;

            return _values.Dequeue();
        }
    }
}

using System.Collections.Generic;

namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Supplies an initiative die roll (a d20 in D&D 5e). Abstracted so turn-order logic
    /// can be unit-tested deterministically.
    /// </summary>
    public interface IInitiativeRoller
    {
        /// <summary>Returns a single initiative die result (1-20 inclusive).</summary>
        int Roll();
    }

    /// <summary>Default roller backed by <see cref="UnityEngine.Random"/>.</summary>
    public sealed class SystemInitiativeRoller : IInitiativeRoller
    {
        public int Roll() => UnityEngine.Random.Range(1, 21);
    }

    /// <summary>
    /// Pure, transport-agnostic turn-order state: rolls initiative, sorts participants,
    /// and advances the active turn with wrap-around. Extracted from the networking layer
    /// so the rules are unit-testable without Netcode or a scene.
    /// </summary>
    public sealed class EncounterTurnOrderService
    {
        public const int NoTurnOwner = -1;

        private readonly List<int> _order = new List<int>();
        private int _index;

        public IReadOnlyList<int> Order => _order;
        public bool HasTurns => _order.Count > 0;
        public int CurrentIndex => _index;
        public int CurrentOwnerId => HasTurns ? _order[_index] : NoTurnOwner;

        /// <summary>
        /// Rolls initiative for each participant (d20 + modifier), sorts highest first,
        /// and resets the active turn to the top of the order.
        /// </summary>
        public void RollInitiative(
            IEnumerable<(int ownerId, int initiativeModifier)> participants,
            IInitiativeRoller roller)
        {
            _order.Clear();
            _index = 0;

            if (participants == null || roller == null)
                return;

            var scored = new List<(int ownerId, int total)>();
            foreach (var participant in participants)
                scored.Add((participant.ownerId, roller.Roll() + participant.initiativeModifier));

            scored.Sort((a, b) => b.total.CompareTo(a.total));

            foreach (var entry in scored)
                _order.Add(entry.ownerId);
        }

        /// <summary>Advances to the next owner (wrapping). Returns the new current owner id.</summary>
        public int Advance()
        {
            if (_order.Count == 0)
                return NoTurnOwner;

            _index = (_index + 1) % _order.Count;
            return _order[_index];
        }

        /// <summary>Appends a late-joining owner to the end of the order when missing.</summary>
        public bool TryAddOwner(int ownerId)
        {
            if (ownerId == NoTurnOwner || _order.Contains(ownerId))
                return false;

            _order.Add(ownerId);
            return true;
        }

        /// <summary>Clears the turn order.</summary>
        public void Clear()
        {
            _order.Clear();
            _index = 0;
        }
    }
}

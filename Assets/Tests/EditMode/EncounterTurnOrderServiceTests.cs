using System.Collections.Generic;
using GameCore.EncounterMode.Services;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class EncounterTurnOrderServiceTests
    {
        /// <summary>Deterministic roller that returns a preset sequence of die results.</summary>
        private sealed class QueueRoller : IInitiativeRoller
        {
            private readonly Queue<int> _values;
            public QueueRoller(params int[] values) => _values = new Queue<int>(values);
            public int Roll() => _values.Count > 0 ? _values.Dequeue() : 0;
        }

        private static List<(int, int)> Participants(params (int ownerId, int mod)[] entries)
        {
            return new List<(int, int)>(entries);
        }

        [Test]
        public void RollInitiative_OrdersByTotalDescending()
        {
            var service = new EncounterTurnOrderService();

            service.RollInitiative(
                Participants((10, 0), (20, 0), (30, 0)),
                new QueueRoller(5, 15, 10));

            CollectionAssert.AreEqual(new[] { 20, 30, 10 }, service.Order);
            Assert.AreEqual(20, service.CurrentOwnerId);
            Assert.IsTrue(service.HasTurns);
        }

        [Test]
        public void RollInitiative_AddsInitiativeModifierToRoll()
        {
            var service = new EncounterTurnOrderService();

            service.RollInitiative(
                Participants((10, 3), (20, 0)),
                new QueueRoller(10, 10));

            CollectionAssert.AreEqual(new[] { 10, 20 }, service.Order);
        }

        [Test]
        public void Advance_WrapsAroundOrder()
        {
            var service = new EncounterTurnOrderService();
            service.RollInitiative(Participants((10, 0), (20, 0), (30, 0)), new QueueRoller(5, 15, 10));

            Assert.AreEqual(30, service.Advance());
            Assert.AreEqual(10, service.Advance());
            Assert.AreEqual(20, service.Advance());
        }

        [Test]
        public void Advance_SingleParticipant_ReturnsSameOwner()
        {
            var service = new EncounterTurnOrderService();
            service.RollInitiative(Participants((42, 0)), new QueueRoller(10));

            Assert.AreEqual(42, service.Advance());
            Assert.AreEqual(42, service.Advance());
        }

        [Test]
        public void Advance_EmptyOrder_ReturnsNoTurnOwner()
        {
            var service = new EncounterTurnOrderService();

            Assert.AreEqual(EncounterTurnOrderService.NoTurnOwner, service.Advance());
        }

        [Test]
        public void Clear_ResetsState()
        {
            var service = new EncounterTurnOrderService();
            service.RollInitiative(Participants((1, 0), (2, 0)), new QueueRoller(1, 2));

            service.Clear();

            Assert.IsFalse(service.HasTurns);
            Assert.AreEqual(EncounterTurnOrderService.NoTurnOwner, service.CurrentOwnerId);
        }

        [Test]
        public void RollInitiative_NullParticipants_LeavesEmpty()
        {
            var service = new EncounterTurnOrderService();

            service.RollInitiative(null, new QueueRoller(1));

            Assert.IsFalse(service.HasTurns);
        }

        [Test]
        public void TryAddOwner_AppendsMissingOwner()
        {
            var service = new EncounterTurnOrderService();
            service.RollInitiative(Participants((10, 0), (20, 0)), new QueueRoller(5, 15));

            Assert.IsTrue(service.TryAddOwner(30));
            CollectionAssert.AreEqual(new[] { 20, 10, 30 }, service.Order);
        }

        [Test]
        public void TryAddOwner_ReturnsFalseForDuplicateOrInvalid()
        {
            var service = new EncounterTurnOrderService();
            service.RollInitiative(Participants((10, 0)), new QueueRoller(5));

            Assert.IsFalse(service.TryAddOwner(10));
            Assert.IsFalse(service.TryAddOwner(EncounterTurnOrderService.NoTurnOwner));
            CollectionAssert.AreEqual(new[] { 10 }, service.Order);
        }
    }
}

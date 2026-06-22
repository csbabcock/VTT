using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Locks in the current behavior of <see cref="MovementTracker"/> before refactoring.
    /// </summary>
    public class MovementTrackerTests
    {
        private static GridCell Cell(int x, int z) => new GridCell(x, z, Vector3.zero);

        [Test]
        public void NewTracker_StartsWithFullBaseMovement()
        {
            var tracker = new MovementTracker(30);

            Assert.AreEqual(30, tracker.RemainingMovementFeet);
            Assert.IsFalse(tracker.IsDashActive);
            Assert.AreEqual(30, tracker.EffectiveMaxSpeed);
            Assert.IsFalse(tracker.IsMovementExhausted);
        }

        [Test]
        public void CalculateDistanceFeet_UsesChebyshevDistanceInFiveFootCells()
        {
            var tracker = new MovementTracker(30);

            Assert.AreEqual(0, tracker.CalculateDistanceFeet(Cell(0, 0), Cell(0, 0)));
            Assert.AreEqual(5, tracker.CalculateDistanceFeet(Cell(0, 0), Cell(1, 1)));
            Assert.AreEqual(10, tracker.CalculateDistanceFeet(Cell(0, 0), Cell(2, 1)));
        }

        [Test]
        public void CalculateDistanceFeet_WithNullCell_ReturnsZero()
        {
            var tracker = new MovementTracker(30);

            Assert.AreEqual(0, tracker.CalculateDistanceFeet(null, Cell(1, 1)));
            Assert.AreEqual(0, tracker.CalculateDistanceFeet(Cell(1, 1), null));
        }

        [Test]
        public void TryDeductMovement_WithinBudget_DeductsAndStoresCell()
        {
            var tracker = new MovementTracker(30);
            var target = Cell(2, 0);

            bool ok = tracker.TryDeductMovement(10, target);

            Assert.IsTrue(ok);
            Assert.AreEqual(20, tracker.RemainingMovementFeet);
            Assert.AreSame(target, tracker.LastSelectedCell);
        }

        [Test]
        public void TryDeductMovement_OverBudget_RejectsAndLeavesStateUnchanged()
        {
            var tracker = new MovementTracker(30);
            tracker.TryDeductMovement(20, Cell(4, 0));

            bool ok = tracker.TryDeductMovement(15, Cell(7, 0));

            Assert.IsFalse(ok);
            Assert.AreEqual(10, tracker.RemainingMovementFeet);
        }

        [Test]
        public void TryDeductMovement_ExactRemaining_LeavesZeroAndExhausted()
        {
            var tracker = new MovementTracker(30);

            bool ok = tracker.TryDeductMovement(30, Cell(6, 0));

            Assert.IsTrue(ok);
            Assert.AreEqual(0, tracker.RemainingMovementFeet);
            Assert.IsTrue(tracker.IsMovementExhausted);
        }

        [Test]
        public void SetRemainingMovementFeet_ClampsNegativeToZero()
        {
            var tracker = new MovementTracker(30);

            tracker.SetRemainingMovementFeet(-5);

            Assert.AreEqual(0, tracker.RemainingMovementFeet);
        }

        [Test]
        public void ResetMovement_RestoresEffectiveMaxSpeed()
        {
            var tracker = new MovementTracker(30);
            tracker.TryDeductMovement(25, Cell(5, 0));

            tracker.ResetMovement();

            Assert.AreEqual(30, tracker.RemainingMovementFeet);
        }

        [Test]
        public void SetDashActive_WhileInMovementMode_DoublesEffectiveMaxAndRemaining()
        {
            var tracker = new MovementTracker(30);

            tracker.SetDashActive(true, wasInMovementMode: true);

            Assert.IsTrue(tracker.IsDashActive);
            Assert.AreEqual(60, tracker.EffectiveMaxSpeed);
            Assert.AreEqual(60, tracker.RemainingMovementFeet);
        }

        [Test]
        public void SetDashActive_WhenExhausted_AddsBackBaseMovement()
        {
            var tracker = new MovementTracker(30);
            tracker.TryDeductMovement(30, Cell(6, 0));

            tracker.SetDashActive(true, wasInMovementMode: true);

            Assert.IsTrue(tracker.IsDashActive);
            Assert.AreEqual(30, tracker.RemainingMovementFeet);
        }

        [Test]
        public void SetDashActive_PreservesUsedMovementWhenRecalculating()
        {
            var tracker = new MovementTracker(30);
            tracker.SetDashActive(true, wasInMovementMode: true); // remaining 60
            tracker.TryDeductMovement(20, Cell(4, 0));            // remaining 40, used 20

            tracker.SetDashActive(false, wasInMovementMode: true);

            Assert.IsFalse(tracker.IsDashActive);
            Assert.AreEqual(10, tracker.RemainingMovementFeet); // 30 base - 20 used
        }

        [Test]
        public void ResetDash_ClearsDashFlag()
        {
            var tracker = new MovementTracker(30);
            tracker.SetDashActive(true, wasInMovementMode: true);

            tracker.ResetDash();

            Assert.IsFalse(tracker.IsDashActive);
        }

        [Test]
        public void SetDashActive_FromFullMovement_GivesDoubleBudget()
        {
            var tracker = new MovementTracker(30);

            tracker.SetDashActive(true, wasInMovementMode: true);

            // Dashing before moving: full base (30) plus the dash bonus (30) = 60.
            Assert.AreEqual(60, tracker.RemainingMovementFeet);
            Assert.AreEqual(60, tracker.EffectiveMaxSpeed);
        }

        [Test]
        public void SetDashActive_FromExhausted_GivesExactlyOneBaseMove_NotDouble()
        {
            var tracker = new MovementTracker(30);
            tracker.TryDeductMovement(30, Cell(6, 0)); // remaining 0

            tracker.SetDashActive(true, wasInMovementMode: true);

            // Already spent the base move, so Dash grants one additional base move (30), not 60.
            Assert.AreEqual(30, tracker.RemainingMovementFeet);
            Assert.AreEqual(60, tracker.EffectiveMaxSpeed);
        }

        [Test]
        public void ApplyAuthoritativeState_TakesServerValuesVerbatim()
        {
            var tracker = new MovementTracker(30);

            tracker.ApplyAuthoritativeState(remainingFeet: 15, dashActive: true);

            Assert.AreEqual(15, tracker.RemainingMovementFeet);
            Assert.IsTrue(tracker.IsDashActive);
            Assert.AreEqual(60, tracker.EffectiveMaxSpeed);
        }

        [Test]
        public void ApplyAuthoritativeState_ClampsNegativeRemainingToZero()
        {
            var tracker = new MovementTracker(30);

            tracker.ApplyAuthoritativeState(remainingFeet: -10, dashActive: false);

            Assert.AreEqual(0, tracker.RemainingMovementFeet);
        }

        // Regression: the networked Dash result must not be re-doubled on the client.
        // Server-side Dash from an exhausted budget yields remaining=30, dash=true; applying that
        // replicated state verbatim must keep 30 (previously SetDashActive recomputed it to 60).
        [Test]
        public void ApplyAuthoritativeState_DashFromExhausted_DoesNotDoubleApply()
        {
            var tracker = new MovementTracker(30);
            tracker.TryDeductMovement(30, Cell(6, 0)); // local remaining 0

            // Server computed: 0 + base(30) = 30 remaining, dash on. Replicate verbatim.
            tracker.ApplyAuthoritativeState(remainingFeet: 30, dashActive: true);

            Assert.AreEqual(30, tracker.RemainingMovementFeet);
            Assert.IsTrue(tracker.IsDashActive);
        }

        // Regression: a replicated approved move while Dash is active must not inflate the budget.
        [Test]
        public void ApplyAuthoritativeState_ApprovedMoveWhileDashing_KeepsServerRemaining()
        {
            var tracker = new MovementTracker(30);
            tracker.ApplyAuthoritativeState(remainingFeet: 60, dashActive: true); // begin dash, full

            // Server approves a 25ft move while dashing -> 35 remaining.
            tracker.ApplyAuthoritativeState(remainingFeet: 35, dashActive: true);

            Assert.AreEqual(35, tracker.RemainingMovementFeet);
            Assert.IsTrue(tracker.IsDashActive);
        }
    }
}

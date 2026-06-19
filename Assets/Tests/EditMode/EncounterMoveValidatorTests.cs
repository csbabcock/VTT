using GameCore.EncounterMode.Grid;
using GameCore.EncounterMode.Services;
using NUnit.Framework;

namespace GameCore.Tests.EditMode
{
    public class EncounterMoveValidatorTests
    {
        [Test]
        public void CalculateDistanceFeet_UsesChebyshevDistanceInFiveFootCells()
        {
            Assert.AreEqual(0, EncounterMoveValidator.CalculateDistanceFeet(0, 0, 0, 0));
            Assert.AreEqual(5, EncounterMoveValidator.CalculateDistanceFeet(0, 0, 1, 0));
            Assert.AreEqual(5, EncounterMoveValidator.CalculateDistanceFeet(0, 0, 1, 1));
            Assert.AreEqual(10, EncounterMoveValidator.CalculateDistanceFeet(0, 0, 2, 1));
        }

        [Test]
        public void Validate_RejectsMoveWhenNoMovementRemaining()
        {
            var result = EncounterMoveValidator.Validate(
                new EncounterMoveValidator.MoveRequest(0, 0, 1, 0, 0));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(0, result.RemainingFeetAfterMove);
        }

        [Test]
        public void Validate_RejectsMoveBeyondRemainingFeet()
        {
            var result = EncounterMoveValidator.Validate(
                new EncounterMoveValidator.MoveRequest(0, 0, 3, 0, 10));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(15, result.DistanceFeet);
            Assert.AreEqual(10, result.RemainingFeetAfterMove);
        }

        [Test]
        public void Validate_AcceptsMoveWithinBudget()
        {
            var result = EncounterMoveValidator.Validate(
                new EncounterMoveValidator.MoveRequest(0, 0, 2, 0, 15));

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(10, result.DistanceFeet);
            Assert.AreEqual(5, result.RemainingFeetAfterMove);
        }

        [Test]
        public void Validate_WithGridCells_MatchesCoordinateValidation()
        {
            var from = new GridCell(0, 0, UnityEngine.Vector3.zero);
            var to = new GridCell(1, 1, UnityEngine.Vector3.one);

            var result = EncounterMoveValidator.Validate(from, to, 30);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(5, result.DistanceFeet);
            Assert.AreEqual(25, result.RemainingFeetAfterMove);
        }

        [Test]
        public void Validate_WithNullCells_IsInvalid()
        {
            var cell = new GridCell(0, 0, UnityEngine.Vector3.zero);

            Assert.IsFalse(EncounterMoveValidator.Validate(null, cell, 30).IsValid);
            Assert.IsFalse(EncounterMoveValidator.Validate(cell, null, 30).IsValid);
        }
    }
}

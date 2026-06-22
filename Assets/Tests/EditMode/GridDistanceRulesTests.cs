using GameCore.EncounterMode.Grid;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class GridDistanceRulesTests
    {
        [Test]
        public void CellsBetween_UsesChebyshevDistance()
        {
            Assert.AreEqual(0, GridDistanceRules.CellsBetween(0, 0, 0, 0));
            Assert.AreEqual(1, GridDistanceRules.CellsBetween(0, 0, 1, 1));
            Assert.AreEqual(2, GridDistanceRules.CellsBetween(0, 0, 2, 1));
            Assert.AreEqual(3, GridDistanceRules.CellsBetween(1, 1, 4, 3));
        }

        [Test]
        public void DistanceFeet_FromCoordinates_IsCellsTimesFive()
        {
            Assert.AreEqual(0, GridDistanceRules.DistanceFeet(0, 0, 0, 0));
            Assert.AreEqual(5, GridDistanceRules.DistanceFeet(0, 0, 1, 1));
            Assert.AreEqual(10, GridDistanceRules.DistanceFeet(0, 0, 2, 1));
        }

        [Test]
        public void DistanceFeet_FromCells_MatchesCoordinates()
        {
            var from = new GridCell(0, 0, Vector3.zero);
            var to = new GridCell(2, 1, Vector3.zero);

            Assert.AreEqual(10, GridDistanceRules.DistanceFeet(from, to));
        }

        [Test]
        public void DistanceFeet_WithNullCell_ReturnsZero()
        {
            var cell = new GridCell(0, 0, Vector3.zero);

            Assert.AreEqual(0, GridDistanceRules.DistanceFeet(null, cell));
            Assert.AreEqual(0, GridDistanceRules.DistanceFeet(cell, null));
        }

        [Test]
        public void FeetToCells_FloorsToWholeCells()
        {
            Assert.AreEqual(0, GridDistanceRules.FeetToCells(4));
            Assert.AreEqual(1, GridDistanceRules.FeetToCells(5));
            Assert.AreEqual(2, GridDistanceRules.FeetToCells(10));
            Assert.AreEqual(2, GridDistanceRules.FeetToCells(12));
        }
    }
}

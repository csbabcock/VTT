using System.Collections;
using GameCore.Actors;
using GameCore.Combat.Targeting;
using GameCore.EncounterMode;
using GameCore.EncounterMode.Grid;
using GameCore.PlayerData;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class MeleeMovementRegressionTests
    {
        private GameObject _attacker;
        private GameObject _target;

        [SetUp]
        public void SetUp()
        {
            _attacker = new GameObject("attacker");
            _target = new GameObject("target");
            _target.transform.position = Vector3.right * 10f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_attacker);
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void GridMove_HalfCellAway_StartsMovementWithoutTeleporting()
        {
            var handler = CreateMovementHandler();
            handler.SetTargetCell(new GridCell(0, 0, Vector3.right * 0.4f), 0);

            Assert.IsTrue(handler.IsMoving);
            Assert.AreEqual(Vector3.zero, _attacker.transform.position);
        }

        [Test]
        public void GridMove_ShortFinalStep_CompletesWithoutMinimumTravelRequirement()
        {
            var handler = CreateMovementHandler();
            var destination = Vector3.right * 0.06f;
            handler.SetTargetWorldPosition(new GridCell(0, 0, destination), 0, destination);
            // Consume the first movement tick, then simulate a short controller step.
            handler.ProcessMovement(true);
            _attacker.transform.position = Vector3.right * 0.03f;
            handler.ProcessMovement(true);

            Assert.IsFalse(handler.IsMoving);
            Assert.AreEqual(destination, _attacker.transform.position);
        }

        [Test]
        public void Approach_WithoutLocomotion_FailsWithoutTeleporting()
        {
            var service = CreateApproachService();

            Assert.IsFalse(service.TryApproach(new Actor(_attacker.transform), new Actor(_target.transform)));
            Assert.AreEqual(Vector3.zero, _attacker.transform.position);
        }

        [Test]
        public void Finalize_OutOfRangeWithoutLocomotion_DoesNotTeleport()
        {
            var routine = CreateApproachService().FinalizeMeleeRange(
                new Actor(_attacker.transform), new Actor(_target.transform));
            Drain(routine);

            Assert.AreEqual(Vector3.zero, _attacker.transform.position);
        }

        [Test]
        public void Finalize_AlreadyInRange_DoesNotRepositionAttacker()
        {
            _target.transform.position = Vector3.right;
            Drain(CreateApproachService().FinalizeMeleeRange(
                new Actor(_attacker.transform), new Actor(_target.transform)));

            Assert.AreEqual(Vector3.zero, _attacker.transform.position);
        }

        private EncounterMovementHandler CreateMovementHandler() =>
            new EncounterMovementHandler(_attacker.AddComponent<CharacterController>(),
                _attacker.transform, new TestGrid(), 5f, 0.12f);

        private static CombatMeleeApproachService CreateApproachService() =>
            new CombatMeleeApproachService(() => null, () => null, () => 1.524f);

        private static void Drain(IEnumerator routine)
        {
            while (routine.MoveNext())
                if (routine.Current is IEnumerator nested)
                    Drain(nested);
        }

        private sealed class Actor : IActor
        {
            public Actor(Transform transform) => Transform = transform;
            public int OwnerId => 0;
            public bool IsLocalPlayer => true;
            public string DisplayName => "Actor";
            public ICharacterSheet Sheet => null;
            public IPlayerDataService DataService => null;
            public Transform Transform { get; }
        }

        private sealed class TestGrid : IGridGenerator
        {
            public GridCell[,] Grid => null;
            public float CellSize => 2f;
            public Vector3 GridOrigin => Vector3.zero;
            public void GenerateGrid(Vector3 origin, int width, int height, float cellSize, LayerMask groundLayer) { }
            public GridCell GetCell(int x, int z) => null;
            public GridCell GetCellAtWorldPosition(Vector3 position) => null;
        }
    }
}

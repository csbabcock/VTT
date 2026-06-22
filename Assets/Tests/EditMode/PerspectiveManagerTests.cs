using GameCore;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    /// <summary>
    /// Locks in <see cref="PerspectiveManager"/> behavior: lazy CinemachineBrain resolution,
    /// brain gating per perspective, and single-toggle state transitions.
    /// </summary>
    public class PerspectiveManagerTests
    {
        private GameObject _playerGo;
        private StubBrain _brain;
        private int _resolveCallCount;

        [SetUp]
        public void SetUp()
        {
            _resolveCallCount = 0;
            _playerGo = new GameObject("PerspectiveManagerTests_Player");
            _brain = _playerGo.AddComponent<StubBrain>();
            _brain.enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerGo != null)
                UnityEngine.Object.DestroyImmediate(_playerGo);
        }

        private PerspectiveManager CreateManager(PerspectiveMode initial = PerspectiveMode.ThirdPerson)
        {
            return new PerspectiveManager(
                _playerGo.transform,
                cinemachineCameraTarget: null,
                resolveCinemachineBrain: ResolveBrain,
                initialPerspective: initial);
        }

        private Behaviour ResolveBrain()
        {
            _resolveCallCount++;
            return _brain;
        }

        [Test]
        public void Initialize_IsNoOp_DoesNotResolveBrainOrChangeEnabledState()
        {
            var manager = CreateManager();

            manager.Initialize();

            Assert.AreEqual(0, _resolveCallCount);
            Assert.IsTrue(_brain.enabled);
            Assert.AreEqual(PerspectiveMode.ThirdPerson, manager.CurrentPerspective);
        }

        [Test]
        public void TogglePerspective_FromThirdPerson_SinglePress_EnablesFirstPersonAndDisablesBrain()
        {
            var manager = CreateManager(PerspectiveMode.ThirdPerson);

            manager.TogglePerspective();

            Assert.AreEqual(PerspectiveMode.FirstPerson, manager.CurrentPerspective);
            Assert.IsFalse(_brain.enabled);
            Assert.AreEqual(1, _resolveCallCount);
        }

        [Test]
        public void TogglePerspective_FromThirdPerson_Twice_ReturnsToThirdPersonWithBrainEnabled()
        {
            var manager = CreateManager(PerspectiveMode.ThirdPerson);

            manager.TogglePerspective();
            manager.TogglePerspective();

            Assert.AreEqual(PerspectiveMode.ThirdPerson, manager.CurrentPerspective);
            Assert.IsTrue(_brain.enabled);
        }

        [Test]
        public void TogglePerspective_FromFirstPerson_EnablesBrain()
        {
            var manager = CreateManager(PerspectiveMode.FirstPerson);
            _brain.enabled = false;

            manager.TogglePerspective();

            Assert.AreEqual(PerspectiveMode.ThirdPerson, manager.CurrentPerspective);
            Assert.IsTrue(_brain.enabled);
        }

        [Test]
        public void TogglePerspective_ResolvesBrainLazily_AndCachesResult()
        {
            var manager = CreateManager();

            manager.TogglePerspective();
            manager.TogglePerspective();

            Assert.AreEqual(1, _resolveCallCount);
        }

        [Test]
        public void TogglePerspective_WithNullBrain_DoesNotThrow()
        {
            var manager = new PerspectiveManager(
                _playerGo.transform,
                cinemachineCameraTarget: null,
                resolveCinemachineBrain: () => null,
                initialPerspective: PerspectiveMode.ThirdPerson);

            Assert.DoesNotThrow(() => manager.TogglePerspective());
            Assert.AreEqual(PerspectiveMode.FirstPerson, manager.CurrentPerspective);
        }

        /// <summary>Minimal stand-in for CinemachineBrain's enabled gate.</summary>
        private sealed class StubBrain : MonoBehaviour { }
    }
}

using System.Collections.Generic;
using GameCore.Actors;
using GameCore.Combat.Targeting;
using GameCore.PlayerData;
using GameCore.Visuals.Highlight;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class AttackTargetHighlightServiceTests
    {
        [Test]
        public void UpdateHover_UsesResolverAndPresenter()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));

            var targetRoot = new GameObject("TargetActor");
            targetRoot.transform.position = new Vector3(0.4f, 1f, 2f);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(targetRoot.transform, false);

            var attacker = new StubActor(new GameObject("Attacker").transform);
            var target = new StubActor(targetRoot.transform);
            var presenter = new RecordingHighlightPresenter();
            var service = new AttackTargetHighlightService(
                presenter,
                () => new List<IActor> { target });
            var resolver = new RendererScreenActorTargetResolver();
            Vector2 screenPoint = camera.WorldToScreenPoint(visual.GetComponent<Renderer>().bounds.center);

            try
            {
                service.UpdateHover(attacker, camera, screenPoint, resolver);

                Assert.AreSame(targetRoot.transform, presenter.LastTarget);
                Assert.IsTrue(presenter.LastHighlighted);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(attacker.Transform.gameObject);
                Object.DestroyImmediate(targetRoot);
            }
        }

        [Test]
        public void Clear_ClearsPresenterHighlight()
        {
            var presenter = new RecordingHighlightPresenter();
            var service = new AttackTargetHighlightService(presenter);

            service.Clear();

            Assert.AreEqual(0, presenter.HighlightCallCount);
            Assert.AreEqual(0, presenter.UnhighlightCallCount);
        }

        private sealed class StubActor : IActor
        {
            public StubActor(Transform transform) => Transform = transform;

            public int OwnerId => 0;

            public bool IsLocalPlayer => true;

            public string DisplayName => "Stub";

            public ICharacterSheet Sheet => null;

            public IPlayerDataService DataService => null;

            public Transform Transform { get; }
        }

        private sealed class RecordingHighlightPresenter : IEntityHighlightPresenter
        {
            public Transform LastTarget { get; private set; }

            public bool LastHighlighted { get; private set; }

            public int HighlightCallCount { get; private set; }

            public int UnhighlightCallCount { get; private set; }

            public void SetHighlighted(Transform target, bool highlighted)
            {
                LastTarget = target;
                LastHighlighted = highlighted;

                if (highlighted)
                    HighlightCallCount++;
                else
                    UnhighlightCallCount++;
            }
        }
    }
}

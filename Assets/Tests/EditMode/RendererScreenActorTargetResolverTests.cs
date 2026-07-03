using System.Collections.Generic;
using GameCore.Actors;
using GameCore.Combat.Targeting;
using GameCore.Interaction.ScreenPick;
using GameCore.PlayerData;
using GameCore.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class RendererScreenActorTargetResolverTests
    {
        [Test]
        public void TryResolveTarget_SelectsActorWhenCursorIsDirectlyOverRenderer()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));

            var targetRoot = new GameObject("TargetActor");
            targetRoot.transform.position = new Vector3(0.4f, 1f, 2f);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(targetRoot.transform, false);

            var actor = new StubActor(targetRoot.transform);
            var resolver = new RendererScreenActorTargetResolver();
            Vector2 screenPoint = camera.WorldToScreenPoint(visual.GetComponent<Renderer>().bounds.center);

            try
            {
                Assert.IsTrue(resolver.TryResolveTarget(
                    camera,
                    screenPoint,
                    new List<IActor> { actor },
                    excludeActor: null,
                    out IActor resolved));
                Assert.AreSame(actor, resolved);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetRoot);
            }
        }

        [Test]
        public void TryResolveTarget_DoesNotSelectActorWhenCursorIsOutsideRenderer()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));

            var targetRoot = CreateTargetWithVisual(new Vector3(0.4f, 1f, 2f));
            var actor = new StubActor(targetRoot.transform);
            var resolver = new RendererScreenActorTargetResolver();

            try
            {
                Assert.IsFalse(resolver.TryResolveTarget(
                    camera,
                    new Vector2(32f, 32f),
                    new List<IActor> { actor },
                    excludeActor: null,
                    out _));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetRoot);
            }
        }

        [Test]
        public void TryResolveTarget_DoesNotSelectActorWhenCursorIsNearButOutsideInsetBounds()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));

            var targetRoot = CreateTargetWithVisual(new Vector3(2.5f, 1f, 2f));
            var actor = new StubActor(targetRoot.transform);
            var resolver = new RendererScreenActorTargetResolver();
            Bounds bounds = targetRoot.GetComponentInChildren<Renderer>().bounds;
            Vector2 screenPoint = camera.WorldToScreenPoint(bounds.center);
            screenPoint.x = camera.WorldToScreenPoint(bounds.max).x + 40f;

            try
            {
                Assert.IsFalse(resolver.TryResolveTarget(
                    camera,
                    screenPoint,
                    new List<IActor> { actor },
                    excludeActor: null,
                    out _));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetRoot);
            }
        }

        [Test]
        public void TryResolveTarget_ExcludesNamedOverlayRenderers()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));

            var targetRoot = new GameObject("TargetActor");
            targetRoot.transform.position = new Vector3(0.4f, 1f, 2f);
            var overlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overlay.name = VisualOverlayConstants.OverlayObjectName;
            overlay.transform.SetParent(targetRoot.transform, false);

            var actor = new StubActor(targetRoot.transform);
            var resolver = new RendererScreenActorTargetResolver();
            Vector2 screenPoint = camera.WorldToScreenPoint(overlay.GetComponent<Renderer>().bounds.center);

            try
            {
                Assert.IsFalse(resolver.TryResolveTarget(
                    camera,
                    screenPoint,
                    new List<IActor> { actor },
                    excludeActor: null,
                    out _));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetRoot);
            }
        }

        private static GameObject CreateTargetWithVisual(Vector3 position)
        {
            var targetRoot = new GameObject("TargetActor");
            targetRoot.transform.position = position;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(targetRoot.transform, false);
            return targetRoot;
        }

        private sealed class StubActor : IActor
        {
            public StubActor(Transform transform) => Transform = transform;

            public int OwnerId => 0;

            public bool IsLocalPlayer => false;

            public string DisplayName => "Stub";

            public ICharacterSheet Sheet => null;

            public IPlayerDataService DataService => null;

            public Transform Transform { get; }
        }
    }
}

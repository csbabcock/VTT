using GameCore.Interaction.ScreenPick;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class RendererScreenPickQueryTests
    {
        [Test]
        public void TryPickClosest_ReturnsTrueWhenCursorIsOverPickableRenderer()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));

            var targetRoot = new GameObject("Target");
            targetRoot.transform.position = new Vector3(0f, 1f, 2f);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(targetRoot.transform, false);

            Vector2 screenPoint = camera.WorldToScreenPoint(visual.GetComponent<Renderer>().bounds.center);

            try
            {
                Assert.IsTrue(RendererScreenPickQuery.TryPickClosest(
                    camera,
                    screenPoint,
                    targetRoot.transform,
                    ScreenSpacePickSettings.Default,
                    PickableRendererFilters.ExcludeCombatOverlays,
                    out _));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetRoot);
            }
        }

        [Test]
        public void TryPickClosest_ReturnsFalseWhenFilterExcludesRenderer()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));

            var targetRoot = new GameObject("Target");
            targetRoot.transform.position = new Vector3(0f, 1f, 2f);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(targetRoot.transform, false);
            visual.name = "BlockedRenderer";

            Vector2 screenPoint = camera.WorldToScreenPoint(visual.GetComponent<Renderer>().bounds.center);
            var filter = new ExcludeNamedRendererFilter("BlockedRenderer");

            try
            {
                Assert.IsFalse(RendererScreenPickQuery.TryPickClosest(
                    camera,
                    screenPoint,
                    targetRoot.transform,
                    ScreenSpacePickSettings.Default,
                    filter,
                    out _));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetRoot);
            }
        }
    }
}

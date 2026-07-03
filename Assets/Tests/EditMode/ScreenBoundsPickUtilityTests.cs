using GameCore.Interaction.ScreenPick;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class ScreenBoundsPickUtilityTests
    {
        [Test]
        public void IsScreenPointInsideInsetBounds_ReturnsTrueForCenterPoint()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -5f);
            camera.transform.rotation = Quaternion.identity;

            var bounds = new Bounds(new Vector3(0f, 0f, 2f), Vector3.one);
            Vector2 screenPoint = camera.WorldToScreenPoint(bounds.center);

            try
            {
                Assert.IsTrue(ScreenBoundsPickUtility.IsScreenPointInsideInsetBounds(
                    camera,
                    screenPoint,
                    bounds,
                    insetFraction: 0.1f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void IsScreenPointInsideInsetBounds_ReturnsFalseForFarPoint()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -5f);
            camera.transform.rotation = Quaternion.identity;

            var bounds = new Bounds(new Vector3(0f, 0f, 2f), Vector3.one);

            try
            {
                Assert.IsFalse(ScreenBoundsPickUtility.IsScreenPointInsideInsetBounds(
                    camera,
                    new Vector2(12f, 18f),
                    bounds,
                    insetFraction: 0.1f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void IsScreenPointNearWorldPoint_RespectsPixelThreshold()
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -5f);
            camera.transform.rotation = Quaternion.identity;

            Vector3 worldPoint = new Vector3(0f, 0f, 2f);
            Vector2 screenPoint = camera.WorldToScreenPoint(worldPoint);

            try
            {
                Assert.IsTrue(ScreenBoundsPickUtility.IsScreenPointNearWorldPoint(
                    camera,
                    screenPoint,
                    worldPoint,
                    maxPixelDistance: 1f));

                Assert.IsFalse(ScreenBoundsPickUtility.IsScreenPointNearWorldPoint(
                    camera,
                    screenPoint + new Vector2(20f, 0f),
                    worldPoint,
                    maxPixelDistance: 1f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}

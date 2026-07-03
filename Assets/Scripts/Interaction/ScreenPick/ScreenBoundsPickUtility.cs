using UnityEngine;

namespace GameCore.Interaction.ScreenPick
{
    /// <summary>Pure screen-space math for bounds picking. Edit Mode testable.</summary>
    public static class ScreenBoundsPickUtility
    {
        public static bool IsScreenPointInsideInsetBounds(
            Camera camera,
            Vector2 screenPoint,
            Bounds bounds,
            float insetFraction)
        {
            if (camera == null || !TryGetScreenRect(camera, bounds, out float minX, out float minY, out float maxX, out float maxY))
                return false;

            float insetX = (maxX - minX) * insetFraction;
            float insetY = (maxY - minY) * insetFraction;
            minX += insetX;
            maxX -= insetX;
            minY += insetY;
            maxY -= insetY;

            if (minX > maxX || minY > maxY)
                return false;

            return screenPoint.x >= minX
                && screenPoint.x <= maxX
                && screenPoint.y >= minY
                && screenPoint.y <= maxY;
        }

        public static bool IsScreenPointNearWorldPoint(
            Camera camera,
            Vector2 screenPosition,
            Vector3 worldPoint,
            float maxPixelDistance)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z < 0f)
                return false;

            float pixelDistance = Vector2.Distance(
                new Vector2(screenPoint.x, screenPoint.y),
                screenPosition);
            return pixelDistance <= maxPixelDistance;
        }

        public static bool TryGetBoundsPickPoint(
            Camera camera,
            Vector2 screenPosition,
            Ray ray,
            Bounds bounds,
            ScreenSpacePickSettings settings,
            out float rayDistance)
        {
            rayDistance = 0f;

            Vector3 toBounds = bounds.center - ray.origin;
            rayDistance = Vector3.Dot(toBounds, ray.direction);
            if (rayDistance < 0f)
                return false;

            Vector3 pointOnRay = ray.GetPoint(rayDistance);
            Vector3 pickPoint = bounds.ClosestPoint(pointOnRay);
            return IsScreenPointNearWorldPoint(
                camera,
                screenPosition,
                pickPoint,
                settings.MaxPixelDistance);
        }

        private static bool TryGetScreenRect(
            Camera camera,
            Bounds bounds,
            out float minX,
            out float minY,
            out float maxX,
            out float maxY)
        {
            minX = float.MaxValue;
            minY = float.MaxValue;
            maxX = float.MinValue;
            maxY = float.MinValue;

            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            bool hasVisibleCorner = false;

            for (int xi = -1; xi <= 1; xi += 2)
            {
                for (int yi = -1; yi <= 1; yi += 2)
                {
                    for (int zi = -1; zi <= 1; zi += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                        Vector3 screen = camera.WorldToScreenPoint(corner);
                        if (screen.z < 0f)
                            continue;

                        hasVisibleCorner = true;
                        minX = Mathf.Min(minX, screen.x);
                        maxX = Mathf.Max(maxX, screen.x);
                        minY = Mathf.Min(minY, screen.y);
                        maxY = Mathf.Max(maxY, screen.y);
                    }
                }
            }

            return hasVisibleCorner;
        }
    }
}

using UnityEngine;

namespace GameCore.Interaction.ScreenPick
{
    /// <summary>Finds the closest renderer hit under a screen-space cursor.</summary>
    public static class RendererScreenPickQuery
    {
        public static bool TryPickClosest(
            Camera camera,
            Vector2 screenPosition,
            Transform root,
            ScreenSpacePickSettings settings,
            IPickableRendererFilter filter,
            out float hitDistance)
        {
            hitDistance = float.MaxValue;
            if (camera == null || root == null)
                return false;

            Ray ray = camera.ScreenPointToRay(screenPosition);
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
            bool hasHit = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || filter != null && !filter.ShouldInclude(renderer))
                    continue;

                Bounds bounds = renderer.bounds;
                if (!ScreenBoundsPickUtility.IsScreenPointInsideInsetBounds(
                        camera,
                        screenPosition,
                        bounds,
                        settings.BoundsInsetFraction))
                {
                    continue;
                }

                if (!ScreenBoundsPickUtility.TryGetBoundsPickPoint(
                        camera,
                        screenPosition,
                        ray,
                        bounds,
                        settings,
                        out float rayDistance))
                {
                    continue;
                }

                if (rayDistance >= hitDistance)
                    continue;

                hitDistance = rayDistance;
                hasHit = true;
            }

            return hasHit;
        }
    }
}

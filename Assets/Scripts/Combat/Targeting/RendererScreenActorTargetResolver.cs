using System.Collections.Generic;
using GameCore.Actors;
using GameCore.Interaction.ScreenPick;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>
    /// Resolves combat targets by picking the actor whose renderers are under the cursor.
    /// </summary>
    public sealed class RendererScreenActorTargetResolver : IActorTargetResolver
    {
        private readonly ScreenSpacePickSettings _settings;
        private readonly IPickableRendererFilter _rendererFilter;

        public RendererScreenActorTargetResolver()
            : this(ScreenSpacePickSettings.Default, PickableRendererFilters.ExcludeCombatOverlays)
        {
        }

        public RendererScreenActorTargetResolver(
            ScreenSpacePickSettings settings,
            IPickableRendererFilter rendererFilter)
        {
            _settings = settings;
            _rendererFilter = rendererFilter ?? PickableRendererFilters.ExcludeCombatOverlays;
        }

        public bool TryResolveTarget(
            Camera camera,
            Vector2 screenPosition,
            IReadOnlyList<IActor> candidates,
            IActor excludeActor,
            out IActor target)
        {
            target = null;

            if (camera == null || candidates == null || candidates.Count == 0)
                return false;

            IActor best = null;
            float bestHitDistance = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                IActor candidate = candidates[i];
                if (candidate == null
                    || ReferenceEquals(candidate, excludeActor)
                    || candidate.Transform == null)
                {
                    continue;
                }

                if (!RendererScreenPickQuery.TryPickClosest(
                        camera,
                        screenPosition,
                        candidate.Transform,
                        _settings,
                        _rendererFilter,
                        out float hitDistance))
                {
                    continue;
                }

                if (hitDistance >= bestHitDistance)
                    continue;

                bestHitDistance = hitDistance;
                best = candidate;
            }

            target = best;
            return best != null;
        }
    }
}

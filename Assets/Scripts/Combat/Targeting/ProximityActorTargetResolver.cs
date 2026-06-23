using System.Collections.Generic;
using GameCore.Actors;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>
    /// Picks the actor closest to the camera ray within a cylindrical radius.
    /// Works with CharacterController avatars that do not expose physics colliders.
    /// </summary>
    public sealed class ProximityActorTargetResolver : IActorTargetResolver
    {
        private readonly float _pickRadiusWorldUnits;

        public ProximityActorTargetResolver(float pickRadiusWorldUnits = 1.25f)
        {
            _pickRadiusWorldUnits = pickRadiusWorldUnits;
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

            Ray ray = camera.ScreenPointToRay(screenPosition);
            IActor best = null;
            float bestDistanceToRay = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                IActor candidate = candidates[i];
                if (candidate == null
                    || ReferenceEquals(candidate, excludeActor)
                    || candidate.Transform == null)
                {
                    continue;
                }

                Vector3 actorPosition = candidate.Transform.position;
                Vector3 toActor = actorPosition - ray.origin;
                float projection = Vector3.Dot(toActor, ray.direction);
                if (projection < 0f)
                    continue;

                Vector3 closestPointOnRay = ray.origin + ray.direction * projection;
                float distanceToRay = Vector3.Distance(closestPointOnRay, actorPosition);
                if (distanceToRay > _pickRadiusWorldUnits || distanceToRay >= bestDistanceToRay)
                    continue;

                bestDistanceToRay = distanceToRay;
                best = candidate;
            }

            target = best;
            return best != null;
        }
    }
}

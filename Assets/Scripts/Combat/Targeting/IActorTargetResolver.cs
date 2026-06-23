using System.Collections.Generic;
using GameCore.Actors;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>Resolves a screen-space click to a scene actor.</summary>
    public interface IActorTargetResolver
    {
        bool TryResolveTarget(
            Camera camera,
            Vector2 screenPosition,
            IReadOnlyList<IActor> candidates,
            IActor excludeActor,
            out IActor target);
    }
}

using System;
using System.Collections.Generic;
using GameCore.Actors;
using GameCore.Visuals.Highlight;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>Shows a hover-only target outline while the player is choosing an attack target.</summary>
    public sealed class AttackTargetHighlightService
    {
        private readonly HoverHighlightService _highlightService;
        private readonly Func<IReadOnlyList<IActor>> _getCandidates;

        public AttackTargetHighlightService(
            IEntityHighlightPresenter presenter = null,
            Func<IReadOnlyList<IActor>> getCandidates = null)
        {
            _highlightService = new HoverHighlightService(
                presenter ?? new QuickOutlineHighlightPresenter());
            _getCandidates = getCandidates ?? (() => ActorRegistry.Actors);
        }

        public void UpdateHover(
            IActor attacker,
            Camera camera,
            Vector2 screenPosition,
            IActorTargetResolver resolver)
        {
            Transform hoveredRoot = null;
            if (attacker != null
                && camera != null
                && resolver != null
                && resolver.TryResolveTarget(
                    camera,
                    screenPosition,
                    _getCandidates(),
                    attacker,
                    out IActor candidate)
                && candidate?.Transform != null)
            {
                hoveredRoot = candidate.Transform;
            }

            _highlightService.UpdateHover(hoveredRoot);
        }

        public void Clear() => _highlightService.Clear();
    }
}

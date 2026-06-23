using GameCore.Actors;
using GameCore.Combat.Feedback;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>Shows a hover-only target outline while the player is choosing an attack target.</summary>
    public sealed class AttackTargetHighlightService
    {
        private IActor _hoveredActor;

        public void UpdateHover(
            IActor attacker,
            Camera camera,
            Vector2 screenPosition,
            IActorTargetResolver resolver)
        {
            IActor hovered = null;
            if (attacker != null
                && camera != null
                && resolver != null
                && resolver.TryResolveTarget(
                    camera,
                    screenPosition,
                    ActorRegistry.Actors,
                    attacker,
                    out IActor candidate)
                && IsAttackableCandidate(attacker, candidate))
            {
                hovered = candidate;
            }

            if (ReferenceEquals(_hoveredActor, hovered))
                return;

            Clear();
            _hoveredActor = hovered;

            if (_hoveredActor?.Transform != null)
            {
                EntityCombatOverlay overlay = EntityCombatOverlay.GetOrCreate(_hoveredActor.Transform);
                overlay?.SetTargetOutline(true);
            }
        }

        public void Clear()
        {
            if (_hoveredActor?.Transform != null)
            {
                EntityCombatOverlay overlay = EntityCombatOverlay.GetOrCreate(_hoveredActor.Transform);
                overlay?.SetTargetOutline(false);
            }

            _hoveredActor = null;
        }

        internal static bool IsAttackableCandidate(IActor attacker, IActor candidate)
        {
            if (candidate == null || attacker == null || ReferenceEquals(candidate, attacker))
                return false;

            if (candidate.Transform == null)
                return false;

            if (candidate.OwnerId == attacker.OwnerId && candidate.OwnerId != 0)
                return false;

            var authority = CharacterSheetAuthorityHelper.GetAuthority(candidate);
            return authority != null && authority.CurrentHitPoints > 0;
        }
    }
}

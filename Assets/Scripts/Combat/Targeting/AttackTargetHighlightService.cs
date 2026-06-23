using System.Collections.Generic;
using GameCore.Actors;
using GameCore.Combat.Feedback;
using UnityEngine;

namespace GameCore.Combat.Targeting
{
    /// <summary>Highlights valid attack targets while the player is choosing a target.</summary>
    public sealed class AttackTargetHighlightService
    {
        private static readonly Color HighlightTint = new Color(1f, 0.55f, 0.15f, 1f);
        private readonly List<EntityTintEffect> _highlighted = new List<EntityTintEffect>();

        public void ShowAttackableTargets(IActor attacker)
        {
            Clear();

            IReadOnlyList<IActor> actors = ActorRegistry.Actors;
            for (int i = 0; i < actors.Count; i++)
            {
                IActor candidate = actors[i];
                if (!IsAttackableCandidate(attacker, candidate))
                    continue;

                EntityTintEffect effect = EntityTintEffect.GetOrCreate(candidate.Transform);
                if (effect == null)
                    continue;

                effect.SetHighlight(true, HighlightTint);
                _highlighted.Add(effect);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _highlighted.Count; i++)
            {
                if (_highlighted[i] != null)
                    _highlighted[i].SetHighlight(false);
            }

            _highlighted.Clear();
        }

        private static bool IsAttackableCandidate(IActor attacker, IActor candidate)
        {
            if (candidate == null || attacker == null || ReferenceEquals(candidate, attacker))
                return false;

            if (candidate.Transform == null || candidate.Sheet == null)
                return false;

            var authority = CharacterSheetAuthorityHelper.GetAuthority(candidate);
            if (authority == null || authority.CurrentHitPoints <= 0)
                return false;

            return true;
        }
    }
}

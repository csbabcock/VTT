using GameCore.Combat.Models;
using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>Routes completed attacks to the actor's presentation adapter.</summary>
    public static class AttackAnimationFeedback
    {
        public const string TriggerName = "Attack";

        public static void PlayIfCompleted(Transform attacker, CombatActionResult result)
        {
            // A completed miss still swings. Rejected actions do not.
            if (!result.Succeeded || attacker == null)
                return;

            var player = attacker.GetComponent<IAttackAnimationPlayer>();
            player?.PlayAttack();
        }
    }
}

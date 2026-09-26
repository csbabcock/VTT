using UnityEngine;

namespace GameCore.Combat.Feedback
{
    public static class HitAnimationFeedback
    {
        public const string TriggerName = "Hit";

        public static void PlayIfHit(Transform target, bool didHit)
        {
            if (target == null || !didHit)
                return;

            target.GetComponent<IHitAnimationPlayer>()?.PlayHit();
        }

        public static void PlayIfDamaged(Transform target, int damageAmount)
        {
            PlayIfHit(target, damageAmount > 0);
        }
    }
}

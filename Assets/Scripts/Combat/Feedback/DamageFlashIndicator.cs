using GameCore.Combat.Feedback;
using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>Quick red flash on an entity when it takes damage.</summary>
    public static class DamageFlashIndicator
    {
        private static readonly Color BrightRed = new Color(1f, 0f, 0f, 1f);
        private const float FlashDuration = 0.45f;

        public static void Flash(Transform root)
        {
            if (root == null)
                return;

            EntityTintEffect effect = EntityTintEffect.GetOrCreate(root);
            effect?.FlashDamage(BrightRed, FlashDuration);
        }
    }
}

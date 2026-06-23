using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>Runtime tint overlay for highlight or damage flash on an entity.</summary>
    public sealed class EntityTintEffect : MonoBehaviour
    {
        private readonly RendererTintUtility _tint = new RendererTintUtility();
        private bool _isCaptured;

        public static EntityTintEffect GetOrCreate(Transform root)
        {
            if (root == null)
                return null;

            var effect = root.GetComponent<EntityTintEffect>();
            if (effect == null)
                effect = root.gameObject.AddComponent<EntityTintEffect>();

            return effect;
        }

        public void SetHighlight(bool active, Color? tint = null)
        {
            EnsureCaptured();
            if (!_isCaptured)
                return;

            if (!active)
            {
                _tint.Restore();
                return;
            }

            Color highlight = tint ?? new Color(1f, 0.55f, 0.15f, 1f);
            Color emission = new Color(1f, 0.35f, 0.05f, 1f);
            _tint.ApplyTint(highlight, emission, enableEmission: true);
        }

        public void FlashDamage(Color flashColor, float duration)
        {
            EnsureCaptured();
            if (!_isCaptured)
                return;

            StopAllCoroutines();
            StartCoroutine(CoFlash(flashColor, duration));
        }

        private System.Collections.IEnumerator CoFlash(Color flashColor, float duration)
        {
            Color emission = new Color(flashColor.r, flashColor.g * 0.2f, flashColor.b * 0.2f, 1f);
            _tint.ApplyTint(flashColor, emission, enableEmission: true);
            yield return new WaitForSeconds(duration);
            _tint.Restore();
        }

        private void EnsureCaptured()
        {
            if (_isCaptured)
                return;

            _tint.Capture(transform);
            _isCaptured = _tint.SlotCount > 0;
        }

        private void OnDisable()
        {
            if (_isCaptured)
                _tint.Restore();
        }
    }
}

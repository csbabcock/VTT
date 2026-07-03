using UnityEngine;

namespace GameCore.Visuals.Highlight
{
    /// <summary>
    /// Runtime outline effect using QuickOutline. Add once per entity root, then toggle via
    /// <see cref="Outline.enabled"/> on the underlying component.
    /// </summary>
    public sealed class EntityOutlineEffect : MonoBehaviour
    {
        private static readonly Color DefaultOutlineColor = new Color(1f, 0.92f, 0.35f, 1f);
        private const float DefaultOutlineWidth = 3f;
        private const Outline.Mode DefaultOutlineMode = Outline.Mode.OutlineAll;

        private Outline _outline;
        private bool _initialized;

        public static EntityOutlineEffect GetOrCreate(Transform root)
        {
            if (root == null)
                return null;

            var effect = root.GetComponent<EntityOutlineEffect>();
            if (effect == null)
                effect = root.gameObject.AddComponent<EntityOutlineEffect>();

            return effect;
        }

        private void Awake() => EnsureOutlineHidden();

        public void SetVisible(bool visible)
        {
            Outline outline = GetOrCreateOutline();
            if (outline == null)
                return;

            outline.enabled = visible;
        }

        private void EnsureOutlineHidden()
        {
            if (_initialized)
                return;

            Outline outline = GetComponentInChildren<Outline>(includeInactive: true);
            if (outline != null)
                outline.enabled = false;

            _initialized = true;
        }

        private Outline GetOrCreateOutline()
        {
            if (_outline != null)
                return _outline;

            _outline = GetComponentInChildren<Outline>(includeInactive: true);
            if (_outline == null)
            {
                _outline = gameObject.AddComponent<Outline>();
                _outline.OutlineMode = DefaultOutlineMode;
                _outline.OutlineColor = DefaultOutlineColor;
                _outline.OutlineWidth = DefaultOutlineWidth;
            }

            return _outline;
        }

        private void OnDisable()
        {
            if (_outline != null)
                _outline.enabled = false;
        }
    }
}

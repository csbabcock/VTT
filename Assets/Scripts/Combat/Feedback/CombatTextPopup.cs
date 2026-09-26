using TMPro;
using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>A short-lived, camera-facing world label for completed attack outcomes.</summary>
    public sealed class CombatTextPopup : MonoBehaviour
    {
        private const float Lifetime = 1.2f;
        private const float RiseDistance = 0.65f;
        private TextMeshPro _label;
        private Material _material;
        private Vector3 _origin;
        private float _elapsed;

        public static Vector3 GetPositionAbove(Transform target)
        {
            var animator = target.GetComponent<Animator>();
            if (animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid)
            {
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                    return head.position + Vector3.up * 0.5f;
            }

            var collider = target.GetComponent<Collider>();
            if (collider != null && collider.enabled)
                return new Vector3(collider.bounds.center.x, collider.bounds.max.y + 0.35f, collider.bounds.center.z);

            return target.position + Vector3.up * 2f;
        }

        public static CombatTextPopup ShowAbove(Transform target, bool didHit = false, int damageAmount = 0)
        {
            return target != null ? ShowAt(GetPositionAbove(target), didHit, damageAmount) : null;
        }

        public static CombatTextPopup ShowAt(Vector3 position, bool didHit = false, int damageAmount = 0)
        {
            var root = new GameObject(didHit ? "Damage Popup" : "Miss Popup");
            root.layer = 2; // Ignore Raycast: the visual must not interfere with targeting.
            root.transform.position = position;
            var popup = root.AddComponent<CombatTextPopup>();
            popup._origin = position;
            popup._label = root.AddComponent<TextMeshPro>();
            popup._label.text = didHit ? $"{Mathf.Max(0, damageAmount)}" : "Miss";
            popup._label.fontSize = 3f;
            popup._label.fontStyle = FontStyles.Bold;
            popup._label.alignment = TextAlignmentOptions.Center;
            popup._label.textWrappingMode = TextWrappingModes.NoWrap;
            popup._label.overflowMode = TextOverflowModes.Overflow;
            popup._label.color = didHit
                ? new Color(1f, 0.3f, 0.25f, 1f)
                : new Color(1f, 0.9f, 0.55f, 1f);
            if (popup._label.fontSharedMaterial != null)
            {
                popup._material = new Material(popup._label.fontSharedMaterial);
                popup._material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
                popup._material.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(25, 20, 10, 220));
                popup._label.fontSharedMaterial = popup._material;
                popup._label.UpdateMeshPadding();
            }
            popup._label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            popup._label.raycastTarget = false;
            popup.Advance(0f, Camera.main);
            return popup;
        }

        private void OnDestroy()
        {
            if (_material == null)
                return;
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
        }

        private void LateUpdate()
        {
            if (Advance(Time.deltaTime, Camera.main))
                Destroy(gameObject);
        }

        // Returns completion separately so the movement/fade can be verified without scene timing.
        private bool Advance(float deltaTime, Camera camera)
        {
            _elapsed += Mathf.Max(0f, deltaTime);
            float progress = Mathf.Clamp01(_elapsed / Lifetime);
            transform.position = _origin + Vector3.up * (RiseDistance * progress);
            transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(_elapsed / 0.12f));
            if (camera != null)
                transform.rotation = camera.transform.rotation;
            _label.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, Lifetime, _elapsed));
            return _elapsed >= Lifetime;
        }
    }
}

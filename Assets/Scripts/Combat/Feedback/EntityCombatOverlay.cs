using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameCore.Combat.Feedback
{
    /// <summary>
    /// Skinned mesh overlay for combat target silhouettes and full-body damage flashes.
    /// Works independently of the character's base URP Lit materials.
    /// </summary>
    public sealed class EntityCombatOverlay : MonoBehaviour
    {
        private const string OverlayObjectName = "CombatVisualOverlay";

        private static Material _silhouetteTemplate;

        private readonly List<OverlayLayer> _layers = new List<OverlayLayer>();
        private bool _isBuilt;
        private bool _outlineVisible;
        private Coroutine _flashRoutine;

        private struct OverlayLayer
        {
            public Renderer Overlay;
            public Material[] Materials;
        }

        public static EntityCombatOverlay GetOrCreate(Transform root)
        {
            if (root == null)
                return null;

            var overlay = root.GetComponent<EntityCombatOverlay>();
            if (overlay == null)
                overlay = root.gameObject.AddComponent<EntityCombatOverlay>();

            return overlay;
        }

        public void SetTargetOutline(bool visible)
        {
            EnsureBuilt();
            _outlineVisible = visible;
            ApplyOverlayState();
        }

        public void PlayDamageFlash(Color color, float duration)
        {
            EnsureBuilt();
            if (!_isBuilt)
                return;

            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            _flashRoutine = StartCoroutine(CoDamageFlash(color, duration));
        }

        private IEnumerator CoDamageFlash(Color color, float duration)
        {
            SetAllLayerColors(color, 1f);
            SetOverlaysEnabled(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                SetAllLayerColors(color, alpha);
                yield return null;
            }

            _flashRoutine = null;
            ApplyOverlayState();
        }

        private void EnsureBuilt()
        {
            if (_isBuilt)
                return;

            SkinnedMeshRenderer[] skinnedRenderers =
                GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                TryAddSkinnedOverlay(skinnedRenderers[i]);
            }

            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                TryAddMeshOverlay(meshRenderers[i]);
            }

            _isBuilt = _layers.Count > 0;
            ApplyOverlayState();
        }

        private void TryAddSkinnedOverlay(SkinnedMeshRenderer source)
        {
            if (source == null || source.sharedMesh == null || IsOverlayRenderer(source))
                return;

            var overlayObject = new GameObject(OverlayObjectName);
            overlayObject.transform.SetParent(source.transform, false);

            var overlayRenderer = overlayObject.AddComponent<SkinnedMeshRenderer>();
            overlayRenderer.sharedMesh = source.sharedMesh;
            overlayRenderer.bones = source.bones;
            overlayRenderer.rootBone = source.rootBone;
            overlayRenderer.localBounds = source.localBounds;
            overlayRenderer.sharedMaterials = CreateMaterialArray(source.sharedMaterials.Length);
            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            overlayRenderer.updateWhenOffscreen = true;
            overlayObject.SetActive(false);

            _layers.Add(new OverlayLayer
            {
                Overlay = overlayRenderer,
                Materials = overlayRenderer.sharedMaterials,
            });
        }

        private void TryAddMeshOverlay(MeshRenderer source)
        {
            if (source == null || IsOverlayRenderer(source))
                return;

            MeshFilter filter = source.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return;

            var overlayObject = new GameObject(OverlayObjectName);
            overlayObject.transform.SetParent(source.transform, false);

            var meshFilter = overlayObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = filter.sharedMesh;

            var overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
            overlayRenderer.sharedMaterials = CreateMaterialArray(source.sharedMaterials.Length);
            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            overlayObject.SetActive(false);

            _layers.Add(new OverlayLayer
            {
                Overlay = overlayRenderer,
                Materials = overlayRenderer.sharedMaterials,
            });
        }

        private static Material[] CreateMaterialArray(int sourceMaterialCount)
        {
            int count = Mathf.Max(1, sourceMaterialCount);
            var materials = new Material[count];
            for (int i = 0; i < count; i++)
                materials[i] = CreateSilhouetteMaterialInstance();

            return materials;
        }

        private static Material CreateSilhouetteMaterialInstance()
        {
            Material template = GetSilhouetteTemplate();
            return new Material(template);
        }

        private void ApplyOverlayState()
        {
            if (!_isBuilt || _flashRoutine != null)
                return;

            if (_outlineVisible)
            {
                SetAllLayerColors(GetSilhouetteColor(), GetSilhouetteColor().a);
                SetOverlaysEnabled(true);
                return;
            }

            SetOverlaysEnabled(false);
        }

        private void SetAllLayerColors(Color color, float alpha)
        {
            color.a = alpha;
            for (int i = 0; i < _layers.Count; i++)
            {
                Material[] materials = _layers[i].Materials;
                if (materials == null)
                    continue;

                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null)
                        continue;

                    material.color = color;
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", color);
                }
            }
        }

        private void SetOverlaysEnabled(bool enabled)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                Renderer overlay = _layers[i].Overlay;
                if (overlay != null)
                    overlay.gameObject.SetActive(enabled);
            }
        }

        private static Color GetSilhouetteColor() => new Color(1f, 1f, 1f, 0.5f);

        private static Material GetSilhouetteTemplate()
        {
            if (_silhouetteTemplate != null)
                return _silhouetteTemplate;

            Shader shader = Shader.Find("GameCore/Combat/Silhouette");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            _silhouetteTemplate = new Material(shader);
            ConfigureTransparentMaterial(_silhouetteTemplate, GetSilhouetteColor());
            return _silhouetteTemplate;
        }

        private static void ConfigureTransparentMaterial(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);

            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.renderQueue = (int)RenderQueue.Transparent + 100;
        }

        private static bool IsOverlayRenderer(Renderer renderer) =>
            renderer != null && renderer.gameObject.name == OverlayObjectName;

        private void OnDisable()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            _outlineVisible = false;
            SetOverlaysEnabled(false);
        }
    }
}

using GameCore.Visuals;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameCore.Combat.Feedback
{
    /// <summary>
    /// Skinned mesh overlay for full-body damage flashes.
    /// Works independently of the character's base URP Lit materials.
    /// Target outlines are handled by <see cref="GameCore.Visuals.Highlight.EntityOutlineEffect"/>.
    /// </summary>
    public sealed class EntityCombatOverlay : MonoBehaviour
    {
        internal const string OverlayObjectName = VisualOverlayConstants.OverlayObjectName;

        private static Material _silhouetteTemplate;

        private readonly List<OverlayLayer> _layers = new List<OverlayLayer>();
        private bool _isBuilt;
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
                TryAddSkinnedOverlay(skinnedRenderers[i]);

            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            for (int i = 0; i < meshRenderers.Length; i++)
                TryAddMeshOverlay(meshRenderers[i]);

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
            Material[] materials = CreateMaterialArray(source.sharedMaterials.Length);
            overlayRenderer.materials = materials;
            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            overlayRenderer.updateWhenOffscreen = true;
            overlayObject.SetActive(false);

            _layers.Add(new OverlayLayer
            {
                Overlay = overlayRenderer,
                Materials = materials,
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
            Material[] materials = CreateMaterialArray(source.sharedMaterials.Length);
            overlayRenderer.materials = materials;
            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            overlayObject.SetActive(false);

            _layers.Add(new OverlayLayer
            {
                Overlay = overlayRenderer,
                Materials = materials,
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
            var instance = new Material(template);
            instance.renderQueue = (int)RenderQueue.Transparent + 200;
            return instance;
        }

        private void ApplyOverlayState()
        {
            if (!_isBuilt || _flashRoutine != null)
                return;

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

                    material.SetColor(ShaderPropertyIds.BaseColor, color);
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
            material.SetColor(ShaderPropertyIds.BaseColor, color);

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

            material.renderQueue = (int)RenderQueue.Transparent + 200;
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

            SetOverlaysEnabled(false);
        }

        private static class ShaderPropertyIds
        {
            public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        }
    }
}

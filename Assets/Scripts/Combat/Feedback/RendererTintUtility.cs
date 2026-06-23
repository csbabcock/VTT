using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>Applies tint across every material slot on child renderers.</summary>
    public sealed class RendererTintUtility
    {
        private readonly List<SlotState> _slots = new List<SlotState>();

        private struct SlotState
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public Color OriginalBaseColor;
            public Color OriginalEmission;
            public int BaseColorId;
            public int EmissionColorId;
            public bool HadEmissionKeyword;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public int SlotCount => _slots.Count;

        public void Capture(Transform root)
        {
            _slots.Clear();
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
                    continue;

                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null)
                        continue;

                    int baseId = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
                    if (!material.HasProperty(baseId))
                        continue;

                    bool hadEmission = material.IsKeywordEnabled("_EMISSION");
                    _slots.Add(new SlotState
                    {
                        Renderer = renderer,
                        MaterialIndex = i,
                        OriginalBaseColor = material.GetColor(baseId),
                        OriginalEmission = material.HasProperty(EmissionColorId)
                            ? material.GetColor(EmissionColorId)
                            : Color.black,
                        BaseColorId = baseId,
                        EmissionColorId = EmissionColorId,
                        HadEmissionKeyword = hadEmission,
                    });
                }
            }
        }

        public void ApplyTint(Color baseTint, Color emissionTint, bool enableEmission)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                SlotState state = _slots[i];
                if (state.Renderer == null)
                    continue;

                Material[] materials = state.Renderer.materials;
                if (state.MaterialIndex < 0 || state.MaterialIndex >= materials.Length)
                    continue;

                Material material = materials[state.MaterialIndex];
                if (material == null)
                    continue;

                material.SetColor(state.BaseColorId, baseTint);
                if (material.HasProperty(state.EmissionColorId))
                {
                    if (enableEmission)
                        material.EnableKeyword("_EMISSION");

                    material.SetColor(state.EmissionColorId, emissionTint);
                }

                state.Renderer.materials = materials;
            }
        }

        public void Restore()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                SlotState state = _slots[i];
                if (state.Renderer == null)
                    continue;

                Material[] materials = state.Renderer.materials;
                if (state.MaterialIndex < 0 || state.MaterialIndex >= materials.Length)
                    continue;

                Material material = materials[state.MaterialIndex];
                if (material == null)
                    continue;

                material.SetColor(state.BaseColorId, state.OriginalBaseColor);
                if (material.HasProperty(state.EmissionColorId))
                {
                    material.SetColor(state.EmissionColorId, state.OriginalEmission);
                    if (!state.HadEmissionKeyword)
                        material.DisableKeyword("_EMISSION");
                }

                state.Renderer.materials = materials;
            }
        }
    }
}

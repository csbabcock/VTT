using UnityEngine;

namespace GameCore.EncounterMode.Grid
{
    /// <summary>
    /// Utility class for creating materials with proper shader fallbacks.
    /// Reduces code duplication and improves performance by caching shader lookups.
    /// </summary>
    public static class MaterialHelper
    {
        private static Shader _cachedUnlitShader;
        private static Shader _cachedTransparentShader;
        private static Shader _cachedDefaultShader;

        /// <summary>
        /// Gets an unlit shader with fallback support.
        /// </summary>
        public static Shader GetUnlitShader()
        {
            if (_cachedUnlitShader != null)
                return _cachedUnlitShader;

            _cachedUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_cachedUnlitShader == null)
                _cachedUnlitShader = Shader.Find("Unlit/Color");
            if (_cachedUnlitShader == null)
                _cachedUnlitShader = Shader.Find("Sprites/Default");

            return _cachedUnlitShader;
        }

        /// <summary>
        /// Gets a transparent shader with fallback support.
        /// </summary>
        public static Shader GetTransparentShader()
        {
            if (_cachedTransparentShader != null)
                return _cachedTransparentShader;

            _cachedTransparentShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_cachedTransparentShader == null)
                _cachedTransparentShader = Shader.Find("Unlit/Transparent");
            if (_cachedTransparentShader == null)
                _cachedTransparentShader = Shader.Find("Unlit/Color");
            if (_cachedTransparentShader == null)
                _cachedTransparentShader = Shader.Find("Sprites/Default");

            return _cachedTransparentShader;
        }

        /// <summary>
        /// Creates a material with the specified color.
        /// </summary>
        public static Material CreateMaterial(Color color, bool transparent = false)
        {
            Shader shader = transparent ? GetTransparentShader() : GetUnlitShader();
            if (shader == null)
                return null;

            Material material = new Material(shader);
            material.color = color;

            if (transparent)
            {
                ConfigureMaterialForTransparency(material);
            }

            return material;
        }

        /// <summary>
        /// Configures a material for transparency rendering.
        /// </summary>
        public static void ConfigureMaterialForTransparency(Material material)
        {
            if (material == null)
                return;

            // Set render queue to Transparent for proper blending
            material.renderQueue = 3000;

            // URP Unlit shader transparency settings
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1); // Transparent surface
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0); // Alpha blend
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 0); // Disable depth write for transparency

            // Standard shader transparency mode
            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3); // Transparent mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }
    }
}


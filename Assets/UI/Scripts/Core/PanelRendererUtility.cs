using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace GameCore.UI
{
    /// <summary>
    /// Resolves the document root from <see cref="PanelRenderer"/> when Unity exposes it as a property
    /// (naming varies by version; reload callback is still required when this returns null).
    /// </summary>
    internal static class PanelRendererUtility
    {
        private static readonly Lazy<Func<PanelRenderer, VisualElement>> RootGetter =
            new Lazy<Func<PanelRenderer, VisualElement>>(BuildRootGetter);

        private static Func<PanelRenderer, VisualElement> BuildRootGetter()
        {
            var type = typeof(PanelRenderer);
            foreach (string name in new[] { "rootVisualElement", "visualTree" })
            {
                PropertyInfo p = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (p != null && typeof(VisualElement).IsAssignableFrom(p.PropertyType))
                {
                    return pr => pr != null ? p.GetValue(pr) as VisualElement : null;
                }
            }

            return _ => null;
        }

        public static VisualElement TryGetRootVisualElement(PanelRenderer panel)
        {
            if (panel == null)
                return null;
            return RootGetter.Value(panel);
        }
    }
}

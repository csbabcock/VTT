using GameCore.Visuals;
using UnityEngine;

namespace GameCore.Interaction.ScreenPick
{
    /// <summary>Common renderer filters for screen picking.</summary>
    public static class PickableRendererFilters
    {
        public static IPickableRendererFilter ExcludeCombatOverlays { get; } =
            new ExcludeNamedRendererFilter(VisualOverlayConstants.OverlayObjectName);
    }

    /// <summary>Excludes renderers whose GameObject uses a specific name.</summary>
    public sealed class ExcludeNamedRendererFilter : IPickableRendererFilter
    {
        private readonly string _excludedName;

        public ExcludeNamedRendererFilter(string excludedName) =>
            _excludedName = excludedName ?? string.Empty;

        public bool ShouldInclude(Renderer renderer) =>
            renderer != null && renderer.gameObject.name != _excludedName;
    }
}

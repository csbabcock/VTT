using UnityEngine;

namespace GameCore.Interaction.ScreenPick
{
    /// <summary>Decides whether a renderer participates in screen-space picking.</summary>
    public interface IPickableRendererFilter
    {
        bool ShouldInclude(Renderer renderer);
    }
}

using UnityEngine;

namespace GameCore.Visuals.Highlight
{
    /// <summary>Applies or removes a hover/selection highlight on a scene object.</summary>
    public interface IEntityHighlightPresenter
    {
        void SetHighlighted(Transform target, bool highlighted);
    }
}

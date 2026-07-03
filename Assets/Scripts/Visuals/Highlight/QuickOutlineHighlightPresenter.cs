using UnityEngine;

namespace GameCore.Visuals.Highlight
{
    /// <summary>Highlights entities using the QuickOutline-backed <see cref="EntityOutlineEffect"/>.</summary>
    public sealed class QuickOutlineHighlightPresenter : IEntityHighlightPresenter
    {
        public void SetHighlighted(Transform target, bool highlighted)
        {
            if (target == null)
                return;

            EntityOutlineEffect effect = EntityOutlineEffect.GetOrCreate(target);
            effect?.SetVisible(highlighted);
        }
    }
}

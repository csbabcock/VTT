using UnityEngine.UIElements;

namespace GameCore.UI.InGame.Services
{
    /// <summary>Safe style mutations for UI Toolkit slide animations during panel reloads.</summary>
    public static class VisualElementAnimationUtility
    {
        public static bool IsAnimatable(VisualElement element)
        {
            if (element == null)
                return false;

            try
            {
                return element.panel != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool TrySetRight(VisualElement element, float right)
        {
            if (!IsAnimatable(element))
                return false;

            try
            {
                element.style.right = right;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void TryMarkDirtyRepaint(VisualElement element)
        {
            if (!IsAnimatable(element))
                return;

            try
            {
                element.MarkDirtyRepaint();
            }
            catch
            {
            }
        }
    }
}

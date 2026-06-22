using GameCore.UI.InGame.Services;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameCore.Tests.EditMode
{
    public class VisualElementAnimationUtilityTests
    {
        [Test]
        public void IsAnimatable_ReturnsFalse_ForDetachedElement()
        {
            var element = new VisualElement();
            Assert.IsFalse(VisualElementAnimationUtility.IsAnimatable(element));
        }

        [Test]
        public void IsAnimatable_ReturnsFalse_ForNull()
        {
            Assert.IsFalse(VisualElementAnimationUtility.IsAnimatable(null));
        }

        [Test]
        public void TrySetRight_ReturnsFalse_ForDetachedElement()
        {
            var element = new VisualElement();
            Assert.IsFalse(VisualElementAnimationUtility.TrySetRight(element, 10f));
        }

        [Test]
        public void TrySetRight_ReturnsTrue_WhenElementHasPanel()
        {
            using var host = new UiToolkitTestHost();
            var root = new VisualElement();
            host.Attach(root);

            Assert.IsTrue(VisualElementAnimationUtility.IsAnimatable(root));
            Assert.IsTrue(VisualElementAnimationUtility.TrySetRight(root, 12f));
        }
    }
}

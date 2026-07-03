using GameCore.Visuals.Highlight;
using NUnit.Framework;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class HoverHighlightServiceTests
    {
        [Test]
        public void UpdateHover_HighlightsNewTargetAndClearsPrevious()
        {
            var first = new GameObject("First").transform;
            var second = new GameObject("Second").transform;
            var presenter = new RecordingHighlightPresenter();
            var service = new HoverHighlightService(presenter);

            try
            {
                service.UpdateHover(first);
                Assert.AreSame(first, service.HighlightedRoot);
                Assert.AreSame(first, presenter.LastTarget);
                Assert.IsTrue(presenter.LastHighlighted);

                service.UpdateHover(second);
                Assert.AreSame(second, service.HighlightedRoot);
                Assert.AreSame(second, presenter.LastTarget);
                Assert.IsTrue(presenter.LastHighlighted);
                Assert.AreEqual(2, presenter.HighlightCallCount);
                Assert.AreEqual(1, presenter.UnhighlightCallCount);
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }

        [Test]
        public void Clear_RemovesHighlight()
        {
            var target = new GameObject("Target").transform;
            var presenter = new RecordingHighlightPresenter();
            var service = new HoverHighlightService(presenter);

            try
            {
                service.UpdateHover(target);
                service.Clear();

                Assert.IsNull(service.HighlightedRoot);
                Assert.AreEqual(1, presenter.UnhighlightCallCount);
            }
            finally
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public void UpdateHover_NoOpWhenTargetUnchanged()
        {
            var target = new GameObject("Target").transform;
            var presenter = new RecordingHighlightPresenter();
            var service = new HoverHighlightService(presenter);

            try
            {
                service.UpdateHover(target);
                service.UpdateHover(target);

                Assert.AreEqual(1, presenter.HighlightCallCount);
                Assert.AreEqual(0, presenter.UnhighlightCallCount);
            }
            finally
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        private sealed class RecordingHighlightPresenter : IEntityHighlightPresenter
        {
            public Transform LastTarget { get; private set; }

            public bool LastHighlighted { get; private set; }

            public int HighlightCallCount { get; private set; }

            public int UnhighlightCallCount { get; private set; }

            public void SetHighlighted(Transform target, bool highlighted)
            {
                LastTarget = target;
                LastHighlighted = highlighted;

                if (highlighted)
                    HighlightCallCount++;
                else
                    UnhighlightCallCount++;
            }
        }
    }
}

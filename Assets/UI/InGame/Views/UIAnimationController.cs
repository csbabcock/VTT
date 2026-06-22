using GameCore.UI.InGame.Services;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace GameCore.UI.InGame
{
    /// <summary>
    /// Handles all UI animations for the in-game UI.
    /// Follows Single Responsibility Principle - only handles animations.
    /// </summary>
    public class UIAnimationController : MonoBehaviour
    {
        #region Constants
        private const float ANIMATION_DURATION = 0.3f;
        #endregion

        #region Private Fields
        private Coroutine _currentAnimation;
        private Coroutine _gameLogAnimation;
        #endregion

        #region Public Methods

        /// <summary>
        /// Animates a panel sliding in from the right.
        /// </summary>
        public Coroutine AnimateSlideIn(VisualElement element, float startRight, float endRight, System.Action onComplete = null)
        {
            if (!VisualElementAnimationUtility.IsAnimatable(element))
                return null;

            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            _currentAnimation = StartCoroutine(AnimateSlideInCoroutine(element, startRight, endRight, onComplete));
            return _currentAnimation;
        }

        /// <summary>
        /// Animates a panel sliding out to the right.
        /// </summary>
        public Coroutine AnimateSlideOut(VisualElement element, float startRight, float endRight, System.Action onComplete = null)
        {
            if (!VisualElementAnimationUtility.IsAnimatable(element))
                return null;

            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            _currentAnimation = StartCoroutine(AnimateSlideOutCoroutine(element, startRight, endRight, onComplete));
            return _currentAnimation;
        }

        /// <summary>
        /// Animates the game log panel sliding in.
        /// </summary>
        public Coroutine AnimateGameLogSlideIn(VisualElement element, float startRight, float endRight, System.Action onComplete = null)
        {
            if (!VisualElementAnimationUtility.IsAnimatable(element))
                return null;

            if (_gameLogAnimation != null)
            {
                StopCoroutine(_gameLogAnimation);
            }

            _gameLogAnimation = StartCoroutine(AnimateSlideInCoroutine(element, startRight, endRight, onComplete));
            return _gameLogAnimation;
        }

        /// <summary>
        /// Animates the game log panel sliding out.
        /// </summary>
        public Coroutine AnimateGameLogSlideOut(VisualElement element, float startRight, float endRight, System.Action onComplete = null)
        {
            if (!VisualElementAnimationUtility.IsAnimatable(element))
                return null;

            if (_gameLogAnimation != null)
            {
                StopCoroutine(_gameLogAnimation);
            }

            _gameLogAnimation = StartCoroutine(AnimateSlideOutCoroutine(element, startRight, endRight, onComplete));
            return _gameLogAnimation;
        }

        /// <summary>
        /// Stops the current animation.
        /// </summary>
        public void StopCurrentAnimation()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }
        }

        /// <summary>
        /// Stops the game log animation.
        /// </summary>
        public void StopGameLogAnimation()
        {
            if (_gameLogAnimation != null)
            {
                StopCoroutine(_gameLogAnimation);
                _gameLogAnimation = null;
            }
        }

        /// <summary>
        /// Stops all running panel animations. Call before discarding cached visual elements.
        /// </summary>
        public void StopAllAnimations()
        {
            StopCurrentAnimation();
            StopGameLogAnimation();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine to animate the panel sliding in.
        /// </summary>
        private IEnumerator AnimateSlideInCoroutine(VisualElement element, float startRight, float endRight, System.Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < ANIMATION_DURATION)
            {
                if (!VisualElementAnimationUtility.IsAnimatable(element))
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ANIMATION_DURATION);
                
                // Ease out cubic for smooth animation
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                float currentRight = Mathf.Lerp(startRight, endRight, t);
                if (!VisualElementAnimationUtility.TrySetRight(element, currentRight))
                    yield break;
                
                // Only mark dirty every few frames to reduce stutter
                if (elapsed % 0.016f < Time.deltaTime) // ~60fps updates
                {
                    VisualElementAnimationUtility.TryMarkDirtyRepaint(element);
                }

                yield return null;
            }

            if (!VisualElementAnimationUtility.TrySetRight(element, endRight))
                yield break;

            VisualElementAnimationUtility.TryMarkDirtyRepaint(element);
            
            onComplete?.Invoke();
        }

        /// <summary>
        /// Coroutine to animate the panel sliding out.
        /// </summary>
        private IEnumerator AnimateSlideOutCoroutine(VisualElement element, float startRight, float endRight, System.Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < ANIMATION_DURATION)
            {
                if (!VisualElementAnimationUtility.IsAnimatable(element))
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ANIMATION_DURATION);
                
                // Ease in cubic for smooth animation
                t = t * t * t;
                
                float currentRight = Mathf.Lerp(startRight, endRight, t);
                if (!VisualElementAnimationUtility.TrySetRight(element, currentRight))
                    yield break;
                
                VisualElementAnimationUtility.TryMarkDirtyRepaint(element);

                yield return null;
            }

            if (!VisualElementAnimationUtility.TrySetRight(element, endRight))
                yield break;

            VisualElementAnimationUtility.TryMarkDirtyRepaint(element);
            
            onComplete?.Invoke();
        }

        #endregion
    }
}

using GameCore.Combat.Feedback;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace GameCore.Tests.EditMode
{
    public sealed class HitAnimationFeedbackTests
    {
        [TestCase(5, 1)]
        [TestCase(0, 0)]
        [TestCase(-5, 0)]
        public void Damage_PlaysReactionOnlyForPositiveDamage(int damage, int expected)
        {
            var target = new GameObject("Target");
            try
            {
                var player = target.AddComponent<RecordingHitAnimationPlayer>();
                HitAnimationFeedback.PlayIfDamaged(target.transform, damage);
                Assert.AreEqual(expected, player.PlayCount);
            }
            finally { Object.DestroyImmediate(target); }
        }

        [Test]
        public void GameplayPrefab_HitInterruptsApproach_AndReturnsToIdle()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerArmature.prefab");
            var target = Object.Instantiate(prefab);
            try
            {
                var animator = target.GetComponent<Animator>();
                var motion = target.GetComponent<AttackMotionPresentation>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.SetBool("Grounded", true);
                animator.SetFloat("MotionSpeed", 1f);
                int attacks = 0;
                motion.Begin(target.transform.position + Vector3.forward * 2f, 0.5f, () => attacks++);
                animator.Update(0.2f);
                HitAnimationFeedback.PlayIfDamaged(target.transform, 4);
                animator.Update(0.01f);
                Assert.IsTrue(animator.GetNextAnimatorStateInfo(0).IsName("Hit"));
                typeof(AttackMotionPresentation).GetMethod("Tick",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(motion, new object[] { 0.01f });
                Assert.IsFalse(motion.IsPlaying);
                Assert.AreEqual(0, attacks);
                animator.Update(0.15f);
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Hit"));
                float clipLength = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;
                for (float elapsed = 0f; elapsed < clipLength + 0.3f; elapsed += 0.02f)
                    animator.Update(0.02f);
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle Walk Run Blend"));
            }
            finally { Object.DestroyImmediate(target); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GameplayPrefab_OneHitTriggerPlaysOnce_AndLaterHitCanPlayAgain(bool duplicateRequests)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerArmature.prefab");
            var target = Object.Instantiate(prefab);
            try
            {
                var animator = target.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.SetBool("Grounded", true);
                animator.SetFloat("MotionSpeed", 1f);
                animator.Update(0.01f);
                var controller = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;
                AnimationClip hitClip = null;
                foreach (var child in controller.layers[0].stateMachine.states)
                    if (child.state.name == "Hit") hitClip = child.state.motion as AnimationClip;
                Assert.NotNull(hitClip);
                Assert.IsFalse(hitClip.isLooping);

                HitAnimationFeedback.PlayIfHit(target.transform, true);
                if (duplicateRequests)
                {
                    // Simulate another request during entry, playback, and the outgoing blend.
                    animator.Update(0.01f);
                    Assert.IsTrue(animator.GetNextAnimatorStateInfo(0).IsName("Hit"));
                    HitAnimationFeedback.PlayIfHit(target.transform, true);
                    animator.Update(0.2f);
                    HitAnimationFeedback.PlayIfHit(target.transform, true);
                    bool leftHit = false;
                    for (float elapsed = 0f; elapsed < hitClip.length * 2f; elapsed += 0.02f)
                    {
                        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Hit")
                            && animator.IsInTransition(0))
                            HitAnimationFeedback.PlayIfHit(target.transform, true);
                        animator.Update(0.02f);
                        bool inHit = animator.GetCurrentAnimatorStateInfo(0).IsName("Hit")
                            || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Hit"));
                        Assert.IsFalse(leftHit && inHit, "A duplicate request queued a second hit reaction.");
                        if (!inHit) leftHit = true;
                    }
                    Assert.IsTrue(leftHit);
                    Assert.AreEqual(0, CountHitEntries(animator, hitClip.length * 2f));
                }
                else
                {
                    Assert.AreEqual(1, CountHitEntries(animator, hitClip.length * 3f));
                }
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle Walk Run Blend"));
                Assert.AreEqual(0, CountHitEntries(animator, hitClip.length * 2f));

                HitAnimationFeedback.PlayIfHit(target.transform, true);
                Assert.AreEqual(1, CountHitEntries(animator, hitClip.length * 3f));
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle Walk Run Blend"));
            }
            finally { Object.DestroyImmediate(target); }
        }

        private static int CountHitEntries(Animator animator, float duration)
        {
            int entries = 0;
            bool wasInHit = false;
            for (float elapsed = 0f; elapsed < duration; elapsed += 0.02f)
            {
                animator.Update(0.02f);
                bool inHit = animator.GetCurrentAnimatorStateInfo(0).IsName("Hit")
                    || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Hit"));
                if (inHit && !wasInHit)
                    entries++;
                wasInHit = inHit;
            }
            return entries;
        }
        [Test]
        public void MissingTargetOrPresentation_IsSafe()
        {
            var target = new GameObject("NoPresentation");
            try
            {
                Assert.DoesNotThrow(() => HitAnimationFeedback.PlayIfDamaged(null, 5));
                Assert.DoesNotThrow(() => HitAnimationFeedback.PlayIfDamaged(target.transform, 5));
            }
            finally { Object.DestroyImmediate(target); }
        }
    }

    public sealed class RecordingHitAnimationPlayer : MonoBehaviour, IHitAnimationPlayer
    {
        public int PlayCount { get; private set; }
        public void PlayHit() => PlayCount++;
    }
}

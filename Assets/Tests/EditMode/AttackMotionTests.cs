using GameCore.Combat.Feedback;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public class AttackMotionTests
    {
        private static void InvokeLifecycle(AttackMotionPresentation motion, string method)
        {
            typeof(AttackMotionPresentation).GetMethod(method,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(motion, null);
        }

        [Test]
        public void Offset_StopsAtBodyContact_AndPreservesHeight()
        {
            Vector3 origin = new Vector3(0f, 2f, 0f);
            Vector3 target = new Vector3(3f, 7f, 4f);
            Vector3 offset = AttackMotion.CalculateOffset(origin, target, 0.5f);
            Assert.AreEqual(4.5f, offset.magnitude, 0.0001f);
            Assert.AreEqual(0f, offset.y);
            Assert.AreEqual(0.5f, Vector3.Distance(origin + offset, new Vector3(3f, 2f, 4f)), 0.0001f);
        }

        [Test]
        public void Offset_AlreadyTouching_DoesNotPushBackward()
        {
            Assert.AreEqual(Vector3.zero, AttackMotion.CalculateOffset(Vector3.zero, Vector3.right * 0.2f, 0.5f));
            Assert.AreEqual(Vector3.zero, AttackMotion.CalculateOffset(Vector3.zero, Vector3.up, 0.5f));
        }

        [Test]
        public void Facing_LooksAtTargetWithoutPitch()
        {
            Quaternion facing = AttackMotion.FacingRotation(Vector3.zero, new Vector3(-2f, 10f, 0f), Quaternion.identity);
            Assert.Less(Vector3.Distance(Vector3.left, facing * Vector3.forward), 0.0001f);
            Assert.AreEqual(Quaternion.identity, AttackMotion.FacingRotation(Vector3.zero, Vector3.up, Quaternion.identity));
        }

        private static void Tick(AttackMotionPresentation motion, float deltaTime)
        {
            typeof(AttackMotionPresentation).GetMethod("Tick",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(motion, new object[] { deltaTime });
        }

        [TestCase(3f)]
        [TestCase(6f)]
        public void GameplayPrefab_RunsAtConfiguredSpeed_AndReturnsOnlyAfterAttackExits(float runSpeed)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerArmature.prefab");
            var actor = Object.Instantiate(prefab);
            try
            {
                var motion = actor.GetComponent<AttackMotionPresentation>();
                var animator = actor.GetComponent<Animator>();
                Transform visual = actor.transform.Find("Skeleton");
                Vector3 origin = actor.transform.position;
                Vector3 rest = visual.localPosition;
                Quaternion restRotation = visual.localRotation;
                Assert.NotNull(motion);

                actor.GetComponent<GameCore.PlayerController>().SprintSpeed = runSpeed;
                float travelDuration = 1.5f / runSpeed;
                animator.Rebind();
                int triggerCount = 0;
                Assert.IsTrue(motion.Begin(origin + Vector3.right * 2f, 0.5f, () =>
                {
                    Assert.AreEqual(1.5f, (visual.localPosition - rest).magnitude, 0.01f);
                    triggerCount++;
                }));
                animator.Update(0.01f);
                Assert.IsTrue(animator.IsInTransition(0));
                Assert.IsTrue(animator.GetNextAnimatorStateInfo(0).IsName("Combat Approach"));
                animator.Update(0.15f);
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Combat Approach"));
                Assert.Greater(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 0f);
                Tick(motion, 0.1f);
                Assert.AreEqual(0, triggerCount);
                Assert.AreEqual(runSpeed * 0.1f, (visual.localPosition - rest).magnitude, 0.001f);
                Tick(motion, travelDuration - 0.1f + 0.0001f);
                Assert.AreEqual(1, triggerCount);
                animator.Play("Attack", 0, 0.4f);
                animator.Update(0f);
                Tick(motion, 0.01f);

                Assert.IsTrue(motion.IsPlaying);
                Assert.AreEqual(origin, actor.transform.position);
                Assert.AreEqual(1.5f, (visual.localPosition - rest).magnitude, 0.01f);

                animator.Play("Attack", 0, 0.9f);
                animator.Update(0f);
                Tick(motion, 0.01f);
                Assert.AreEqual(1.5f, (visual.localPosition - rest).magnitude, 0.01f);
                Assert.IsTrue(motion.IsPlaying);
                Assert.AreEqual(1, triggerCount);

                animator.Play("Attack", 0, 1f);
                animator.Update(0.01f);
                Assert.IsTrue(animator.IsInTransition(0));
                Assert.IsTrue(animator.GetNextAnimatorStateInfo(0).IsName("Combat Return"));
                Tick(motion, 0.01f);
                Assert.AreEqual(1.5f, (visual.localPosition - rest).magnitude, 0.01f);
                animator.Update(0.15f);
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Combat Return"));
                float returnTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                animator.Update(0.05f);
                Assert.Less(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, returnTime);
                Tick(motion, travelDuration / 2f);
                Assert.Less(Quaternion.Angle(visual.rotation, Quaternion.LookRotation(Vector3.right)), 0.01f);
                Assert.That((visual.localPosition - rest).magnitude, Is.InRange(0.7f, 0.8f));
                Tick(motion, travelDuration / 2f);
                Assert.IsFalse(motion.IsPlaying);
                animator.Update(0.01f);
                Assert.IsTrue(animator.IsInTransition(0));
                Assert.IsTrue(animator.GetNextAnimatorStateInfo(0).IsName("Idle Walk Run Blend"));
                animator.Update(0.15f);
                Tick(motion, 0f);
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle Walk Run Blend"));
                Assert.Less(Quaternion.Angle(visual.rotation, Quaternion.LookRotation(Vector3.right)), 0.01f);
                Assert.IsFalse(animator.GetBool("CombatMotion"));
                Assert.AreEqual(rest, visual.localPosition);
                Assert.AreEqual(restRotation, visual.localRotation);
                Assert.AreEqual(origin, actor.transform.position);
            }
            finally { Object.DestroyImmediate(actor); }
        }

        [Test]
        public void Presentation_InterruptedOrRepeated_RestoresOriginalPose()
        {
            var root = new GameObject("Actor");
            var skeleton = new GameObject("Skeleton").transform;
            skeleton.SetParent(root.transform, false);
            skeleton.localPosition = Vector3.up;
            root.AddComponent<Animator>();
            var motion = root.AddComponent<AttackMotionPresentation>();
            try
            {
                InvokeLifecycle(motion, "Awake");
                int triggerCount = 0;
                motion.Begin(Vector3.forward * 2f, 0.5f, () => triggerCount++);
                skeleton.localPosition += Vector3.forward;
                motion.Begin(Vector3.right * 2f, 0.5f);
                Assert.AreEqual(Vector3.up, skeleton.localPosition);
                skeleton.localPosition += Vector3.right;
                InvokeLifecycle(motion, "OnDisable");
                Tick(motion, 1f);
                Assert.AreEqual(0, triggerCount);
                Assert.IsFalse(motion.IsPlaying);
                Assert.AreEqual(Vector3.up, skeleton.localPosition);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}

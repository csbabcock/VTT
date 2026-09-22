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

        [TestCase(0f, 0f)]
        [TestCase(0.3f, 1f)]
        [TestCase(0.5f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(2f, 0f)]
        public void LungeWeight_ReachesContactAndReturns(float time, float expected)
        {
            Assert.AreEqual(expected, AttackMotion.LungeWeight(time), 0.0001f);
        }

        [Test]
        public void LungeWeight_ApproachAndReturnAreGradual()
        {
            Assert.That(AttackMotion.LungeWeight(0.15f), Is.InRange(0.4f, 0.6f));
            Assert.That(AttackMotion.LungeWeight(0.775f), Is.InRange(0.4f, 0.6f));
        }

        [Test]
        public void Facing_LooksAtTargetWithoutPitch()
        {
            Quaternion facing = AttackMotion.FacingRotation(Vector3.zero, new Vector3(-2f, 10f, 0f), Quaternion.identity);
            Assert.Less(Vector3.Distance(Vector3.left, facing * Vector3.forward), 0.0001f);
            Assert.AreEqual(Quaternion.identity, AttackMotion.FacingRotation(Vector3.zero, Vector3.up, Quaternion.identity));
        }

        [Test]
        public void GameplayPrefab_LungesVisualOnly_AndRestoresOnCompletion()
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

                animator.Rebind();
                motion.Begin(origin + Vector3.forward * 2f, 0.5f);
                animator.Play("Attack", 0, 0.4f);
                animator.Update(0f);
                InvokeLifecycle(motion, "LateUpdate");

                Assert.IsTrue(motion.IsPlaying);
                Assert.AreEqual(origin, actor.transform.position);
                Assert.AreEqual(1.5f, (visual.localPosition - rest).magnitude, 0.01f);

                animator.Play("Attack", 0, 1f);
                animator.Update(0f);
                InvokeLifecycle(motion, "LateUpdate");
                Assert.Less(Vector3.Distance(rest, visual.localPosition), 0.001f);

                motion.Restore();
                Assert.IsFalse(motion.IsPlaying);
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
                motion.Begin(Vector3.forward * 2f, 0.5f);
                skeleton.localPosition += Vector3.forward;
                motion.Begin(Vector3.right * 2f, 0.5f);
                Assert.AreEqual(Vector3.up, skeleton.localPosition);
                skeleton.localPosition += Vector3.right;
                InvokeLifecycle(motion, "OnDisable");
                Assert.IsFalse(motion.IsPlaying);
                Assert.AreEqual(Vector3.up, skeleton.localPosition);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}

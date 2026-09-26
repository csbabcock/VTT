using GameCore.Combat.Feedback;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public sealed class CombatTextPopupTests
    {
        private static void TickAttack(AttackMotionPresentation motion, float deltaTime)
        {
            typeof(AttackMotionPresentation).GetMethod("Tick",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(motion, new object[] { deltaTime });
        }

        [TestCase(false, 0)]
        [TestCase(true, 0)]
        [TestCase(true, 7)]
        [TestCase(true, 23)]
        public void Impact_ShowsOneOutcomePopup_WithTotalDamage(bool didHit, int damageAmount)
        {
            var attacker = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerArmature.prefab"));
            var target = new GameObject("Target");
            target.transform.position = Vector3.forward * 2f;
            var reaction = target.AddComponent<RecordingHitAnimationPlayer>();
            try
            {
                var animator = attacker.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                var motion = attacker.GetComponent<AttackMotionPresentation>();
                attacker.GetComponent<IAttackAnimationPlayer>().PlayAttack(target.transform, didHit, damageAmount);
                Assert.IsEmpty(Object.FindObjectsByType<CombatTextPopup>(FindObjectsSortMode.None));
                TickAttack(motion, 1f);
                animator.ResetTrigger("Attack");
                animator.Play("Attack", 0, 0.2f);
                animator.Update(0f);
                TickAttack(motion, 0.01f);
                Assert.IsEmpty(Object.FindObjectsByType<CombatTextPopup>(FindObjectsSortMode.None));
                Assert.AreEqual(0, reaction.PlayCount);

                animator.Play("Attack", 0, 0.4f);
                animator.Update(0f);
                TickAttack(motion, 0.01f);
                TickAttack(motion, 0.01f);
                var popups = Object.FindObjectsByType<CombatTextPopup>(FindObjectsSortMode.None);
                Assert.AreEqual(1, popups.Length);
                Assert.AreEqual(didHit ? 1 : 0, reaction.PlayCount);
                {
                    Assert.AreEqual(didHit ? $"{damageAmount}" : "Miss", popups[0].GetComponent<TextMeshPro>().text);
                    Assert.Greater(popups[0].transform.position.y, target.transform.position.y);
                    Assert.AreEqual(target.transform.position.x, popups[0].transform.position.x);
                    Assert.AreEqual(target.transform.position.z, popups[0].transform.position.z);
                }
            }
            finally
            {
                Object.DestroyImmediate(attacker);
                Object.DestroyImmediate(target);
                foreach (var popup in Object.FindObjectsByType<CombatTextPopup>(FindObjectsSortMode.None))
                    Object.DestroyImmediate(popup.gameObject);
            }
        }

        [Test]
        public void Popup_RisesFacesCameraFadesAndExpires()
        {
            var cameraObject = new GameObject("Popup Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(15f, 70f, 0f);
            Vector3 origin = new Vector3(3f, 2f, 5f);
            var popup = CombatTextPopup.ShowAt(origin);
            try
            {
                var advance = typeof(CombatTextPopup).GetMethod("Advance",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsFalse((bool)advance.Invoke(popup, new object[] { 0.6f, camera }));
                Assert.Greater(popup.transform.position.y, origin.y);
                Assert.AreEqual(origin.x, popup.transform.position.x);
                Assert.Less(Quaternion.Angle(camera.transform.rotation, popup.transform.rotation), 0.01f);
                Assert.That(popup.GetComponent<TextMeshPro>().alpha, Is.InRange(0.01f, 0.99f));
                Assert.IsTrue((bool)advance.Invoke(popup, new object[] { 0.6f, camera }));
                Assert.AreEqual(0f, popup.GetComponent<TextMeshPro>().alpha);
                Assert.AreEqual(2, popup.gameObject.layer);
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}

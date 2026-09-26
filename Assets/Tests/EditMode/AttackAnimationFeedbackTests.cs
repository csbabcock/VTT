using GameCore.Combat.Feedback;
using GameCore.Combat.Models;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GameCore.Tests.EditMode
{
    public sealed class AttackAnimationFeedbackTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void CompletedAttack_PlaysOnAttacker_ForHitAndMiss(bool hit)
        {
            var root = new GameObject("Attacker");
            try
            {
                var player = root.AddComponent<RecordingAttackAnimationPlayer>();
                var outcome = hit
                    ? AttackOutcome.Hit(4, false, 15, 20, 10)
                    : AttackOutcome.Miss(1, 6, 10);

                AttackAnimationFeedback.PlayIfCompleted(
                    root.transform, CombatActionResult.Completed(outcome, "Attacker", "Target", "Unarmed"), root.transform);

                Assert.AreEqual(1, player.PlayCount);
                Assert.AreSame(root.transform, player.LastTarget);
                Assert.AreEqual(hit, player.LastDidHit);
                Assert.AreEqual(hit ? 4 : 0, player.LastDamageAmount);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(CombatFailureReason.OutOfRange)]
        [TestCase(CombatFailureReason.NotYourTurn)]
        [TestCase(CombatFailureReason.ActionAlreadyUsed)]
        [TestCase(CombatFailureReason.TargetDestroyed)]
        public void RejectedAttack_DoesNotPlay(CombatFailureReason reason)
        {
            var root = new GameObject("Attacker");
            try
            {
                var player = root.AddComponent<RecordingAttackAnimationPlayer>();
                AttackAnimationFeedback.PlayIfCompleted(
                    root.transform, CombatActionResult.Failed(reason, "Attacker", "Target", "Unarmed"));
                Assert.AreEqual(0, player.PlayCount);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void MissingPresentation_DoesNotPreventCombatCompletion()
        {
            var root = new GameObject("ActorWithoutVisuals");
            var result = CombatActionResult.Completed(AttackOutcome.Miss(1, 1, 10), "A", "B", "Unarmed");
            try
            {
                Assert.DoesNotThrow(() => AttackAnimationFeedback.PlayIfCompleted(root.transform, result));
                Assert.DoesNotThrow(() => AttackAnimationFeedback.PlayIfCompleted(null, result));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void EncounterIdle_BlendsOnEntry_RestoresDefaultOnExit_AndPreservesRunning()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerArmature.prefab");
            var actor = Object.Instantiate(prefab);
            try
            {
                var animator = actor.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                var handler = new GameCore.AnimationHandler(animator);
                handler.Initialize();
                handler.UpdateAnimations(0f, 1f, true, false, false);
                animator.Update(0.01f);
                AssertIdleClip(animator, "Assets/Animations/Base/Stand--Idle.anim.fbx");

                handler.UpdateEncounterMode(true, 0.075f);
                Assert.AreEqual(0.5f, animator.GetFloat("EncounterIdle"), 0.001f);
                handler.UpdateEncounterMode(true, 0.075f);
                animator.Update(0.01f);
                AssertIdleClip(animator, "Assets/Animations/Combat/Idle.fbx");

                handler.UpdateAnimations(6f, 1f, true, false, false);
                animator.Update(0.01f);
                AssertIdleClip(animator, "Assets/Animations/Base/Locomotion--Run_N.anim.fbx");

                handler.UpdateAnimations(0f, 1f, true, false, false);
                handler.UpdateEncounterMode(false, 0.15f);
                animator.Update(0.01f);
                AssertIdleClip(animator, "Assets/Animations/Base/Stand--Idle.anim.fbx");
                Assert.AreEqual(0f, animator.GetFloat("EncounterIdle"));
            }
            finally { Object.DestroyImmediate(actor); }
        }

        private static void AssertIdleClip(Animator animator, string path)
        {
            Assert.That(animator.GetCurrentAnimatorClipInfo(0), Has.Some.Matches<AnimatorClipInfo>(
                info => info.weight > 0.99f && AssetDatabase.GetAssetPath(info.clip) == path));
        }

        [Test]
        public void GameplayController_AttackUsesImportedClip_AndReturnsToLocomotion()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/ThirdPerson.controller");
            Assert.NotNull(controller);
            Assert.That(controller.parameters, Has.Some.Matches<AnimatorControllerParameter>(
                p => p.name == AttackAnimationFeedback.TriggerName && p.type == AnimatorControllerParameterType.Trigger));
            var machine = controller.layers[0].stateMachine;
            Assert.AreEqual("Idle Walk Run Blend", machine.defaultState.name);
            var locomotion = machine.defaultState.motion as BlendTree;
            Assert.NotNull(locomotion);
            var idle = locomotion.children[0].motion as BlendTree;
            Assert.NotNull(idle);
            Assert.AreEqual("EncounterIdle", idle.blendParameter);
            Assert.AreEqual("Assets/Animations/Base/Stand--Idle.anim.fbx",
                AssetDatabase.GetAssetPath(idle.children[0].motion));
            Assert.AreEqual("Assets/Animations/Combat/Idle.fbx",
                AssetDatabase.GetAssetPath(idle.children[1].motion));
            Assert.IsTrue(((AnimationClip)idle.children[1].motion).isLooping);
            AnimatorState hit = null;
            foreach (var child in machine.states)
                if (child.state.name == "Hit") hit = child.state;
            Assert.NotNull(hit);
            Assert.AreEqual("Assets/Animations/Combat/Hit_Unarmed.fbx", AssetDatabase.GetAssetPath(hit.motion));
            Assert.AreEqual(1f, hit.speed);
            Assert.AreSame(machine.defaultState, hit.transitions[0].destinationState);
            Assert.IsTrue(hit.transitions[0].hasExitTime);
            Assert.AreEqual(1f, hit.transitions[0].exitTime);
            Assert.That(machine.anyStateTransitions, Has.Some.Matches<AnimatorStateTransition>(
                t => t.destinationState == hit && t.conditions.Length == 1
                    && t.conditions[0].parameter == HitAnimationFeedback.TriggerName));
            foreach (string path in new[] {
                "Assets/Animations/Combat/Idle.fbx",
                "Assets/Animations/Combat/Unarmed_Strike.fbx",
                "Assets/Animations/Combat/Hit_Unarmed.fbx" })
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                Assert.AreEqual(ModelImporterAnimationType.Human, importer.animationType, path);
            }
            Assert.IsFalse(((AnimationClip)hit.motion).isLooping);

            AnimatorState approach = null;
            AnimatorState retreat = null;
            foreach (var child in machine.states)
            {
                if (child.state.name == "Combat Approach") approach = child.state;
                if (child.state.name == "Combat Return") retreat = child.state;
            }
            Assert.NotNull(approach);
            Assert.NotNull(retreat);
            Assert.AreSame(approach.motion, retreat.motion);
            Assert.AreEqual("Assets/Animations/Base/Locomotion--Run_N.anim.fbx", AssetDatabase.GetAssetPath(approach.motion));
            Assert.AreEqual(1f, approach.speed);
            Assert.AreEqual(-1f, retreat.speed);
            Assert.IsFalse(approach.speedParameterActive);
            Assert.IsFalse(retreat.speedParameterActive);
            Assert.IsEmpty(approach.transitions);
            Assert.IsEmpty(retreat.transitions);

            AnimatorState attack = null;
            foreach (var child in machine.states)
                if (child.state.name == "Attack") attack = child.state;
            Assert.NotNull(attack);
            Assert.AreEqual("Assets/Animations/Combat/Unarmed_Strike.fbx", AssetDatabase.GetAssetPath(attack.motion));
            Assert.IsFalse(attack.speedParameterActive);
            Assert.AreEqual(1f, attack.speed);
            Assert.IsFalse(((AnimationClip)attack.motion).isLooping);
            Assert.AreEqual(2, attack.transitions.Length);
            var exit = attack.transitions[0];
            Assert.AreSame(retreat, exit.destinationState);
            Assert.AreEqual(0.12f, exit.duration, 0.001f);
            Assert.IsTrue(exit.hasFixedDuration);
            Assert.AreEqual(1f, exit.offset);
            Assert.IsTrue(exit.hasExitTime);
            Assert.AreEqual(1f, exit.exitTime);
            Assert.AreEqual(1, exit.conditions.Length);
            Assert.AreEqual("CombatMotion", exit.conditions[0].parameter);
            Assert.AreEqual(AnimatorConditionMode.If, exit.conditions[0].mode);
            var idleExit = attack.transitions[1];
            Assert.AreSame(machine.defaultState, idleExit.destinationState);
            Assert.IsTrue(idleExit.hasExitTime);
            Assert.AreEqual(1f, idleExit.exitTime);
            Assert.AreEqual("CombatMotion", idleExit.conditions[0].parameter);
            Assert.AreEqual(AnimatorConditionMode.IfNot, idleExit.conditions[0].mode);
            Assert.That(machine.anyStateTransitions, Has.Some.Matches<AnimatorStateTransition>(
                t => t.destinationState == attack && !t.hasExitTime &&
                     t.conditions.Length == 1 && t.conditions[0].parameter == AttackAnimationFeedback.TriggerName));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerArmature.prefab");
            Assert.AreSame(controller, prefab.GetComponent<Animator>().runtimeAnimatorController);
            Assert.NotNull(prefab.GetComponent<IAttackAnimationPlayer>());
        }
    }

    public sealed class RecordingAttackAnimationPlayer : MonoBehaviour, IAttackAnimationPlayer
    {
        public int PlayCount { get; private set; }
        public Transform LastTarget { get; private set; }
        public bool LastDidHit { get; private set; }
        public int LastDamageAmount { get; private set; }
        public void PlayAttack(Transform target, bool didHit = false, int damageAmount = 0)
        {
            LastTarget = target;
            LastDidHit = didHit;
            LastDamageAmount = damageAmount;
            PlayCount++;
        }
    }
}

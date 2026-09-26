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
        public void GameplayController_AttackUsesImportedClip_AndReturnsToLocomotion()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/ThirdPerson.controller");
            Assert.NotNull(controller);
            Assert.That(controller.parameters, Has.Some.Matches<AnimatorControllerParameter>(
                p => p.name == AttackAnimationFeedback.TriggerName && p.type == AnimatorControllerParameterType.Trigger));
            var machine = controller.layers[0].stateMachine;
            Assert.AreEqual("Idle Walk Run Blend", machine.defaultState.name);
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
            Assert.AreEqual("Assets/Animations/Standing Melee Attack Downward.fbx", AssetDatabase.GetAssetPath(attack.motion));
            Assert.IsFalse(attack.speedParameterActive);
            Assert.AreEqual(2f, attack.speed);
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
        public void PlayAttack(Transform target)
        {
            LastTarget = target;
            PlayCount++;
        }
    }
}

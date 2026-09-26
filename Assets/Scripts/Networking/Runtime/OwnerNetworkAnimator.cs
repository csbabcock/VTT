using GameCore.Combat.Feedback;
using GameCore.Combat.Targeting;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>Owner-driven attack animation and replicated visual melee motion.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AttackMotionPresentation))]
    public class OwnerNetworkAnimator : NetworkAnimator, IAttackAnimationPlayer
    {
        protected override bool OnIsServerAuthoritative() => false;

        public void PlayAttack(Transform target)
        {
            if (Animator == null || !Animator.isActiveAndEnabled || (IsSpawned && !IsOwner))
                return;

            if (target != null)
            {
                // Visual body contact; the gameplay colliders stay in their grid cells.
                float contactDistance = MeleeStandoff.GetBodyRadius(transform)
                    + MeleeStandoff.GetBodyRadius(target) + 0.02f;
                if (GetComponent<AttackMotionPresentation>().Begin(target.position, contactDistance, TriggerAttack))
                {
                    if (IsSpawned)
                        PresentAttackMotionRpc(target.position, contactDistance);
                    return;
                }
            }

            TriggerAttack();
        }

        private void TriggerAttack()
        {
            if (IsSpawned)
                SetTrigger(AttackAnimationFeedback.TriggerName);
            else
                Animator.SetTrigger(AttackAnimationFeedback.TriggerName);
        }

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Owner)]
        private void PresentAttackMotionRpc(Vector3 targetPosition, float contactDistance)
        {
            GetComponent<AttackMotionPresentation>().Begin(targetPosition, contactDistance);
        }

        public override void OnNetworkDespawn()
        {
            GetComponent<AttackMotionPresentation>().Restore();
            base.OnNetworkDespawn();
        }
    }
}

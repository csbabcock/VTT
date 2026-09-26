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
    public class OwnerNetworkAnimator : NetworkAnimator, IAttackAnimationPlayer, IHitAnimationPlayer
    {
        protected override bool OnIsServerAuthoritative() => false;

        public void PlayAttack(Transform target, bool didHit = false, int damageAmount = 0)
        {
            if (Animator == null || !Animator.isActiveAndEnabled || (IsSpawned && !IsOwner))
                return;

            if (target != null)
            {
                // Visual body contact; the gameplay colliders stay in their grid cells.
                float contactDistance = MeleeStandoff.GetBodyRadius(transform)
                    + MeleeStandoff.GetBodyRadius(target) + 0.02f;
                if (GetComponent<AttackMotionPresentation>().Begin(target.position, contactDistance, TriggerAttack,
                    () => PresentImpact(target, didHit, damageAmount)))
                {
                    if (IsSpawned)
                        PresentAttackMotionRpc(target.position, contactDistance);
                    return;
                }
            }

            TriggerAttack();
            PresentImpact(target, didHit, damageAmount);
        }

        private void PresentImpact(Transform target, bool didHit, int damageAmount)
        {
            if (target == null)
                return;

            HitAnimationFeedback.PlayIfHit(target, didHit);

            Vector3 position = CombatTextPopup.GetPositionAbove(target);
            CombatTextPopup.ShowAt(position, didHit, damageAmount);
            if (IsSpawned)
                PresentOutcomeRpc(position, didHit, damageAmount);
        }

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Owner)]
        private void PresentOutcomeRpc(Vector3 position, bool didHit, int damageAmount)
        {
            CombatTextPopup.ShowAt(position, didHit, damageAmount);
        }

        public void PlayHit()
        {
            // A successful attacker can own a different NetworkObject than this target.
            // Route cosmetic feedback to the target owner, then use normal Animator replication.
            if (IsSpawned && !IsOwner)
            {
                RequestHitReactionRpc();
                return;
            }

            if (Animator == null || !Animator.isActiveAndEnabled)
                return;

            // A trigger echoed by NetworkAnimator can remain pending while Hit is active,
            // then replay after its exit. Replicate a direct crossfade as state only.
            if (Animator.GetCurrentAnimatorStateInfo(0).IsName(HitAnimationFeedback.TriggerName)
                || (Animator.IsInTransition(0)
                    && Animator.GetNextAnimatorStateInfo(0).IsName(HitAnimationFeedback.TriggerName)))
                return;

            Animator.ResetTrigger(HitAnimationFeedback.TriggerName);
            Animator.CrossFadeInFixedTime(
                Animator.StringToHash("Base Layer." + HitAnimationFeedback.TriggerName), 0.08f, 0, 0f);
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestHitReactionRpc()
        {
            PlayHit();
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

using GameCore.Combat.Feedback;
using Unity.Netcode.Components;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Owner-authoritative <see cref="NetworkAnimator"/>. The default NetworkAnimator is
    /// server-authoritative, so animation parameters driven locally by the owning client's
    /// PlayerController would never replicate. This matches our owner-authoritative
    /// NetworkTransform: the controlling client drives the Animator and changes are
    /// replicated to the server and all other clients.
    ///
    /// Add this to the player prefab (instead of the stock NetworkAnimator) and assign the
    /// player's Animator to its Animator field.
    /// </summary>
    [DisallowMultipleComponent]
    public class OwnerNetworkAnimator : NetworkAnimator, IAttackAnimationPlayer
    {
        protected override bool OnIsServerAuthoritative() => false;

        public void PlayAttack()
        {
            if (Animator == null || !Animator.isActiveAndEnabled)
                return;

            if (IsSpawned)
            {
                // Triggers must go through Netcode to reach the other clients.
                if (IsOwner)
                    SetTrigger(AttackAnimationFeedback.TriggerName);
            }
            else
            {
                // Direct-scene/offline play has no spawned NetworkObject.
                Animator.SetTrigger(AttackAnimationFeedback.TriggerName);
            }
        }
    }
}

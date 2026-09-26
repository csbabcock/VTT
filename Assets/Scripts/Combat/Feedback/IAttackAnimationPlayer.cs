using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>Plays an attack visual without owning attack rules or damage.</summary>
    public interface IAttackAnimationPlayer
    {
        void PlayAttack(Transform target, bool didHit = false, int damageAmount = 0);
    }
}

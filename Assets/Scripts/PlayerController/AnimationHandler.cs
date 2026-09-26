using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Handles animation updates (Single Responsibility)
    /// </summary>
    public class AnimationHandler : IAnimationHandler
    {
        private readonly Animator _animator;
        private readonly bool _hasAnimator;

        private static readonly int EncounterIdle = Animator.StringToHash("EncounterIdle");
        private bool _hasEncounterIdle;
        private const float IdleBlendDuration = 0.15f;
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        public AnimationHandler(Animator animator)
        {
            _animator = animator;
            _hasAnimator = animator != null;
        }

        public void Initialize()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            if (_hasAnimator && _animator.runtimeAnimatorController != null)
            {
                foreach (var parameter in _animator.parameters)
                    if (parameter.nameHash == EncounterIdle && parameter.type == AnimatorControllerParameterType.Float)
                        _hasEncounterIdle = true;
            }
        }

        public void UpdateEncounterMode(bool isEncounterActive, float deltaTime)
        {
            if (!_hasAnimator || !_hasEncounterIdle)
                return;

            float weight = Mathf.MoveTowards(_animator.GetFloat(EncounterIdle),
                isEncounterActive ? 1f : 0f, Mathf.Max(0f, deltaTime) / IdleBlendDuration);
            _animator.SetFloat(EncounterIdle, weight);
        }

        public void UpdateAnimations(float speed, float motionSpeed, bool isGrounded, bool isJumping, bool isFalling)
        {
            if (!_hasAnimator) return;

            _animator.SetFloat(_animIDSpeed, speed);
            _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
            _animator.SetBool(_animIDGrounded, isGrounded);
            _animator.SetBool(_animIDJump, isJumping);
            _animator.SetBool(_animIDFreeFall, isFalling);
        }
    }
}


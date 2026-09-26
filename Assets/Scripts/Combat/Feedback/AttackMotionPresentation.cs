using UnityEngine;

namespace GameCore.Combat.Feedback
{
    /// <summary>
    /// Offsets the skeleton after animation evaluation, leaving the actor, collider,
    /// camera anchor and authoritative grid position in place.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class AttackMotionPresentation : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Animator _animator;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Quaternion _startRotation;
        private Quaternion _facingRotation;
        private Vector3 _offset;
        private Vector3 _returnPosition;
        private const string ApproachState = "Combat Approach";
        private const string ReturnState = "Combat Return";
        private const string LocomotionState = "Idle Walk Run Blend";
        private const string MotionParameter = "CombatMotion";
        private const float BlendDuration = 0.12f;
        private const float DefaultRunSpeed = 5.335f;
        private float _travelDuration;
        private float _elapsed;
        private bool _approaching;
        private System.Action _onReachedTarget;
        private System.Action _onImpact;
        private const float ImpactNormalizedTime = 0.35f;
        private float _returnElapsed;
        private bool _enteredAttack;
        private bool _returning;
        private bool _capturedRest;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_visualRoot == null)
                _visualRoot = transform.Find("Skeleton");
            CaptureRestPose();
        }

        private void CaptureRestPose()
        {
            if (_capturedRest || _visualRoot == null || _visualRoot == transform)
                return;
            _restPosition = _visualRoot.localPosition;
            _restRotation = _visualRoot.localRotation;
            _capturedRest = true;
        }

        public bool Begin(Vector3 targetPosition, float contactDistance, System.Action onReachedTarget = null, System.Action onImpact = null)
        {
            CaptureRestPose();
            if (!_capturedRest || _animator == null || !isActiveAndEnabled)
                return false;

            Restore();
            _startRotation = _visualRoot.rotation;
            _facingRotation = AttackMotion.FacingRotation(transform.position, targetPosition, _startRotation);
            _offset = AttackMotion.CalculateOffset(transform.position, targetPosition, contactDistance);
            var playerController = GetComponent<GameCore.PlayerController>();
            float runSpeed = playerController != null ? playerController.SprintSpeed : DefaultRunSpeed;
            // Match normal running in both directions, independently of attack playback speed.
            _travelDuration = Mathf.Max(0.0001f, _offset.magnitude / Mathf.Max(0.01f, runSpeed));
            _elapsed = 0f;
            _approaching = true;
            _onReachedTarget = onReachedTarget;
            _onImpact = onImpact;
            _enteredAttack = false;
            _returning = false;
            IsPlaying = true;
            SetCombatMotion(true);
            if (_offset.sqrMagnitude > 0.0001f)
                BlendToState(ApproachState);
            playerController?.CancelEncounterGridMovement();
            return true;
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        private void Tick(float deltaTime)
        {
            if (!IsPlaying)
                return;
            if (_visualRoot == null || _animator == null || !_animator.isActiveAndEnabled)
            {
                Restore();
                return;
            }

            // Let a hit interrupt presentation without blending over the reaction.
            bool reacting = _animator.GetCurrentAnimatorStateInfo(0).IsName(HitAnimationFeedback.TriggerName)
                || (_animator.IsInTransition(0)
                    && _animator.GetNextAnimatorStateInfo(0).IsName(HitAnimationFeedback.TriggerName));
            if (reacting)
            {
                Restore(false);
                return;
            }

            if (_returning)
            {
                _returnElapsed += deltaTime;
                float weight = Mathf.Clamp01(_returnElapsed / _travelDuration);
                _visualRoot.localPosition = Vector3.Lerp(_returnPosition, _restPosition, weight);
                _visualRoot.rotation = _facingRotation;
                if (_returnElapsed >= _travelDuration)
                    CompleteReturn();
                return;
            }

            _elapsed += deltaTime;
            Vector3 localOffset = _visualRoot.parent.InverseTransformVector(_offset);
            if (_approaching)
            {
                float weight = Mathf.Clamp01(_elapsed / _travelDuration);
                _visualRoot.localPosition = _restPosition + localOffset * weight;
                _visualRoot.rotation = Quaternion.Slerp(_startRotation, _facingRotation, weight);
                if (_elapsed >= _travelDuration)
                {
                    _approaching = false;
                    _elapsed = 0f;
                    var onReachedTarget = _onReachedTarget;
                    _onReachedTarget = null;
                    onReachedTarget?.Invoke();
                }
                return;
            }

            // Stay at contact until the full attack clip has finished.
            _visualRoot.localPosition = _restPosition + localOffset;
            _visualRoot.rotation = _facingRotation;
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            bool inAttack = state.IsName(AttackAnimationFeedback.TriggerName);
            bool blendingToReturn = false;
            if (_animator.IsInTransition(0))
            {
                AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
                blendingToReturn = next.IsName(ReturnState);
                if (next.IsName(AttackAnimationFeedback.TriggerName))
                {
                    state = next;
                    inAttack = true;
                }
            }

            if (inAttack)
            {
                _enteredAttack = true;
                if (state.normalizedTime >= ImpactNormalizedTime)
                {
                    var onImpact = _onImpact;
                    _onImpact = null;
                    onImpact?.Invoke();
                }
            }

            // Also recover from interruption, a missing trigger/state, or despawn/disable.
            bool attackFinished = inAttack && state.normalizedTime >= 1f && blendingToReturn;
            if (attackFinished || (_enteredAttack && !inAttack) || (!_enteredAttack && _elapsed > 1f))
            {
                _returnPosition = _visualRoot.localPosition;
                _returnElapsed = 0f;
                _returning = true;
                if (_offset.sqrMagnitude > 0.0001f && !blendingToReturn && !state.IsName(ReturnState))
                    BlendToState(ReturnState);
            }
        }

        private void BlendToState(string stateName)
        {
            if (_animator == null || !_animator.isActiveAndEnabled ||
                _animator.runtimeAnimatorController == null)
                return;

            int stateHash = Animator.StringToHash("Base Layer." + stateName);
            if (_animator.HasState(0, stateHash))
                _animator.CrossFadeInFixedTime(stateHash, BlendDuration, 0, 0f);
        }

        private void SetCombatMotion(bool active)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;
            foreach (var parameter in _animator.parameters)
            {
                if (parameter.name == MotionParameter && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    _animator.SetBool(MotionParameter, active);
                    return;
                }
            }
        }

        private void CompleteReturn()
        {
            // Transfer the visual facing to the actor before removing the skeleton offset.
            // This prevents snapping back to the pre-attack orientation at arrival.
            Quaternion restingWorldRotation = _visualRoot.parent.rotation * _restRotation;
            transform.rotation = (_facingRotation * Quaternion.Inverse(restingWorldRotation)) * transform.rotation;
            Restore();
        }

        public void Restore(bool blendToLocomotion = true)
        {
            SetCombatMotion(false);
            if (IsPlaying && blendToLocomotion)
                BlendToState(LocomotionState);
            if (_capturedRest && _visualRoot != null)
            {
                _visualRoot.localPosition = _restPosition;
                _visualRoot.localRotation = _restRotation;
            }
            IsPlaying = false;
            _approaching = false;
            _onReachedTarget = null;
            _returning = false;
            _onImpact = null;
        }

        private void OnDisable() => Restore();
    }
}

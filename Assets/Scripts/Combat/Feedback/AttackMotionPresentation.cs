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
        private Quaternion _returnRotation;
        private float _elapsed;
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

        public void Begin(Vector3 targetPosition, float contactDistance)
        {
            CaptureRestPose();
            if (!_capturedRest || _animator == null || !isActiveAndEnabled)
                return;

            Restore();
            _startRotation = _visualRoot.rotation;
            _facingRotation = AttackMotion.FacingRotation(transform.position, targetPosition, _startRotation);
            _offset = AttackMotion.CalculateOffset(transform.position, targetPosition, contactDistance);
            _elapsed = 0f;
            _enteredAttack = false;
            _returning = false;
            IsPlaying = true;
            GetComponent<GameCore.PlayerController>()?.CancelEncounterGridMovement();
        }

        private void LateUpdate()
        {
            if (!IsPlaying)
                return;
            if (_visualRoot == null || _animator == null || !_animator.isActiveAndEnabled)
            {
                Restore();
                return;
            }

            if (_returning)
            {
                _returnElapsed += Time.deltaTime;
                float weight = Mathf.SmoothStep(0f, 1f, _returnElapsed / 0.15f);
                _visualRoot.localPosition = Vector3.Lerp(_returnPosition, _restPosition, weight);
                _visualRoot.localRotation = Quaternion.Slerp(_returnRotation, _restRotation, weight);
                if (_returnElapsed >= 0.15f)
                    Restore();
                return;
            }

            _elapsed += Time.deltaTime;
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            bool inAttack = state.IsName(AttackAnimationFeedback.TriggerName);
            if (_animator.IsInTransition(0))
            {
                AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
                if (next.IsName(AttackAnimationFeedback.TriggerName))
                {
                    state = next;
                    inAttack = true;
                }
            }

            if (inAttack)
            {
                _enteredAttack = true;
                Vector3 localOffset = _visualRoot.parent.InverseTransformVector(_offset);
                _visualRoot.localPosition = _restPosition + localOffset * AttackMotion.LungeWeight(state.normalizedTime);
                _visualRoot.rotation = Quaternion.Slerp(_startRotation, _facingRotation,
                    Mathf.SmoothStep(0f, 1f, _elapsed / 0.12f));
            }

            // Also recover from interruption, a missing trigger/state, or despawn/disable.
            if ((_enteredAttack && !inAttack) || (!_enteredAttack && _elapsed > 1f))
            {
                _returnPosition = _visualRoot.localPosition;
                _returnRotation = _visualRoot.localRotation;
                _returnElapsed = 0f;
                _returning = true;
            }
        }

        public void Restore()
        {
            if (_capturedRest && _visualRoot != null)
            {
                _visualRoot.localPosition = _restPosition;
                _visualRoot.localRotation = _restRotation;
            }
            IsPlaying = false;
            _returning = false;
        }

        private void OnDisable() => Restore();
    }
}

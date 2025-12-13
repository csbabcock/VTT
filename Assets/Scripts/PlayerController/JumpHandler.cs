namespace GameCore
{
    /// <summary>
    /// Handles jump and gravity logic (Single Responsibility)
    /// </summary>
    public class JumpHandler : IJumpHandler
    {
        private readonly float _jumpHeight;
        private readonly float _gravity;
        private readonly float _jumpTimeout;
        private readonly float _fallTimeout;
        private readonly float _terminalVelocity;

        private float _verticalVelocity;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        public float VerticalVelocity => _verticalVelocity;

        public JumpHandler(float jumpHeight, float gravity, float jumpTimeout, float fallTimeout, float terminalVelocity = 53.0f)
        {
            _jumpHeight = jumpHeight;
            _gravity = gravity;
            _jumpTimeout = jumpTimeout;
            _fallTimeout = fallTimeout;
            _terminalVelocity = terminalVelocity;
            
            _jumpTimeoutDelta = _jumpTimeout;
            _fallTimeoutDelta = _fallTimeout;
        }

        public void ProcessJump(bool jumpInput, bool isGrounded)
        {
            if (isGrounded)
            {
                _fallTimeoutDelta = _fallTimeout;

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (jumpInput && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = UnityEngine.Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= UnityEngine.Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = _jumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= UnityEngine.Time.deltaTime;
                }
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += _gravity * UnityEngine.Time.deltaTime;
            }
        }

        public bool IsJumping => _jumpTimeoutDelta <= 0.0f && _verticalVelocity > 0.0f;
        public bool IsFalling => _fallTimeoutDelta <= 0.0f && !IsJumping;
    }
}


using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Handles grounded checking logic (Single Responsibility)
    /// </summary>
    public class GroundedChecker : IGroundedChecker
    {
        private readonly Transform _transform;
        private readonly LayerMask _groundLayers;
        private readonly float _groundedOffset;
        private readonly float _groundedRadius;

        public bool IsGrounded { get; private set; }

        public GroundedChecker(Transform transform, float groundedOffset, float groundedRadius, LayerMask groundLayers)
        {
            _transform = transform;
            _groundedOffset = groundedOffset;
            _groundedRadius = groundedRadius;
            _groundLayers = groundLayers;
        }

        public void CheckGrounded()
        {
            Vector3 spherePosition = new Vector3(
                _transform.position.x,
                _transform.position.y - _groundedOffset,
                _transform.position.z
            );
            
            IsGrounded = Physics.CheckSphere(
                spherePosition,
                _groundedRadius,
                _groundLayers,
                QueryTriggerInteraction.Ignore
            );
        }
    }
}


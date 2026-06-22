using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Exponential smoothing for replicated player camera poses on the DM spectate client.
    /// </summary>
    public sealed class PlayerViewStateSmoother
    {
        private bool _initialized;
        private Vector3 _position;
        private Quaternion _rotation;
        private float _fieldOfView;
        private bool _isOrthographic;

        public bool IsInitialized => _initialized;

        public void Reset()
        {
            _initialized = false;
        }

        public PlayerViewStateNetwork Step(
            PlayerViewStateNetwork target,
            float deltaTime,
            float positionSharpness,
            float rotationSharpness)
        {
            if (!_initialized || deltaTime <= 0f)
            {
                _position = target.Position;
                _rotation = target.Rotation;
                _fieldOfView = target.FieldOfView;
                _isOrthographic = target.IsOrthographic;
                _initialized = true;
                return target;
            }

            float positionBlend = 1f - Mathf.Exp(-positionSharpness * deltaTime);
            float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * deltaTime);

            _position = Vector3.Lerp(_position, target.Position, positionBlend);
            _rotation = Quaternion.Slerp(_rotation, target.Rotation, rotationBlend);
            _fieldOfView = Mathf.Lerp(_fieldOfView, target.FieldOfView, rotationBlend);

            if (_isOrthographic != target.IsOrthographic)
            {
                _isOrthographic = target.IsOrthographic;
                _fieldOfView = target.FieldOfView;
            }

            return new PlayerViewStateNetwork
            {
                Position = _position,
                Rotation = _rotation,
                FieldOfView = _fieldOfView,
                IsOrthographic = _isOrthographic,
            };
        }
    }
}

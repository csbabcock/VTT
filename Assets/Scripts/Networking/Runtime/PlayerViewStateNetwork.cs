using System;
using Unity.Netcode;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>Netcode-serializable camera pose replicated from each player's owning client.</summary>
    public struct PlayerViewStateNetwork : INetworkSerializable, IEquatable<PlayerViewStateNetwork>
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float FieldOfView;
        public bool IsOrthographic;

        public static PlayerViewStateNetwork FromCamera(Camera camera)
        {
            if (camera == null)
                return default;

            return new PlayerViewStateNetwork
            {
                Position = camera.transform.position,
                Rotation = camera.transform.rotation,
                FieldOfView = camera.fieldOfView,
                IsOrthographic = camera.orthographic,
            };
        }

        public void ApplyTo(Camera camera)
        {
            if (camera == null)
                return;

            camera.transform.SetPositionAndRotation(Position, Rotation);
            camera.orthographic = IsOrthographic;
            if (!IsOrthographic)
                camera.fieldOfView = FieldOfView;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref FieldOfView);
            serializer.SerializeValue(ref IsOrthographic);
        }

        public bool Equals(PlayerViewStateNetwork other)
        {
            return Position.Equals(other.Position)
                   && Rotation.Equals(other.Rotation)
                   && Mathf.Approximately(FieldOfView, other.FieldOfView)
                   && IsOrthographic == other.IsOrthographic;
        }

        public override bool Equals(object obj) => obj is PlayerViewStateNetwork other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Position.GetHashCode();
                hash = (hash * 397) ^ Rotation.GetHashCode();
                hash = (hash * 397) ^ FieldOfView.GetHashCode();
                hash = (hash * 397) ^ IsOrthographic.GetHashCode();
                return hash;
            }
        }
    }
}

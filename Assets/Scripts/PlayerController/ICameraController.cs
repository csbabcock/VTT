using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Interface for camera control
    /// </summary>
    public interface ICameraController
    {
        void ProcessRotation(Vector2 lookInput, bool lockCamera);
        void UpdateCamera();
        float Yaw { get; }
        float Pitch { get; }
    }
}


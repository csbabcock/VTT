using UnityEngine;

namespace GameCore.DmTools
{
    /// <summary>Pure movement math for the DM fly camera (unit-testable).</summary>
    public static class DmFlyCameraMath
    {
        public static Vector3 ComputePanDelta(Vector2 mouseDelta, float sensitivity)
        {
            return new Vector3(-mouseDelta.x * sensitivity, -mouseDelta.y * sensitivity, 0f);
        }

        public static Vector3 ApplyPan(Vector3 position, Quaternion rotation, Vector2 mouseDelta, float sensitivity)
        {
            Vector3 delta = ComputePanDelta(mouseDelta, sensitivity);
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            return position + right * delta.x + up * delta.y;
        }

        public static (Vector3 position, float yawDegrees, float pitchDegrees) Orbit(
            Vector3 position,
            float yawDegrees,
            float pitchDegrees,
            Vector3 pivot,
            Vector2 mouseDelta,
            float sensitivity,
            float minPitch,
            float maxPitch)
        {
            yawDegrees += mouseDelta.x * sensitivity;
            pitchDegrees -= mouseDelta.y * sensitivity;
            pitchDegrees = Mathf.Clamp(pitchDegrees, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            float distance = Vector3.Distance(position, pivot);
            if (distance < 0.01f)
                distance = 0.01f;

            position = pivot - rotation * Vector3.forward * distance;
            return (position, yawDegrees, pitchDegrees);
        }

        public static Vector3 ApplyZoom(Vector3 position, Quaternion rotation, float amount)
        {
            return position + rotation * Vector3.forward * amount;
        }

        public static Vector3 ComputeFlyMoveDelta(
            Vector2 moveInput,
            float verticalInput,
            Quaternion rotation,
            float speed,
            float deltaTime)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;
            Vector3 move = forward * moveInput.y + right * moveInput.x + Vector3.up * verticalInput;
            if (move.sqrMagnitude > 1f)
                move.Normalize();
            return move * speed * deltaTime;
        }

        public static float ScaleSpeed(float baseSpeed, bool fastModifier, float fastMultiplier)
        {
            return fastModifier ? baseSpeed * fastMultiplier : baseSpeed;
        }
    }
}

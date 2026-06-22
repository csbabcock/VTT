using GameCore.Actors;
using GameCore.Networking;
using UnityEngine;

namespace GameCore.DmTools
{
    /// <summary>
    /// Ensures DM-only runtime systems exist without manual scene wiring.
    /// </summary>
    public static class DmToolsBootstrap
    {
        public static void EnsureForLocalSession()
        {
            if (!SessionRoleLocator.IsDungeonMaster)
                return;

            EnsureFlyCamera();
            DmPlayerSpectateGateway.EnsureForLocalSession();
            DisableOfflinePlayerAvatars();
        }

        private static void EnsureFlyCamera()
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            var flyCamera = camera.GetComponent<DmFlyCameraController>();
            if (flyCamera == null)
                flyCamera = camera.gameObject.AddComponent<DmFlyCameraController>();

            flyCamera.enabled = true;
        }

        private static void DisableOfflinePlayerAvatars()
        {
            if (NetworkSessionProbe.IsNetworkListening())
                return;

            foreach (var controller in Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include))
            {
                if (controller != null && controller.gameObject.activeSelf)
                    controller.gameObject.SetActive(false);
            }
        }
    }
}

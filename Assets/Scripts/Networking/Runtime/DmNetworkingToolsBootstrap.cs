using GameCore.Actors;
using GameCore.DmTools;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Registers networked DM spectate wiring and ensures runtime components exist.
    /// </summary>
    public static class DmNetworkingToolsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterSpectateGateway()
        {
            DmPlayerSpectateGateway.Register(
                EnsureSpectateController,
                CanSpectateOwner,
                ExitSpectate);
        }

        public static void EnsureForLocalSession() => EnsureSpectateController();

        private static void EnsureSpectateController()
        {
            if (!SessionRoleLocator.IsDungeonMaster)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            if (camera.GetComponent<DmPlayerSpectateController>() == null)
                camera.gameObject.AddComponent<DmPlayerSpectateController>();
        }

        private static bool CanSpectateOwner(int ownerId)
        {
            IActor actor = ActorRegistry.GetByOwner(ownerId);
            if (actor?.Transform == null)
                return false;

            return actor.Transform.GetComponent<NetworkPlayerViewState>() != null;
        }

        private static void ExitSpectate()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            var controller = camera.GetComponent<DmPlayerSpectateController>();
            controller?.ExitSpectate();
        }
    }
}

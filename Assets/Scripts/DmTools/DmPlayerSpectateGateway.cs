using System;

namespace GameCore.DmTools
{
    /// <summary>
    /// Netcode-agnostic DM spectate seam. The networking layer registers concrete
    /// camera wiring so UI and core DM tools avoid a GameCore.Networking reference.
    /// </summary>
    public static class DmPlayerSpectateGateway
    {
        private static Action _ensureRuntime;
        private static Func<int, bool> _canSpectateOwner;
        private static Action _exitSpectate;

        public static void Register(Action ensureRuntime, Func<int, bool> canSpectateOwner, Action exitSpectate)
        {
            _ensureRuntime = ensureRuntime;
            _canSpectateOwner = canSpectateOwner;
            _exitSpectate = exitSpectate;
        }

        public static void EnsureForLocalSession() => _ensureRuntime?.Invoke();

        public static bool CanSpectateOwner(int ownerId) =>
            _canSpectateOwner != null && _canSpectateOwner(ownerId);

        public static void ExitSpectate() => _exitSpectate?.Invoke();
    }
}

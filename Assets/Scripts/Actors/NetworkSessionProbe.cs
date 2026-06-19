using System;
using System.Reflection;

namespace GameCore.Actors
{
    /// <summary>
    /// Detects an active NGO session without referencing Unity.Netcode from GameCore.
    /// </summary>
    internal static class NetworkSessionProbe
    {
        public static bool IsNetworkListening()
        {
            Type managerType = Type.GetType("Unity.Netcode.NetworkManager, Unity.Netcode.Runtime");
            if (managerType == null)
                return false;

            PropertyInfo singletonProperty = managerType.GetProperty(
                "Singleton", BindingFlags.Public | BindingFlags.Static);
            object singleton = singletonProperty?.GetValue(null);
            if (singleton == null)
                return false;

            PropertyInfo isListeningProperty = managerType.GetProperty(
                "IsListening", BindingFlags.Public | BindingFlags.Instance);
            return isListeningProperty != null && (bool)isListeningProperty.GetValue(singleton);
        }
    }
}

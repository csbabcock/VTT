using UnityEngine;
using Unity.Netcode;

public class NetworkButtons : MonoBehaviour
{
    public void StartClient() { NetworkManager.Singleton.StartClient(); }
    public void StartHost()   { NetworkManager.Singleton.StartHost(); }
    public void StartServer() { NetworkManager.Singleton.StartServer(); }
}
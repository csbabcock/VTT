using Unity.Netcode.Components;
using UnityEngine;
namespace VTT
{
    [DisallowMultipleComponent]
    public class ClientNetworkAnimator: NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
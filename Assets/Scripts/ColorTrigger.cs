using Unity.Netcode;
using UnityEngine;

public class ColorTrigger : NetworkBehaviour
{
    public NetworkVariable<Color> m_NetworkColor = new(Color.white);

    private Material m_InstanceMaterial;

    public override void OnNetworkSpawn()
    {
        m_NetworkColor.OnValueChanged += OnColorChanged;
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            m_InstanceMaterial = new Material(meshRenderer.material);
            meshRenderer.material = m_InstanceMaterial;
            UpdateMaterialColor(m_NetworkColor.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        m_NetworkColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        UpdateMaterialColor(newColor);
    }

    private void UpdateMaterialColor(Color newColor)
    {
        if (m_InstanceMaterial != null) m_InstanceMaterial.SetColor("_BaseColor", newColor);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        NetworkObject networkObject = other.GetComponent<NetworkObject>();
        if (IsClient && networkObject != null && networkObject.IsOwner)
        {
            ChangeColorServerRpc(networkObject.OwnerClientId);
        }
    }
    
    [Rpc(SendTo.Server)]
    private void ChangeColorServerRpc(ulong playerId)
    {
        // Simple team system: blue for even, red for odd
        Color newColor =
            (playerId % 2 == 0) ? new Color(0, 0, 1, 0.5f) : new Color(1, 0, 0, 0.5f);

        m_NetworkColor.Value = newColor;
    }
}
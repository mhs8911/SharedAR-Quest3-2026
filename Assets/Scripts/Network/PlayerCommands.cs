// ============================================================================
// Mariam code addition
// File: Scripts/Network/PlayerCommands.cs
// NEW. Lives on the PlayerAvatar prefab so clients can ask the server to
// spawn / despawn the shared object.
// This is why SharedObjectSpawner no longer needs to be a networked scene object.
// ============================================================================
using Unity.Netcode;
using UnityEngine;

public class PlayerCommands : NetworkBehaviour
{
    public static PlayerCommands Local { get; private set; }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerCommands] OnNetworkSpawn on {gameObject.name}. IsOwner={IsOwner}, IsServer={IsServer}");
        if (IsOwner) Local = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this) Local = null;
    }

    public void RequestSpawn(Vector3 sharedPosition, Quaternion sharedRotation)
    {
        Debug.Log("[PlayerCommands] RequestSpawn called, sending RPC to server.");
        SpawnSharedObjectRpc(sharedPosition, sharedRotation);
    }

    public void RequestDespawn()
        => DespawnSharedObjectRpc();

    [Rpc(SendTo.Server)]
    private void SpawnSharedObjectRpc(Vector3 sharedPosition, Quaternion sharedRotation)
    {
        Debug.Log($"[PlayerCommands] SpawnSharedObjectRpc received on server. Spawner null? {SharedObjectSpawner.Instance == null}");
        if (SharedObjectSpawner.Instance != null)
            SharedObjectSpawner.Instance.ServerSpawn(sharedPosition, sharedRotation);
    }

    [Rpc(SendTo.Server)]
    private void DespawnSharedObjectRpc()
    {
        if (SharedObjectSpawner.Instance != null)
            SharedObjectSpawner.Instance.ServerDespawn();
    }
}

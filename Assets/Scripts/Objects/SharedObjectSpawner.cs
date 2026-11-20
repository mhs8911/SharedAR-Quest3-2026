// ============================================================================
// Mariam code addition
// File: Scripts/Objects/SharedObjectSpawner.cs
// Changes from your version:
//  [REMOVED] Update() polling, FindObjectOfType<QRCodeAnchorManager>() every frame,
//            and the input checks. SpawnTrigger already handles input.
//  [REMOVED] DetectPinch(): OVRHand.GetFingerPinched is not a static method.
//  [CHANGED] spawn position is now in front of the requesting player's head,
//            sent in MARKER space, so every device agrees on where it goes.
//  [CHANGED] Clients can spawn too: the request goes to the server through
//            PlayerCommands, and ServerSpawn() runs on the server.
//  [ADDED]   SpawnSharedObject() / DespawnSharedObject() (no args), which are
//            exactly what your SpawnTrigger already calls.
//  [ADDED]   one shared object at a time; DontDestroyWithOwner so it survives
//            the client that grabbed it leaving.
// Plain MonoBehaviour in SharedARMain (NOT a NetworkObject).
// ============================================================================
using Unity.Netcode;
using UnityEngine;

public class SharedObjectSpawner : MonoBehaviour
{
    public static SharedObjectSpawner Instance { get; private set; }

    [SerializeField] private GameObject sharedObjectPrefab;   // must have a NetworkObject
    [SerializeField] private AlignmentManager alignmentManager;
    [SerializeField] private Transform head;                  // CenterEyeAnchor (auto-found if empty)
    [SerializeField] private float spawnDistance = 0.6f;
[SerializeField] private bool useFixedSpawn = true;                 // Editor test toggle
[SerializeField] private Vector3 fixedSharedSpawn = new Vector3(0f, 0f, 0.6f);

    private NetworkObject spawned;   // server only

    private void Awake()
    {
        Instance = this;
    }

    // ---- Called by SpawnTrigger on ANY device --------------------------------------

    public void SpawnSharedObject()
{
    Debug.Log("[Spawner] SpawnSharedObject called.");
    if (!CanRequest()) return;

    Vector3 sharedPos;
    Quaternion sharedRot;

    if (useFixedSpawn)
    {
        sharedPos = fixedSharedSpawn;
        sharedRot = Quaternion.identity;
        Debug.Log($"[Spawner] Using fixed shared spawn {sharedPos}.");
    }
    else
    {
        Vector3 worldPos = head.position + head.forward * spawnDistance;
        Quaternion worldRot = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
        sharedPos = alignmentManager.WorldToSharedPosition(worldPos);
        sharedRot = alignmentManager.WorldToSharedRotation(worldRot);
    }

    PlayerCommands.Local.RequestSpawn(sharedPos, sharedRot);
}

    public void DespawnSharedObject()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (PlayerCommands.Local == null) return;
        PlayerCommands.Local.RequestDespawn();
    }

    // ---- Server side (called by PlayerCommands RPCs) -------------------------------

public void ServerSpawn(Vector3 sharedPosition, Quaternion sharedRotation)
{
    Debug.Log($"[Spawner] ServerSpawn entered. IsServer={NetworkManager.Singleton.IsServer}, " +
              $"spawnedNull={spawned == null}, spawnedIsSpawned={spawned != null && spawned.IsSpawned}, " +
              $"aligned={alignmentManager != null && alignmentManager.IsAligned}");

    if (!NetworkManager.Singleton.IsServer)
    {
        Debug.LogWarning("[Spawner] ServerSpawn exit: not server.");
        return;
    }
    if (spawned != null && spawned.IsSpawned)
    {
        Debug.LogWarning("[Spawner] ServerSpawn exit: a shared object already exists. Press D to despawn first.");
        return;
    }

        Vector3 worldPos = alignmentManager.SharedToWorldPosition(sharedPosition);
        Quaternion worldRot = alignmentManager.SharedToWorldRotation(sharedRotation);

        GameObject obj = Instantiate(sharedObjectPrefab, worldPos, worldRot);
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[Spawner] SharedObject prefab has no NetworkObject.");
            Destroy(obj);
            return;
        }

        netObj.DontDestroyWithOwner = true;
        netObj.Spawn();
        spawned = netObj;

        Debug.Log($"Shared object spawned at shared {sharedPosition} (world {worldPos})");
    }

    public void ServerDespawn()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (spawned != null && spawned.IsSpawned)
            spawned.Despawn(true);
        spawned = null;
    }

    // ---- Helpers -------------------------------------------------------------------

private bool CanRequest()
{
    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
    {
        Debug.LogWarning("[Spawner] Not connected yet.");
        return false;
    }
    if (alignmentManager == null)
    {
        Debug.LogError("[Spawner] alignmentManager is not assigned in the Inspector.");
        return false;
    }
    if (!alignmentManager.IsAligned)
    {
        Debug.LogWarning("[Spawner] Request dropped: this device is not aligned.");
        return false;
    }
    if (PlayerCommands.Local == null)
    {
        Debug.LogError("[Spawner] Request dropped: PlayerCommands.Local is null.");
        return false;
    }
    if (head == null)
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null) head = rig.centerEyeAnchor;
    }
    if (head == null)
    {
        Debug.LogError("[Spawner] Request dropped: no head transform found.");
        return false;
    }
    return true;
}
}

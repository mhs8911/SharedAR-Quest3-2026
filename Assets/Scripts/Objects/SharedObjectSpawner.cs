// Scripts/Objects/SharedObjectSpawner.cs
// Role: Either player can call SpawnSharedObject().
//       The object is instantiated via Photon so it appears for both users
//       at the midpoint between their two avatar head positions —
//       all in shared-anchor world space.

using Photon.Pun;
using UnityEngine;

public class SharedObjectSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform localHead;            // OVRCameraRig centerEyeAnchor
    [SerializeField] private string    sharedObjectPrefab = "SharedObject"; // must be in Resources/

    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeightOffset = 0f;  // adjust if needed
    [SerializeField] private bool  requireAnchorAlignment = true;

    private bool _aligned = false;
    private GameObject _spawnedObject;

    private void Start()
    {
        if (localHead == null)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null) localHead = rig.centerEyeAnchor;
        }

        SharedAnchorManager.Instance.OnAlignmentComplete += () =>
        {
            _aligned = true;
            Debug.Log("[Spawner] Alignment complete — ready to spawn.");
        };
    }

    /// <summary>
    /// Call this from a button press, controller trigger, hand gesture, etc.
    /// Computes the midpoint between both players' heads and spawns there.
    /// </summary>
    public void SpawnSharedObject()
    {
        if (requireAnchorAlignment && !_aligned)
        {
            Debug.LogWarning("[Spawner] Cannot spawn — coordinate spaces not yet aligned.");
            return;
        }

        Vector3 spawnPos = GetMidpointBetweenPlayers();
        spawnPos.y      += spawnHeightOffset;

        // Face the spawned object toward the local player
        Vector3    lookDir  = localHead.position - spawnPos;
        lookDir.y           = 0;
        Quaternion spawnRot = lookDir != Vector3.zero
            ? Quaternion.LookRotation(lookDir)
            : Quaternion.identity;

        // PhotonNetwork.Instantiate syncs this across all clients automatically
        _spawnedObject = PhotonNetwork.Instantiate(
            sharedObjectPrefab,
            spawnPos,
            spawnRot
        );

        Debug.Log($"[Spawner] Shared object spawned at {spawnPos}");
    }

    /// <summary>Destroy the shared object for all players.</summary>
    public void DespawnSharedObject()
    {
        if (_spawnedObject != null && _spawnedObject.GetComponent<PhotonView>().IsMine)
            PhotonNetwork.Destroy(_spawnedObject);
    }

    // ── Midpoint Calculation ─────────────────────────────────────────────────

    private Vector3 GetMidpointBetweenPlayers()
    {
        Vector3 localPos  = localHead ? localHead.position : Vector3.zero;
        Vector3 remotePos = GetRemotePlayerHeadPosition();
        return (localPos + remotePos) * 0.5f;
    }

    private Vector3 GetRemotePlayerHeadPosition()
    {
        // Find all PlayerAvatarSync in scene — the one not ours is the remote player
        var syncs = FindObjectsOfType<PlayerAvatarSync>();
        foreach (var sync in syncs)
        {
            if (!sync.photonView.IsMine)
            {
                // The head visual transform represents the remote player's head
                Transform headVisual = sync.transform.Find("HeadVisual");
                if (headVisual != null) return headVisual.position;
            }
        }

        // Fallback: spawn 1.5m in front of local player if remote pos unknown
        Debug.LogWarning("[Spawner] Remote player position unknown — spawning in front of local.");
        return localHead.position + localHead.forward * 1.5f;
    }
}

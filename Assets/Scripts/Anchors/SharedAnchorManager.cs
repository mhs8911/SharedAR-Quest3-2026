// Scripts/Anchors/SharedAnchorManager.cs
// Role: The heart of the system.
//       Host creates a Meta Spatial Anchor, saves it to Meta's cloud,
//       shares it to the remote user, and broadcasts the UUID.
//       Client receives UUID and loads + localizes the same anchor.
//       Both players then align their OVRCameraRig to that anchor →
//       giving them a shared virtual coordinate space even across rooms.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oculus.Platform;
using Oculus.Platform.Models;
using Photon.Pun;
using UnityEngine;

public class SharedAnchorManager : MonoBehaviour
{
    public static SharedAnchorManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject anchorVisualizerPrefab;
    [SerializeField] private Transform  ovrCameraRigTrackingSpace; // drag OVRCameraRig > TrackingSpace here

    public event Action OnAlignmentComplete;

    private OVRSpatialAnchor _anchor;
    private ulong            _remoteMetaUserId;
    private bool             _remoteUserIdReady;
    private bool             _anchorReadyToShare;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        var net = SharedARNetworkManager.Instance;
        net.OnBothPlayersReady     += OnBothPlayersReady;
        net.OnAnchorUUIDBroadcast  += OnAnchorUUIDBroadcast; // client-side

        // Fetch our own remote user ID to share with the other player
        FetchAndBroadcastMetaUserId();
    }

    // ── Step 1: Exchange Meta User IDs via Photon Player Properties ──────────

    private async void FetchAndBroadcastMetaUserId()
    {
        // Requires Oculus Platform SDK initialized in OculusPlatformInit component
        var msg = await Users.GetLoggedInUser().Gen();
        if (msg.IsError) { Debug.LogError("[Anchors] Failed to get Meta user ID"); return; }

        ulong myId = msg.Data.ID;
        Debug.Log($"[Anchors] Local Meta User ID: {myId}");

        var props = new ExitGames.Client.Photon.Hashtable { { "MetaUID", myId.ToString() } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // Photon raises this on player property change — grab the remote user's ID
    public void OnRemotePlayerMetaId(ulong remoteId)
    {
        _remoteMetaUserId = remoteId;
        _remoteUserIdReady = true;
        TryShareIfReady();
    }

    // ── Step 2: Host creates anchor when both players are in room ────────────

    private void OnBothPlayersReady()
    {
        if (SharedARNetworkManager.Instance.IsHost)
            CreateAndSaveAnchorAsync();
        // Client just waits for OnAnchorUUIDBroadcast
    }

    private async void CreateAndSaveAnchorAsync()
    {
        Debug.Log("[Anchors] Creating spatial anchor in front of host...");

        // Place anchor ~1m in front of the user at floor level
        Camera  cam        = Camera.main;
        Vector3 forward    = cam.transform.forward;
        forward.y          = 0;
        Vector3 anchorPos  = cam.transform.position + forward.normalized * 1.0f;
        anchorPos.y        = 0f;
        Quaternion anchorRot = Quaternion.LookRotation(forward);

        GameObject go = Instantiate(anchorVisualizerPrefab, anchorPos, anchorRot);
        _anchor = go.AddComponent<OVRSpatialAnchor>();

        // Wait for Meta runtime to localize it
        await WaitForLocalization(_anchor);
        Debug.Log($"[Anchors] Anchor localized. UUID: {_anchor.Uuid}");

        // Save to Meta cloud
        var saveResult = await _anchor.SaveAsync(new OVRSpatialAnchor.SaveOptions
        {
            Storage = OVRSpace.StorageLocation.Cloud
        });

        if (!saveResult) { Debug.LogError("[Anchors] Cloud save failed."); return; }
        Debug.Log("[Anchors] Anchor saved to Meta cloud.");

        // Broadcast UUID to the other player via Photon
        SharedARNetworkManager.Instance.BroadcastAnchorUUID(_anchor.Uuid.ToString());

        _anchorReadyToShare = true;
        TryShareIfReady();

        // Host aligns immediately using their own anchor
        AlignTrackingSpaceToAnchor(_anchor);
    }

    private void TryShareIfReady()
    {
        if (_anchorReadyToShare && _remoteUserIdReady)
            ShareAnchorAsync(_remoteMetaUserId);
    }

    private async void ShareAnchorAsync(ulong remoteUserId)
    {
        Debug.Log($"[Anchors] Sharing anchor to Meta user {remoteUserId}...");
        var users  = new OVRSpaceUser[] { new OVRSpaceUser(remoteUserId) };
        var result = await _anchor.ShareAsync(users);

        Debug.Log(result ? "[Anchors] Anchor shared successfully." 
                         : "[Anchors] Anchor share FAILED.");
    }

    // ── Step 3: Client loads and localizes the shared anchor ─────────────────

    private async void OnAnchorUUIDBroadcast(string uuidStr)
    {
        if (!Guid.TryParse(uuidStr, out Guid uuid))
        {
            Debug.LogError("[Anchors] Invalid UUID received."); return;
        }

        Debug.Log($"[Anchors] Loading shared anchor: {uuid}");

        var unboundList = new List<OVRSpatialAnchor.UnboundAnchor>();
        bool loaded = await OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(
            new List<Guid> { uuid }, unboundList);

        if (!loaded || unboundList.Count == 0)
        {
            Debug.LogError("[Anchors] Failed to load shared anchor. Is it shared to this user?");
            return;
        }

        var unbound = unboundList[0];

        // Bind to a new scene object
        GameObject go = anchorVisualizerPrefab != null
            ? Instantiate(anchorVisualizerPrefab)
            : new GameObject("SharedAnchor_Client");

        _anchor = go.AddComponent<OVRSpatialAnchor>();
        unbound.BindTo(_anchor);

        await WaitForLocalization(_anchor);
        Debug.Log("[Anchors] Client localized shared anchor. Aligning...");

        AlignTrackingSpaceToAnchor(_anchor);
    }

    // ── Step 4: Align OVRCameraRig so anchor sits at world origin ────────────
    //
    //    After this, world position (0,0,0) == the shared anchor for BOTH users.
    //    Any Photon-spawned object at (1, 0, 0) will appear 1m to the right
    //    of that anchor for both players — even though they're in separate rooms.

    private void AlignTrackingSpaceToAnchor(OVRSpatialAnchor anchor)
    {
        if (ovrCameraRigTrackingSpace == null)
        {
            Debug.LogError("[Anchors] ovrCameraRigTrackingSpace not assigned in Inspector.");
            return;
        }

        Transform anchorT = anchor.transform;

        // Compute the offset needed to put anchor at world origin
        Quaternion rotOffset = Quaternion.Inverse(anchorT.rotation);
        Vector3    posOffset = -(rotOffset * anchorT.position);

        ovrCameraRigTrackingSpace.rotation = rotOffset;
        ovrCameraRigTrackingSpace.position = posOffset;

        Debug.Log("[Anchors] ✅ Tracking space aligned. Shared coordinate system active.");
        OnAlignmentComplete?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task WaitForLocalization(OVRSpatialAnchor anchor, int timeoutMs = 10000)
    {
        int elapsed = 0;
        while (!anchor.Localized && elapsed < timeoutMs)
        {
            await Task.Delay(100);
            elapsed += 100;
        }
        if (!anchor.Localized)
            Debug.LogError("[Anchors] Anchor failed to localize within timeout.");
    }
}

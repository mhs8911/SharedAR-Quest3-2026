// ============================================================================
// Mariam code addition
// File: Scripts/Objects/NetworkedSharedObject.cs
// Changes from your version:
//  [REMOVED] [RequireComponent(typeof(NetworkTransform))] and the NetworkTransform
//            field. Pose sync now lives in MarkerSpaceTransform (marker space).
//  [CHANGED] Rigidbody: kept kinematic everywhere by default. A grab system moves the
//            object and MarkerSpaceTransform syncs it. Set ownerSimulatesPhysics
//            to restore your old "owner runs physics" behaviour.
//  [REMOVED] empty FixedUpdate.
//  [ADDED]   Grab() / Release(): call these from your grab or pinch events.
//            The server hands ownership to the grabber.
//  [ADDED]   HolderId NetworkVariable plus color feedback
//            (White = free, Blue = host holds it, Red = client holds it).
// Required on the SharedObject prefab: NetworkObject, MarkerSpaceTransform.
// ============================================================================
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MarkerSpaceTransform))]
public class NetworkedSharedObject : NetworkBehaviour
{
    private const ulong NoHolder = ulong.MaxValue;

    [Header("Visual feedback (assign Color/White, Blue, Red materials in that order)")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material[] colorMaterials;   // [0]=free, [1]=host, [2]=client

    [Header("Physics")]
    [SerializeField] private bool ownerSimulatesPhysics = false;

    // Server writes, everyone reads
    public readonly NetworkVariable<ulong> HolderId = new NetworkVariable<ulong>(NoHolder);

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"Object spawned. Owner: {OwnerClientId}, IsOwner: {IsOwner}");   // from your original

        HolderId.OnValueChanged += OnHolderChanged;
        ApplyColor();
        UpdateRigidbody();

        if (IsServer)
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        HolderId.OnValueChanged -= OnHolderChanged;
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    public override void OnGainedOwnership() => UpdateRigidbody();
    public override void OnLostOwnership() => UpdateRigidbody();

    // ---- Public API: hook to your grab / pinch events --------------------------

    public void Grab()
    {
        if (IsSpawned) RequestGrabRpc();
    }

    public void Release()
    {
        if (IsSpawned) RequestReleaseRpc();
    }

    // ---- Server logic ----------------------------------------------------------

    [Rpc(SendTo.Server)]
    private void RequestGrabRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        // someone else is holding it
        if (HolderId.Value != NoHolder && HolderId.Value != sender)
            return;

        HolderId.Value = sender;

        if (NetworkObject.OwnerClientId != sender)
            NetworkObject.ChangeOwnership(sender);
    }

    [Rpc(SendTo.Server)]
    private void RequestReleaseRpc(RpcParams rpcParams = default)
    {
        if (HolderId.Value != rpcParams.Receive.SenderClientId)
            return;

        // Ownership intentionally stays with the last holder so their final pose
        // is not lost. The next grab transfers it.
        HolderId.Value = NoHolder;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (HolderId.Value == clientId)
            HolderId.Value = NoHolder;
    }

    // ---- Local visuals ---------------------------------------------------------

    private void OnHolderChanged(ulong previous, ulong current) => ApplyColor();

    private void ApplyColor()
    {
        if (targetRenderer == null || colorMaterials == null || colorMaterials.Length < 3)
            return;

        ulong holder = HolderId.Value;
        int index = holder == NoHolder ? 0 : (holder == NetworkManager.ServerClientId ? 1 : 2);
        targetRenderer.sharedMaterial = colorMaterials[index];
    }

    private void UpdateRigidbody()
    {
        if (rb != null)
            rb.isKinematic = !(IsOwner && ownerSimulatesPhysics);
    }
}

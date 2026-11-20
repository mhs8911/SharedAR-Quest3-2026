// ============================================================================
// Mariam code addition
// File: Scripts/Objects/MarkerSpaceTransform.cs
// NEW. Replaces NetworkTransform on SharedObject. Syncs the object's pose in
// MARKER space, so it lands at the same physical spot on both headsets.
// Do NOT also add NetworkTransform to the prefab.
// ============================================================================
using Unity.Netcode;
using UnityEngine;

public class MarkerSpaceTransform : NetworkBehaviour
{
    [SerializeField] private float sendRate = 30f;
    [SerializeField, Range(0f, 60f)] private float smoothing = 25f;
    [SerializeField] private float positionThreshold = 0.0005f;   // meters
    [SerializeField] private float angleThreshold = 0.1f;         // degrees

    // Owner writes, everyone reads
    public readonly NetworkVariable<NetPose> SharedPose =
        new NetworkVariable<NetPose>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    private AlignmentManager alignment;
    private Renderer[] renderers;
    private NetPose lastSent;
    private bool hasSent;
    private bool hasSnapped;
    private float nextSendTime;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        alignment = FindFirstObjectByType<AlignmentManager>();
        hasSent = false;
        hasSnapped = false;

        // Non-owners stay hidden until the first pose arrives (NGO does not sync the
        // spawn transform without NetworkTransform).
        SetRenderers(IsOwner);
    }

    public override void OnGainedOwnership()
    {
        hasSent = false;       // push the current pose right away
        SetRenderers(true);
    }

    private void Update()
    {
        if (!IsSpawned || alignment == null || !alignment.IsAligned)
            return;

        if (IsOwner) OwnerWrite();
        else RemoteApply();
    }

    private void OwnerWrite()
    {
        if (Time.unscaledTime < nextSendTime)
            return;

        NetPose current = new NetPose(
            alignment.WorldToSharedPosition(transform.position),
            alignment.WorldToSharedRotation(transform.rotation));

        if (hasSent
            && (current.position - lastSent.position).sqrMagnitude < positionThreshold * positionThreshold
            && Quaternion.Angle(current.rotation, lastSent.rotation) < angleThreshold)
            return;

        nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
        SharedPose.Value = current;
        lastSent = current;
        hasSent = true;
    }

    private void RemoteApply()
    {
        NetPose target = SharedPose.Value;
        if (!target.IsValid)
            return;

        Vector3 worldPos = alignment.SharedToWorldPosition(target.position);
        Quaternion worldRot = alignment.SharedToWorldRotation(target.rotation);

        if (!hasSnapped || smoothing <= 0f)
        {
            transform.SetPositionAndRotation(worldPos, worldRot);
            hasSnapped = true;
            SetRenderers(true);
            return;
        }

        float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, worldPos, k),
            Quaternion.Slerp(transform.rotation, worldRot, k));
    }

    private void SetRenderers(bool visible)
    {
        if (renderers == null) return;
        foreach (Renderer r in renderers)
            if (r != null) r.enabled = visible;
    }
}

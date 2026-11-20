// ============================================================================
// Mariam code addition
// File: Scripts/Network/NetworkedPlayerPose.cs
// NEW. Replaces PlayerAvatarSync. Changes from that script:
//  [CHANGED] sends marker-relative SharedPlayerPose (Mitsuki's math) instead of
//            world-space positions, so both headsets agree.
//  [CHANGED] sends head + both hands WITH rotation in ONE NetworkVariable
//            (before: 4 separate variables, no hand rotation).
//  [CHANGED] throttled to sendRate instead of every frame.
//  [REMOVED] primitive visuals moved to RemoteHeadHandsVisualizer.
// Put on the PlayerAvatar prefab (with a NetworkObject).
// ============================================================================
using Unity.Netcode;
using UnityEngine;

public class NetworkedPlayerPose : NetworkBehaviour
{
    [SerializeField] private float sendRate = 30f;

    // Owner writes, everyone reads
    public readonly NetworkVariable<SharedPlayerPose> SharedPose =
        new NetworkVariable<SharedPlayerPose>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    private SharedPlayerPoseProvider localProvider;
    private float nextSendTime;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            localProvider = FindFirstObjectByType<SharedPlayerPoseProvider>();
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || localProvider == null)
            return;

        if (Time.unscaledTime < nextSendTime)
            return;

        if (!localProvider.TryGetSharedPlayerPose(out SharedPlayerPose pose))
            return;

        nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
        SharedPose.Value = pose;
    }

    /// <summary>Latest shared-space pose. False until the owner has sent real data.</summary>
    public bool TryGetSharedPlayerPose(out SharedPlayerPose pose)
    {
        pose = SharedPose.Value;
        Quaternion q = pose.head.rotation;
        return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 0.5f;
    }
}

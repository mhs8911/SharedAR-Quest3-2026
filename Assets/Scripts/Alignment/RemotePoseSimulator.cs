// ============================================================================
// Mitsuki Original Code  |  Scripts/Alignment/RemotePoseSimulator.cs
// NO CODE CHANGES. Offline / single-machine testing only (simulates AR<->VR with no
// network). Disable it in any scene running real Netcode, or it will fight the
// networked avatars.
// ============================================================================
using UnityEngine;

public class RemotePoseSimulator : MonoBehaviour
{
    public AlignmentManager alignmentManager;

    // AR → VR
    public Transform arPlayer;
    public Transform vrSharedSpace;
    public Transform remoteARAvatar;

    // VR → AR
    public Transform vrPlayer;
    public Transform remoteVRAvatar;

    void Update()
    {
        if (!alignmentManager.IsAligned)
            return;

        // -------------------------
        // AR → VR
        // -------------------------

        Vector3 arSharedPosition =
            alignmentManager.WorldToSharedPosition(
                arPlayer.position
            );

        Quaternion arSharedRotation =
            alignmentManager.WorldToSharedRotation(
                arPlayer.rotation
            );

        Vector3 arInVRWorld =
            vrSharedSpace.TransformPoint(
                arSharedPosition
            );

        Quaternion arInVRRotation =
            vrSharedSpace.rotation
            * arSharedRotation;

        remoteARAvatar.SetPositionAndRotation(
            arInVRWorld,
            arInVRRotation
        );


        // -------------------------
        // VR → Shared
        // -------------------------

        Vector3 vrSharedPosition =
            vrSharedSpace.InverseTransformPoint(
                vrPlayer.position
            );

        Quaternion vrSharedRotation =
            Quaternion.Inverse(vrSharedSpace.rotation)
            * vrPlayer.rotation;


        // -------------------------
        // Shared → AR World
        // -------------------------

        Vector3 vrInARWorld =
            alignmentManager.SharedToWorldPosition(
                vrSharedPosition
            );

        Quaternion vrInARRotation =
            alignmentManager.SharedToWorldRotation(
                vrSharedRotation
            );

        remoteVRAvatar.SetPositionAndRotation(
            vrInARWorld,
            vrInARRotation
        );
    }
}

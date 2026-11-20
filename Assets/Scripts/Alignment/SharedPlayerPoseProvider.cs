// ============================================================================
// Mitsuki Original Code  |  Scripts/Alignment/SharedPlayerPoseProvider.cs
// NO CODE CHANGES. NetworkedPlayerPose reads from this on the local (owner) device.
// Assign head = CenterEyeAnchor, leftHand = LeftHandAnchor, rightHand = RightHandAnchor.
// ============================================================================
using UnityEngine;

public class SharedPlayerPoseProvider : MonoBehaviour
{
    public AlignmentManager alignmentManager;

    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    public bool TryGetSharedPlayerPose(out SharedPlayerPose pose)
    {
        pose = default;

        if (alignmentManager == null ||
            !alignmentManager.IsAligned ||
            head == null ||
            leftHand == null ||
            rightHand == null)
        {
            return false;
        }

        pose.head = ToSharedPose(head);
        pose.leftHand = ToSharedPose(leftHand);
        pose.rightHand = ToSharedPose(rightHand);

        return true;
    }

    private Pose ToSharedPose(Transform target)
    {
        Vector3 position =
            alignmentManager.WorldToSharedPosition(
                target.position);

        Quaternion rotation =
            alignmentManager.WorldToSharedRotation(
                target.rotation);

        return new Pose(position, rotation);
    }
}

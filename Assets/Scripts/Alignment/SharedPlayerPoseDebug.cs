// ============================================================================
// Mitsuki Original Code  |  Scripts/Alignment/SharedPlayerPoseDebug.cs
// NO CODE CHANGES.
// ============================================================================
using UnityEngine;

public class SharedPlayerPoseDebug : MonoBehaviour
{
    public SharedPlayerPoseProvider provider;

    private bool hasLogged;

    void Update()
    {
        if (hasLogged || provider == null)
            return;

        if (!provider.TryGetSharedPlayerPose(
            out SharedPlayerPose pose))
            return;

        Debug.Log($"Head: {pose.head.position}");
        Debug.Log($"Left Hand: {pose.leftHand.position}");
        Debug.Log($"Right Hand: {pose.rightHand.position}");

        hasLogged = true;
    }
}

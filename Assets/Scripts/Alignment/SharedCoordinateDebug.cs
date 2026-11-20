// ============================================================================
// Mitsuki Original Code  |  Scripts/Alignment/SharedCoordinateDebug.cs
// Only change: logging is null-guarded and throttled. The original logged every
// frame, which floods logcat on a headset.
// ============================================================================
using UnityEngine;

public class SharedCoordinateDebug : MonoBehaviour
{
    public Transform markerOrigin;
    public Transform arPlayer;
    public Transform vrPlayer;

    // [ADDED]
    public float logInterval = 1f;
    private float nextLogTime;

    void Update()
    {
        // [ADDED] guard + throttle
        if (markerOrigin == null || arPlayer == null || vrPlayer == null)
            return;
        if (Time.time < nextLogTime)
            return;
        nextLogTime = Time.time + logInterval;

        Vector3 arLocalPosition =
            markerOrigin.InverseTransformPoint(arPlayer.position);

        Quaternion arLocalRotation =
            Quaternion.Inverse(markerOrigin.rotation) * arPlayer.rotation;

        Vector3 vrLocalPosition =
            markerOrigin.InverseTransformPoint(vrPlayer.position);

        Quaternion vrLocalRotation =
            Quaternion.Inverse(markerOrigin.rotation) * vrPlayer.rotation;

        Debug.Log(
            $"AR Pos: {arLocalPosition} Rot: {arLocalRotation.eulerAngles} | " +
            $"VR Pos: {vrLocalPosition} Rot: {vrLocalRotation.eulerAngles}"
        );
    }
}

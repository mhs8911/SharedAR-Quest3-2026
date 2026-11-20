// ============================================================================
// Mitsuki Original Code  |  Scripts/Alignment/FakeMarkerProvider.cs
// For Editor testing and a VR second user (put fakeMarker where the physical
// QR code would be in the VR scene).
// ============================================================================
using UnityEngine;

// [CHANGED] original: public class FakeMarkerProvider : MonoBehaviour
// Inherits MarkerProviderBase so AlignmentManager can swap it with MrukQrMarkerProvider.
public class FakeMarkerProvider : MarkerProviderBase
{
    public Transform fakeMarker;

    // [ADDED] Original logged every frame. Logging is now opt-in.
    [SerializeField] private bool logPose = false;

    // [CHANGED] original: public bool TryGetMarkerPose(...)
    public override bool TryGetMarkerPose(out Vector3 position, out Quaternion rotation)
    {
        if (fakeMarker == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = fakeMarker.position;
        rotation = fakeMarker.rotation;

        // [REMOVED] original (moved into Update behind logPose):
        // Debug.Log(
        //     $"Detected Marker → Position: {position}, Rotation: {rotation.eulerAngles}"
        // );

        return true;
    }

    void Update()
    {
        // [CHANGED] original: TryGetMarkerPose(out _, out _);
        if (logPose && TryGetMarkerPose(out Vector3 p, out Quaternion r))
        {
          //  Debug.Log($"Detected Marker → Position: {p}, Rotation: {r.eulerAngles}");
        }
    }
}

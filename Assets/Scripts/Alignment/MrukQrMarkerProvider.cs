// ============================================================================
// Mariam code addition
// File: Scripts/Alignment/MrukQrMarkerProvider.cs
// NEW. Replaces QRCodeAnchorManager. Changes from that script:
//  [CHANGED] namespace Meta.XR.MRUtility -> Meta.XR.MRUtilityKit
//  [CHANGED] events are on MRUK.Instance.SceneSettings (UnityEvents), not MRUK.Instance
//  [CHANGED] removal compares trackable to trackable (was trackable == anchor)
//  [REMOVED] SharedARNetworkManager.SetAnchorReady: sending a world-space pose to the
//            other headset is wrong, since each device has its own world origin.
//  [REMOVED] RegisterSceneLoadedCallback: QR tracking doesn't need a loaded room scene.
// Verify member names against your installed MRUK version.
// Requires: OVRManager Anchor Support = Enabled, Scene Support = Required,
//           MRUK > Scene Settings > Tracker Configuration > QR Code Tracking ON,
//           Scene (spatial data) permission granted at runtime.
// ============================================================================
using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class MrukQrMarkerProvider : MarkerProviderBase
{
    [Tooltip("Leave empty to accept the first QR code seen. Otherwise the QR text must contain this.")]
    [SerializeField] private string requiredPayload = "shared-ar-anchor";   // kept from QRCodeAnchorManager

    private MRUKTrackable current;
    private bool subscribed;

    private IEnumerator Start()
    {
        while (MRUK.Instance == null) yield return null;

        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
        MRUK.Instance.SceneSettings.TrackableRemoved.AddListener(OnTrackableRemoved);
        subscribed = true;
    }

    private void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        string payload = trackable.MarkerPayloadString ?? "";
        if (!string.IsNullOrEmpty(requiredPayload) && !payload.Contains(requiredPayload))
            return;

        current = trackable;
        Debug.Log($"[QR] Marker found: '{payload}'");
    }

    private void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (trackable == current)
        {
            current = null;
            Debug.Log("[QR] Marker lost");
        }
    }

    public override bool TryGetMarkerPose(out Vector3 position, out Quaternion rotation)
    {
        if (current != null && current.IsTracked)
        {
            position = current.transform.position;
            rotation = current.transform.rotation;
            return true;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    private void OnDestroy()
    {
        if (subscribed && MRUK.Instance != null)
        {
            MRUK.Instance.SceneSettings.TrackableAdded.RemoveListener(OnTrackableAdded);
            MRUK.Instance.SceneSettings.TrackableRemoved.RemoveListener(OnTrackableRemoved);
        }
    }
}

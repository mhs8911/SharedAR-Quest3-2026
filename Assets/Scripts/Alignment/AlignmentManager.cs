// ============================================================================
// Mitsuki Original Code  |  Scripts/Alignment/AlignmentManager.cs
// Conversion methods unchanged. Added: provider base type, optional lock, events,
// and [AlignDiag] diagnostics (toggle with verboseDiagnostics).
// ============================================================================
using System;
using UnityEngine;

public class AlignmentManager : MonoBehaviour
{
    public Transform sharedSpace;

    // Base type, so FakeMarkerProvider (Editor/VR) or MrukQrMarkerProvider (Quest AR)
    // can be assigned. Field name unchanged, so the scene reference survives.
    public MarkerProviderBase markerProvider;

    [Header("Diagnostics")]
    public bool verboseDiagnostics = true;
    public float diagnosticInterval = 1f;

    // Optional lock. Turn ON for device builds to stop QR jitter.
    // OFF by default = "always follow the marker" behaviour.
    [Header("Locking (recommended ON for device builds)")]
    public bool lockWhenStable = false;
    public float stableSeconds = 1.5f;
    public float stablePositionTolerance = 0.01f;   // meters
    public float stableAngleTolerance = 2f;         // degrees

    public bool IsAligned { get; private set; }
    public bool IsLocked { get; private set; }
    public event Action OnAligned;
    public event Action OnAlignmentLost;

    private float stableTimer;
    private bool hasPose;
    private float diagTimer;

    void Update()
    {
        UpdateAlignment();
    }

    void UpdateAlignment()
    {
        // Once locked, stop following the marker
        if (IsLocked)
            return;

        // Throttle diagnostics to once per diagnosticInterval
        bool report = false;
        if (verboseDiagnostics)
        {
            diagTimer += Time.deltaTime;
            if (diagTimer >= diagnosticInterval)
            {
                diagTimer = 0f;
                report = true;
            }
        }

        if (markerProvider == null || sharedSpace == null)
        {
            if (report)
                Debug.LogWarning($"[AlignDiag] Missing reference: sharedSpace={(sharedSpace == null ? "NULL" : "ok")}, " +
                                 $"markerProvider={(markerProvider == null ? "NULL" : "ok")}");
            stableTimer = 0f;
            SetAligned(false);
            return;
        }

        if (!markerProvider.TryGetMarkerPose(
                out Vector3 markerPosition,
                out Quaternion markerRotation))
        {
            if (report)
                Debug.LogWarning($"[AlignDiag] {markerProvider.GetType().Name} on " +
                                 $"'{markerProvider.gameObject.name}' returned NO pose.");
            stableTimer = 0f;
            SetAligned(false);
            return;
        }

        // Stability check. Must run BEFORE sharedSpace is overwritten.
        bool moved = !hasPose
            || Vector3.Distance(markerPosition, sharedSpace.position) > stablePositionTolerance
            || Quaternion.Angle(markerRotation, sharedSpace.rotation) > stableAngleTolerance;

        if (report)
            Debug.Log($"[AlignDiag] Pose received from {markerProvider.GetType().Name}: " +
                      $"pos={markerPosition}, rot={markerRotation.eulerAngles}, moved={moved}, " +
                      $"stableTimer={stableTimer:F2}");

        sharedSpace.SetPositionAndRotation(
            markerPosition,
            markerRotation
        );
        hasPose = true;

        SetAligned(true);

        stableTimer = moved ? 0f : stableTimer + Time.deltaTime;
        if (lockWhenStable && stableTimer >= stableSeconds)
        {
            IsLocked = true;
            Debug.Log("[Alignment] Locked to marker.");
        }
    }

    // Hook to a UI button to re-scan the marker.
    public void Realign()
    {
        IsLocked = false;
        stableTimer = 0f;
        hasPose = false;
        if (verboseDiagnostics)
            Debug.Log("[AlignDiag] Realign requested. Lock cleared.");
    }

    // Fires events only on state changes.
    private void SetAligned(bool value)
    {
        if (value == IsAligned) return;
        IsAligned = value;

        if (verboseDiagnostics)
            Debug.Log($"[AlignDiag] IsAligned changed to {value}");

        if (value) OnAligned?.Invoke();
        else OnAlignmentLost?.Invoke();
    }

    // World / Tracking → Shared   (UNCHANGED)
    public Vector3 WorldToSharedPosition(Vector3 worldPosition)
    {
        return sharedSpace.InverseTransformPoint(worldPosition);
    }

    public Quaternion WorldToSharedRotation(Quaternion worldRotation)
    {
        return Quaternion.Inverse(sharedSpace.rotation)
               * worldRotation;
    }

    // Shared → World / Tracking   (UNCHANGED)
    public Vector3 SharedToWorldPosition(Vector3 sharedPosition)
    {
        return sharedSpace.TransformPoint(sharedPosition);
    }

    public Quaternion SharedToWorldRotation(Quaternion sharedRotation)
    {
        return sharedSpace.rotation
               * sharedRotation;
    }
}

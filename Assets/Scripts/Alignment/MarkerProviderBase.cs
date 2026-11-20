// ============================================================================
// Mariam code addition
// File: Scripts/Alignment/MarkerProviderBase.cs
// NEW. Abstract marker source. The signature matches Mitsuki's original
// FakeMarkerProvider.TryGetMarkerPose, so AlignmentManager works with any provider.
// ============================================================================
using UnityEngine;

public abstract class MarkerProviderBase : MonoBehaviour
{
    public abstract bool TryGetMarkerPose(out Vector3 position, out Quaternion rotation);
}

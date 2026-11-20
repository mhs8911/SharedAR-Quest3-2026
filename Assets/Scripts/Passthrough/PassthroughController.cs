// ============================================================================
// Mariam code addition
// File: Scripts/Passthrough/PassthroughController.cs
// ONE CHANGE: FindObjectOfType -> FindFirstObjectByType (obsolete in Unity 6).
// Everything else is identical. If OVRManager.isInsightPassthroughEnabled shows an
// obsolete warning in your Meta SDK version, enable passthrough on OVRManager in the
// Inspector instead and delete that block.
// ============================================================================
using UnityEngine;

[RequireComponent(typeof(OVRPassthroughLayer))]
public class PassthroughController : MonoBehaviour
{
    [Header("Passthrough Settings")]
    [SerializeField] private bool   startEnabled   = true;
    [SerializeField, Range(0f, 1f)]
    private float                   opacity         = 1f;

    private OVRPassthroughLayer _passthroughLayer;
    private OVRManager          _ovrManager;

    private void Awake()
    {
        _passthroughLayer = GetComponent<OVRPassthroughLayer>();
        // [CHANGED] original: FindObjectOfType<OVRManager>();
        _ovrManager       = FindFirstObjectByType<OVRManager>();
    }

    private void Start()
    {
        // Tell OVRManager we are using passthrough
        if (_ovrManager != null)
        {
            _ovrManager.isInsightPassthroughEnabled = true;
        }

        // Configure the passthrough layer
        _passthroughLayer.textureOpacity        = opacity;
        _passthroughLayer.hidden                = !startEnabled;

        // Make Unity's camera background transparent so passthrough shows through
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }

        Debug.Log("[Passthrough] AR passthrough layer active.");
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Toggle passthrough on/off at runtime (e.g. for a VR/AR mode switch).</summary>
    public void SetPassthroughEnabled(bool enabled)
    {
        _passthroughLayer.hidden = !enabled;
        Debug.Log($"[Passthrough] Layer {(enabled ? "enabled" : "disabled")}.");
    }

    /// <summary>Adjust opacity at runtime (0 = fully virtual, 1 = full passthrough).</summary>
    public void SetOpacity(float value)
    {
        opacity = Mathf.Clamp01(value);
        _passthroughLayer.textureOpacity = opacity;
    }

    /// <summary>Apply an edge-detection style visual effect to the passthrough feed.</summary>
    public void SetEdgeColor(Color color)
    {
        _passthroughLayer.edgeColor = color;
    }
}

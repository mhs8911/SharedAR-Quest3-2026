// Scripts/Passthrough/PassthroughController.cs
// Role: Enables Meta Quest 3's color passthrough layer so the
//       real world is visible through the headset (true AR mode).
//       Attach to your OVRCameraRig GameObject.

using UnityEngine;

[RequireComponent(typeof(OVRPassthroughLayer))]
public class PassthroughController : MonoBehaviour
{
    [Header("Passthrough Settings")]
    [SerializeField] private bool   startEnabled   = true;
    [SerializeField, Range(0f, 1f)]
    private float                   opacity         = 1f;
    [SerializeField]
    private OVRPassthroughLayer.ProjectionSurfaceType surfaceType
        = OVRPassthroughLayer.ProjectionSurfaceType.Reconstructed;

    private OVRPassthroughLayer _passthroughLayer;
    private OVRManager          _ovrManager;

    private void Awake()
    {
        _passthroughLayer = GetComponent<OVRPassthroughLayer>();
        _ovrManager       = FindObjectOfType<OVRManager>();
    }

    private void Start()
    {
        // Tell OVRManager we are using passthrough
        if (_ovrManager != null)
        {
            _ovrManager.isInsightPassthroughEnabled = true;
        }

        // Configure the passthrough layer
        _passthroughLayer.projectionSurfaceType = surfaceType;
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

    // ── Public API ───────────────────────────────────────────────────    

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


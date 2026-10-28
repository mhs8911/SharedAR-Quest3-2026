// Scripts/DebugStatusUI.cs
// Role: Renders a world-space status panel visible in-headset so
//       you can see connection and alignment state during development.
//       Attach to a Canvas set to World Space mode.

using Photon.Pun;
using TMPro;
using UnityEngine;

public class DebugStatusUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI networkStatusText;
    [SerializeField] private TextMeshProUGUI anchorStatusText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Transform       followTarget; // assign centerEyeAnchor

    [Header("UI Follow Settings")]
    [SerializeField] private Vector3 offsetFromHead = new Vector3(0f, -0.3f, 1.0f);
    [SerializeField] private float   followSpeed    = 4f;

    private bool _anchorAligned = false;

    private void Start()
    {
        SharedAnchorManager.Instance.OnAlignmentComplete += () =>
        {
            _anchorAligned = true;
        };

        if (followTarget == null)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig) followTarget = rig.centerEyeAnchor;
        }
    }

    private void Update()
    {
        UpdateText();
        FollowHead();
    }

    private void UpdateText()
    {
        if (networkStatusText)
            networkStatusText.text = $"Network: {(PhotonNetwork.IsConnected ? "✅ Connected" : "⏳ Connecting...")}";

        if (anchorStatusText)
            anchorStatusText.text  = $"Anchor: {(_anchorAligned ? "✅ Aligned" : "⏳ Waiting...")}";

        if (playerCountText)
            playerCountText.text   = $"Players: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0} / 2";
    }

    private void FollowHead()
    {
        if (followTarget == null) return;

        Vector3 targetPos = followTarget.TransformPoint(offsetFromHead);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // Always face the player
        transform.LookAt(followTarget);
        transform.Rotate(0f, 180f, 0f);
    }
}

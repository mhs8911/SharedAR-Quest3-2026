// ============================================================================
// Mariam code addition
// File: Scripts/Network/DebugStatusUI.cs
// Changes from your version:
//  [REMOVED] using Photon.Pun; PhotonNetwork.IsConnected / CurrentRoom.PlayerCount
//  [REMOVED] SharedAnchorManager.Instance.OnAlignmentComplete (that script is deleted)
//  [CHANGED] anchor line now reads AlignmentManager.IsAligned / IsLocked
//  [CHANGED] network line reads NetworkManager state; host shows its IP so the
//            client knows where to connect
//  [CHANGED] player count = number of spawned NetworkedPlayerPose objects
//            (works on host and client; ConnectedClients is server-only)
//  [KEPT]    follow-the-head behaviour and all serialized fields
//  [CHANGED] FindObjectOfType -> FindFirstObjectByType (Unity 6)
// ============================================================================
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DebugStatusUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI networkStatusText;
    [SerializeField] private TextMeshProUGUI anchorStatusText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Transform       followTarget; // assign centerEyeAnchor

    [Header("References (auto-found if empty)")]
    [SerializeField] private AlignmentManager alignmentManager;   // [ADDED]

    [Header("UI Follow Settings")]
    [SerializeField] private Vector3 offsetFromHead = new Vector3(0f, -0.3f, 1.0f);
    [SerializeField] private float   followSpeed    = 4f;

    private int   playerCount;        // [ADDED]
    private float nextCountTime;      // [ADDED]

    private void Start()
    {
        if (alignmentManager == null)
            alignmentManager = FindFirstObjectByType<AlignmentManager>();

        if (followTarget == null)
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
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
        NetworkManager nm = NetworkManager.Singleton;

        if (networkStatusText)
        {
            string status;
            if (nm == null) status = "⚠ No NetworkManager";
            else if (nm.IsServer && nm.IsListening)
                status = $"✅ Hosting ({SharedARNetworkManager.Instance?.LocalIp})";
            else if (nm.IsConnectedClient) status = "✅ Connected (Client)";
            else if (nm.IsClient) status = "⏳ Connecting...";
            else status = "⏳ Offline (X = host, Y = join)";

            networkStatusText.text = $"Network: {status}";
        }

        if (anchorStatusText)
        {
            string state = "⏳ Waiting for QR...";
            if (alignmentManager != null && alignmentManager.IsAligned)
                state = alignmentManager.IsLocked ? "✅ Aligned (locked)" : "✅ Aligned";
            anchorStatusText.text = $"Anchor: {state}";
        }

        if (playerCountText)
        {
            if (Time.time >= nextCountTime)
            {
                nextCountTime = Time.time + 0.5f;
                playerCount = FindObjectsByType<NetworkedPlayerPose>(FindObjectsSortMode.None).Length;
            }
            playerCountText.text = $"Players: {playerCount} / 2";
        }
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

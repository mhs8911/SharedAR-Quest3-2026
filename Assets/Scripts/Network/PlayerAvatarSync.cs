// Scripts/Network/PlayerAvatarSync.cs
// Role: Attached to the PlayerAvatar prefab (which has a PhotonView).
//       Streams head + hand transforms across the network so each
//       user sees a simple representation of the other player.
//       Position is in shared-anchor space, so it lines up correctly.

using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerAvatarSync : MonoBehaviourPun, IPunObservable
{
    [Header("Local Source Transforms (assign for local player)")]
    [SerializeField] private Transform headSource;
    [SerializeField] private Transform leftHandSource;
    [SerializeField] private Transform rightHandSource;

    [Header("Remote Visual Targets (assign for remote representation)")]
    [SerializeField] private Transform headVisual;
    [SerializeField] private Transform leftHandVisual;
    [SerializeField] private Transform rightHandVisual;

    // Smoothing for remote player motion
    private Vector3    _remoteHeadPos,  _remoteLeftPos,  _remoteRightPos;
    private Quaternion _remoteHeadRot,  _remoteLeftRot,  _remoteRightRot;
    private const float SMOOTH = 12f;

    private void Awake()
    {
        // If this is the local player, hide the visual representation
        // If remote, hide the source (no OVR rig here)
        if (photonView.IsMine)
        {
            if (headVisual)      headVisual.gameObject.SetActive(false);
            if (leftHandVisual)  leftHandVisual.gameObject.SetActive(false);
            if (rightHandVisual) rightHandVisual.gameObject.SetActive(false);

            // Auto-bind to OVRCameraRig transforms if not set in Inspector
            AutoBindLocalSources();
        }
        else
        {
            if (headSource)      headSource.gameObject.SetActive(false);
            if (leftHandSource)  leftHandSource.gameObject.SetActive(false);
            if (rightHandSource) rightHandSource.gameObject.SetActive(false);
        }
    }

    private void AutoBindLocalSources()
    {
        // Finds OVRCameraRig in scene and pulls the correct transforms
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig == null) return;

        if (headSource      == null) headSource      = rig.centerEyeAnchor;
        if (leftHandSource  == null) leftHandSource  = rig.leftHandAnchor;
        if (rightHandSource == null) rightHandSource = rig.rightHandAnchor;
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            // Smoothly interpolate remote player visuals
            if (headVisual)
            {
                headVisual.position = Vector3.Lerp(headVisual.position, _remoteHeadPos, Time.deltaTime * SMOOTH);
                headVisual.rotation = Quaternion.Slerp(headVisual.rotation, _remoteHeadRot, Time.deltaTime * SMOOTH);
            }
            if (leftHandVisual)
            {
                leftHandVisual.position = Vector3.Lerp(leftHandVisual.position, _remoteLeftPos, Time.deltaTime * SMOOTH);
                leftHandVisual.rotation = Quaternion.Slerp(leftHandVisual.rotation, _remoteLeftRot, Time.deltaTime * SMOOTH);
            }
            if (rightHandVisual)
            {
                rightHandVisual.position = Vector3.Lerp(rightHandVisual.position, _remoteRightPos, Time.deltaTime * SMOOTH);
                rightHandVisual.rotation = Quaternion.Slerp(rightHandVisual.rotation, _remoteRightRot, Time.deltaTime * SMOOTH);
            }
        }
    }

    // ── IPunObservable ───────────────────────────────────────────────────────

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send local transforms
            stream.SendNext(headSource      ? headSource.position      : Vector3.zero);
            stream.SendNext(headSource      ? headSource.rotation      : Quaternion.identity);
            stream.SendNext(leftHandSource  ? leftHandSource.position  : Vector3.zero);
            stream.SendNext(leftHandSource  ? leftHandSource.rotation  : Quaternion.identity);
            stream.SendNext(rightHandSource ? rightHandSource.position : Vector3.zero);
            stream.SendNext(rightHandSource ? rightHandSource.rotation : Quaternion.identity);
        }
        else
        {
            // Receive remote transforms
            _remoteHeadPos   = (Vector3)stream.ReceiveNext();
            _remoteHeadRot   = (Quaternion)stream.ReceiveNext();
            _remoteLeftPos   = (Vector3)stream.ReceiveNext();
            _remoteLeftRot   = (Quaternion)stream.ReceiveNext();
            _remoteRightPos  = (Vector3)stream.ReceiveNext();
            _remoteRightRot  = (Quaternion)stream.ReceiveNext();
        }
    }
}

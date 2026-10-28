// Scripts/Objects/NetworkedSharedObject.cs
// Role: Attached to the SharedObject prefab.
//       Syncs position/rotation across the network continuously.
//       Also supports grab-and-move: whoever grabs it becomes the owner
//       and streams its transform; others smoothly follow.

using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkedSharedObject : MonoBehaviourPun, IPunObservable
{
    [Header("Sync Settings")]
    [SerializeField] private float positionLerpSpeed = 15f;
    [SerializeField] private float rotationLerpSpeed = 15f;

    private Vector3    _networkPos;
    private Quaternion _networkRot;
    private bool       _isBeingGrabbed = false;

    // OVR Interaction: hook these to your grab events in the Inspector
    // e.g. GrabInteractable.WhenPointerEventRaised → OnGrabStart/End
    public void OnGrabStart()
    {
        // Request ownership so THIS client streams position
        photonView.RequestOwnership();
        _isBeingGrabbed = true;
    }

    public void OnGrabEnd()
    {
        _isBeingGrabbed = false;
    }

    private void Awake()
    {
        _networkPos = transform.position;
        _networkRot = transform.rotation;

        var rb = GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = true;
    }

    private void Update()
    {
        // Only the owner (grabber or spawner) drives position;
        // all others interpolate toward the received network transform
        if (!photonView.IsMine)
        {
            transform.position = Vector3.Lerp(
                transform.position, _networkPos, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, _networkRot, Time.deltaTime * rotationLerpSpeed);
        }
    }

    // ── IPunObservable ───────────────────────────────────────────────────────

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(_isBeingGrabbed);
        }
        else
        {
            _networkPos      = (Vector3)stream.ReceiveNext();
            _networkRot      = (Quaternion)stream.ReceiveNext();
            _isBeingGrabbed  = (bool)stream.ReceiveNext();
        }
    }
}

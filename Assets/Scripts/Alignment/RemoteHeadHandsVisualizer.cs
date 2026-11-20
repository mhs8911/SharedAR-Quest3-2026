// ============================================================================
// Mitsuki Original Code  |  Scripts/Alignment/RemoteHeadHandsVisualizer.cs
// Original mapping kept. Can now read poses from the network (NetworkedPlayerPose).
// ============================================================================
using UnityEngine;

public class RemoteHeadHandsVisualizer : MonoBehaviour
{
    // ORIGINAL (kept): now OPTIONAL. Local loopback source for Editor tests.
    public SharedPlayerPoseProvider poseProvider;

    // ORIGINAL (kept): now an OPTIONAL override. If empty, alignmentManager.sharedSpace
    // is used (what AR and VR users both do over the network).
    public Transform vrSharedSpace;

    // [ADDED] Network pose source. Auto-found on the PlayerAvatar prefab.
    public NetworkedPlayerPose networkPose;

    // [ADDED] Converts shared → this device's world.
    public AlignmentManager alignmentManager;

    // [ADDED]
    [Range(0f, 60f)] public float smoothing = 25f;   // 0 = no smoothing
    public bool hideWhenOwner = true;                // don't draw your own avatar
    public bool autoCreatePrimitives = true;         // capsule + spheres if targets are empty

    public Transform remoteHead;
    public Transform remoteLeftHand;
    public Transform remoteRightHand;

    // [ADDED]
    private bool snapNext = true;
    private bool visible = true;

    // [ADDED]
    void Awake()
    {
        if (networkPose == null)
            networkPose = GetComponentInParent<NetworkedPlayerPose>();

        if (autoCreatePrimitives)
            CreatePrimitivesIfMissing();
    }

    // [ADDED]
    void Start()
    {
        if (alignmentManager == null && vrSharedSpace == null)
            alignmentManager = FindFirstObjectByType<AlignmentManager>();
    }

    void LateUpdate()
    {
        // ORIGINAL: if (poseProvider == null || vrSharedSpace == null)
        //               return;
        // [CHANGED] a pose source and a target space are still required, but either can
        // now come from the network / alignment manager.

        // [ADDED] never draw the local player's own avatar
        if (hideWhenOwner && networkPose != null && networkPose.IsSpawned && networkPose.IsOwner)
        {
            SetVisible(false);
            return;
        }

        // ORIGINAL: if (!poseProvider.TryGetSharedPlayerPose(out SharedPlayerPose pose))
        //               return;
        // [CHANGED] goes through TryGetPose (network first, local provider second)
        if (!TryGetPose(out SharedPlayerPose pose))
        {
            SetVisible(false);
            snapNext = true;
            return;
        }

        // [ADDED] wait until there is a space to map into
        bool haveSpace = vrSharedSpace != null
                         || (alignmentManager != null && alignmentManager.IsAligned);
        if (!haveSpace)
        {
            SetVisible(false);
            snapNext = true;
            return;
        }

        SetVisible(true);

        // Shared poses -> local world poses   (ORIGINAL comment said "VR world poses")
        ApplyPose(remoteHead, pose.head);
        ApplyPose(remoteLeftHand, pose.leftHand);
        ApplyPose(remoteRightHand, pose.rightHand);

        snapNext = false;   // [ADDED]
    }

    // [ADDED]
    private bool TryGetPose(out SharedPlayerPose pose)
    {
        if (networkPose != null)
            return networkPose.TryGetSharedPlayerPose(out pose);

        if (poseProvider != null)
            return poseProvider.TryGetSharedPlayerPose(out pose);

        pose = default;
        return false;
    }

    void ApplyPose(Transform target, Pose sharedPose)
    {
        if (target == null)
            return;

        // ORIGINAL:
        // target.SetPositionAndRotation(
        //     vrSharedSpace.TransformPoint(sharedPose.position),
        //     vrSharedSpace.rotation * sharedPose.rotation
        // );
        // [CHANGED] same math. Uses vrSharedSpace if assigned, otherwise the
        // AlignmentManager conversion. Optional smoothing.
        Vector3 worldPos;
        Quaternion worldRot;

        if (vrSharedSpace != null)
        {
            worldPos = vrSharedSpace.TransformPoint(sharedPose.position);
            worldRot = vrSharedSpace.rotation * sharedPose.rotation;
        }
        else
        {
            worldPos = alignmentManager.SharedToWorldPosition(sharedPose.position);
            worldRot = alignmentManager.SharedToWorldRotation(sharedPose.rotation);
        }

        if (snapNext || smoothing <= 0f)
        {
            target.SetPositionAndRotation(worldPos, worldRot);
        }
        else
        {
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            target.SetPositionAndRotation(
                Vector3.Lerp(target.position, worldPos, k),
                Quaternion.Slerp(target.rotation, worldRot, k));
        }
    }

    // [ADDED]
    private void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;
        if (remoteHead) remoteHead.gameObject.SetActive(value);
        if (remoteLeftHand) remoteLeftHand.gameObject.SetActive(value);
        if (remoteRightHand) remoteRightHand.gameObject.SetActive(value);
    }

    // [ADDED] Same idea as the primitives your old PlayerAvatarSync created in code.
    private void CreatePrimitivesIfMissing()
    {
        if (remoteHead == null)
            remoteHead = MakePrimitive(PrimitiveType.Capsule, "RemoteHead", new Vector3(0.2f, 0.25f, 0.2f));
        if (remoteLeftHand == null)
            remoteLeftHand = MakePrimitive(PrimitiveType.Sphere, "RemoteLeftHand", Vector3.one * 0.08f);
        if (remoteRightHand == null)
            remoteRightHand = MakePrimitive(PrimitiveType.Sphere, "RemoteRightHand", Vector3.one * 0.08f);
    }

    private Transform MakePrimitive(PrimitiveType type, string objName, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = objName;
        go.transform.SetParent(transform, false);
        go.transform.localScale = scale;
        Collider c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
        return go.transform;
    }
}

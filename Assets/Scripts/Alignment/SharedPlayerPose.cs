// ============================================================================
// Mitsuki Original Code  |  Scripts/Alignment/SharedPlayerPose.cs
// Fields unchanged. Now network-serializable so it can live in a NetworkVariable.
// ============================================================================
using System;
using Unity.Netcode;                // [ADDED]
using UnityEngine;

[Serializable]
// [CHANGED] original: public struct SharedPlayerPose
public struct SharedPlayerPose : INetworkSerializable, IEquatable<SharedPlayerPose>
{
    public Pose head;
    public Pose leftHand;
    public Pose rightHand;

    // [ADDED] NGO serialization. Pose is two public fields, sent directly.
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref head.position);
        serializer.SerializeValue(ref head.rotation);
        serializer.SerializeValue(ref leftHand.position);
        serializer.SerializeValue(ref leftHand.rotation);
        serializer.SerializeValue(ref rightHand.position);
        serializer.SerializeValue(ref rightHand.rotation);
    }

    // [ADDED] NetworkVariable uses equality to detect changes.
    public bool Equals(SharedPlayerPose other)
    {
        return PoseEquals(head, other.head)
            && PoseEquals(leftHand, other.leftHand)
            && PoseEquals(rightHand, other.rightHand);
    }

    public override bool Equals(object obj) => obj is SharedPlayerPose other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(head.position, head.rotation,
                         leftHand.position, leftHand.rotation,
                         rightHand.position, rightHand.rotation);

    private static bool PoseEquals(Pose a, Pose b) =>
        a.position.Equals(b.position) && a.rotation.Equals(b.rotation);
}

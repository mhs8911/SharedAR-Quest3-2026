// ============================================================================
// Mariam code addition
// File: Scripts/Alignment/NetPose.cs
// NEW. A small serializable pose for single-object sync (used by MarkerSpaceTransform).
// ============================================================================
using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct NetPose : INetworkSerializable, IEquatable<NetPose>
{
    public Vector3 position;
    public Quaternion rotation;

    public NetPose(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    // default(NetPose) has a zero quaternion, so treat that as "no data yet"
    public bool IsValid =>
        rotation.x * rotation.x + rotation.y * rotation.y +
        rotation.z * rotation.z + rotation.w * rotation.w > 0.5f;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
    }

    public bool Equals(NetPose other) =>
        position.Equals(other.position) && rotation.Equals(other.rotation);

    public override bool Equals(object obj) => obj is NetPose other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(position, rotation);
}

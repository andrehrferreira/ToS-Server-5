// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct UpdateEntityQuantizedPacket: INetworkPacket
{
    public int Size => 33;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPackets.UpdateEntityQuantized);
        buffer.Write(EntityId);
        buffer.Write(QuantizedX);
        buffer.Write(QuantizedY);
        buffer.Write(QuantizedZ);
        buffer.Write(QuadrantX);
        buffer.Write(QuadrantY);
        buffer.Write(Yaw);
        buffer.Write(Velocity, 0.1f);
        buffer.Write(AnimationState);
        buffer.Write(Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        EntityId = buffer.Read<uint>();
        QuantizedX = buffer.Read<short>();
        QuantizedY = buffer.Read<short>();
        QuantizedZ = buffer.Read<short>();
        QuadrantX = buffer.Read<short>();
        QuadrantY = buffer.Read<short>();
        Yaw = buffer.Read<float>();
        Velocity = buffer.ReadFVector(0.1f);
        AnimationState = buffer.Read<ushort>();
        Flags = buffer.Read<uint>();
    }
}

// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct UpdateEntityPacket: INetworkPacket
{
    public int Size => 29;

    public uint EntityId;
    public FVector Positon;
    public FRotator Rotator;
    public float Speed;
    public ushort AnimationState;
    public uint Flags;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPackets.UpdateEntity);
        buffer.Write(EntityId);
        buffer.Write(Positon, 0.1f);
        buffer.Write(Rotator, 0.1f);
        buffer.Write(Speed);
        buffer.Write(AnimationState);
        buffer.Write(Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        EntityId = buffer.Read<uint>();
        Positon = buffer.ReadFVector(0.1f);
        Rotator = buffer.ReadFRotator(0.1f);
        Speed = buffer.Read<float>();
        AnimationState = buffer.Read<ushort>();
        Flags = buffer.Read<uint>();
    }
}

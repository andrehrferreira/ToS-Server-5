// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct UpdateEntityPacket: INetworkPacket
{
    public int Size => 37;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPacket.UpdateEntity);
        buffer.Write(EntityId);
        buffer.Write(Positon);
        buffer.Write(Rotator);
        buffer.Write(AnimationState);
        buffer.Write(Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        EntityId = buffer.Read<uint>();
        Positon = buffer.Read<FVector>();
        Rotator = buffer.Read<FRotator>();
        AnimationState = buffer.Read<ushort>();
        Flags = buffer.Read<uint>();
    }
}

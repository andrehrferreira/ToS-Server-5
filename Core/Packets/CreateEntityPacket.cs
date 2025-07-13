// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct CreateEntityPacket: INetworkPacket
{
    public int Size => 35;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Reliable);
        buffer.Write((ushort)ServerPacket.CreateEntity);
        buffer.Write(EntityId);
        buffer.Write(Positon);
        buffer.Write(Rotator);
        buffer.Write(Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        EntityId = buffer.Read<uint>();
        Positon = buffer.Read<FVector>();
        Rotator = buffer.Read<FRotator>();
        Flags = buffer.Read<uint>();
    }
}

// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct CreateEntityPacket: INetworkPacket
{
    public int Size => 23;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPackets.CreateEntity);
        buffer.Write(EntityId);
        buffer.Write(Positon, 0.1f);
        buffer.Write(Rotator, 0.1f);
        buffer.Write(Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        EntityId = buffer.Read<uint>();
        Positon = buffer.ReadFVector(0.1f);
        Rotator = buffer.ReadFRotator(0.1f);
        Flags = buffer.Read<uint>();
    }
}

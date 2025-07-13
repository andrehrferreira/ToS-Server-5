// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct RemoveEntityPacket: INetworkPacket
{
    public int Size => 7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Reliable);
        buffer.Write((ushort)ServerPacket.RemoveEntity);
        buffer.Write(EntityId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        EntityId = buffer.ReadUInt();
    }
}

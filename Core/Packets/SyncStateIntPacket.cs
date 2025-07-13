// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct SyncStateIntPacket: INetworkPacket
{
    public int Size => 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Reliable);
        buffer.Write((ushort)ServerPacket.SyncStateInt);
    }
}

// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct PingPacket: INetworkPacket
{
    public int Size => 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Ping);
        buffer.Write(SentTimestamp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        SentTimestamp = buffer.Read<ushort>();
    }
}

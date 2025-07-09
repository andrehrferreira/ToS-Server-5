// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct PingPacket: INetworkPacket
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Reset();
        buffer.Write(PacketType.Ping);
        buffer.Write(SentTimestamp);
    }
}

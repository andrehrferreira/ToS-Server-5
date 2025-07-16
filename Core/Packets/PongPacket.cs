// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct PongPacket: INetworkPacketRecive
{
    public int Size => 3;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        SentTimestamp = buffer.Read<ushort>();
    }
}

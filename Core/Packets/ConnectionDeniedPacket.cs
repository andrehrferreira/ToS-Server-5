// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct ConnectionDeniedPacket: INetworkPacket
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Reset();

        buffer.Write(PacketType.ConnectionDenied);
    }
}

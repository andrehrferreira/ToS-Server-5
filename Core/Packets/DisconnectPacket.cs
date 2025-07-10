// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct DisconnectPacket: INetworkPacket
{
    public int Size => 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Disconnect);
    }
}

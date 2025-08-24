// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct ReliableHandshakePacket: INetworkPacket
{
    public int Size => 3600;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.ReliableHandshake);
        buffer.WriteUtf8String(Message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Message = buffer.ReadUtf8String();
    }
}

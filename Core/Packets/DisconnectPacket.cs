// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class DisconnectPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Serialize(object? data = null)
    {
        Serialize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize()
    {
        ByteBuffer.WritePacketType(_buffer, 0, PacketType.Disconnect);
    }
}

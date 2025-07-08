// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class PingPacket: Packet
{
    public PingPacket()
    {
        _buffer = new byte[9];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Serialize(object? data = null)
    {
        var typedData = data is Ping p ? p : throw new InvalidCastException("Invalid data type.");
        Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(Ping data)
    {
        uint offset = 0;
        offset = ByteBuffer.WritePacketType(_buffer, offset, PacketType.Ping);
        offset = ByteBuffer.WriteLong(_buffer, offset, data.SentTimestamp);
    }
}

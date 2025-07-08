// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;
using System.Buffers;

public class ConnectionAcceptedPacket: Packet
{
    public ConnectionAcceptedPacket()
    {
        _buffer = new byte[5];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Serialize(object? data = null)
    {
        var typedData = data is ConnectionAccepted p ? p : throw new InvalidCastException("Invalid data type.");
        Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ConnectionAccepted data)
    {
        uint offset = 0;
        offset = ByteBuffer.WritePacketType(_buffer, offset, PacketType.ConnectionAccepted);
        offset = ByteBuffer.WriteUInt(_buffer, offset, data.Id);
    }
}

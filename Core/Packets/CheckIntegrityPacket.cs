// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;
using System.Buffers;

public class CheckIntegrityPacket: Packet
{
    public CheckIntegrityPacket()
    {
        _buffer = new byte[7];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Serialize(object? data = null)
    {
        var typedData = data is CheckIntegrity p ? p : throw new InvalidCastException("Invalid data type.");
        Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(CheckIntegrity data)
    {
        uint offset = 0;
        offset = ByteBuffer.WritePacketType(_buffer, offset, PacketType.CheckIntegrity);
        offset = ByteBuffer.WriteUShort(_buffer, offset, data.Index);
        offset = ByteBuffer.WriteUInt(_buffer, offset, data.Version);
    }
}

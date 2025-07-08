// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class CheckIntegrityPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] Serialize(object? data = null)
    {
        var typedData = data is CheckIntegrity p ? p : throw new InvalidCastException("Invalid data type.");
        return Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] Serialize(CheckIntegrity data)
    {
        uint offset = 0;
        byte[] buffer = new byte[3600];
        offset = ByteBuffer.WritePacketType(buffer, offset, PacketType.CheckIntegrity);
        offset = ByteBuffer.WriteUShort(buffer, offset, data.Index);
        offset = ByteBuffer.WriteUInt(buffer, offset, data.Version);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , CheckIntegrity data, Entity entity)
    {
        var buffer = Serialize(data);

        if (EntitySocketMap.TryGet(entity.Id, out var socket))
             socket.Send(buffer);
    }
}

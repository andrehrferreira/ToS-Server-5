// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class PingPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] Serialize(object? data = null)
    {
        var typedData = data is Ping p ? p : throw new InvalidCastException("Invalid data type.");
        return Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] Serialize(Ping data)
    {
        uint offset = 0;
        byte[] buffer = new byte[3600];
        offset = ByteBuffer.WritePacketType(buffer, offset, PacketType.Ping);
        offset = ByteBuffer.WriteLong(buffer, offset, data.SentTimestamp);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , Ping data, Entity entity)
    {
        var buffer = Serialize(data);
        QueueBuffer.AddBuffer(entity.Id, buffer);
    }
}

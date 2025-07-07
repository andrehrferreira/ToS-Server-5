// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class PingPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ByteBuffer Serialize(object? data = null)
    {
        var typedData = data is Ping p ? p : throw new InvalidCastException("Invalid data type.");
        return Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Serialize(Ping data)
    {
        var buffer = ByteBuffer.CreateEmptyBuffer();
        buffer.Write(PacketType.Ping);
        buffer.Write(data.SentTimestamp);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , Ping data, Entity entity)
    {
        var buffer = Serialize(data);
        QueueBuffer.AddBuffer(entity.Id, buffer);
    }
}

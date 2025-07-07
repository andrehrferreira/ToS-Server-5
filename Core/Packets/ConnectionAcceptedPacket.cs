// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class ConnectionAcceptedPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ByteBuffer Serialize(object? data = null)
    {
        var typedData = data is ConnectionAccepted p ? p : throw new InvalidCastException("Invalid data type.");
        return Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Serialize(ConnectionAccepted data)
    {
        var buffer = ByteBuffer.CreateEmptyBuffer();
        buffer.Write(PacketType.ConnectionAccepted);
        buffer.Write(data.Id);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , ConnectionAccepted data, Entity entity)
    {
        var buffer = Serialize(data);

        if (EntitySocketMap.TryGet(entity.Id, out var socket))
             socket.Send(buffer);
    }
}

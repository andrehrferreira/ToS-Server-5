// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class ConnectionAcceptedPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override NativeBuffer Serialize(object? data = null)
    {
        var typedData = data is ConnectionAccepted p ? p : throw new InvalidCastException("Invalid data type.");
        return Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe NativeBuffer Serialize(ConnectionAccepted data)
    {
        uint offset = 0;
        byte* buffer = (byte*)NativeMemory.Alloc(3600);
        offset = ByteBuffer.WritePacketType(buffer, offset, PacketType.ConnectionAccepted);
        offset = ByteBuffer.WriteUInt(buffer, offset, data.Id);
        return new NativeBuffer(buffer, 3600);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , ConnectionAccepted data, Entity entity)
    {
        var buffer = Serialize(data);

        if (EntitySocketMap.TryGet(entity.Id, out var socket))
             socket.Send(buffer);
    }
}

// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class ConnectionDeniedPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] Serialize(object? data = null)
    {
        return Serialize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] Serialize()
    {
        byte[] buffer = new byte[1];
        ByteBuffer.WritePacketType(buffer, 0, PacketType.ConnectionDenied);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , ConnectionDenied data, Entity entity)
    {
        var buffer = Serialize();

        if (EntitySocketMap.TryGet(entity.Id, out var socket))
             socket.Send(buffer);
    }
}

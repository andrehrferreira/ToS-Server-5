// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class ConnectionDeniedPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override NativeBuffer Serialize(object? data = null)
    {
        return Serialize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe NativeBuffer Serialize()
    {
        byte* buffer = (byte*)NativeMemory.Alloc(1);
        ByteBuffer.WritePacketType(buffer, 0, PacketType.ConnectionDenied);
        return new NativeBuffer(buffer, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , ConnectionDenied data, Entity entity)
    {
        var buffer = Serialize();

        if (EntitySocketMap.TryGet(entity.Id, out var socket))
             socket.Send(buffer);
    }
}

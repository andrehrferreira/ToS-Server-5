// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class RemoveEntityPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ByteBuffer Serialize(object? data = null)
    {
        return Serialize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Serialize()
    {
        var buffer = ByteBuffer.CreateEmptyBuffer();
        buffer.Reliable = true;
        buffer.Write((byte)ServerPacket.RemoveEntity);
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , RemoveEntity data, Entity entity)
    {
        var buffer = Serialize();

        if (EntitySocketMap.TryGet(entity.Id, out var socket))
             socket.Send(buffer);
    }
}

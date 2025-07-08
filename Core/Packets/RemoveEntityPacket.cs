// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class RemoveEntityPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] Serialize(object? data = null)
    {
        return Serialize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe NativeBuffer Serialize()
    {
        unsafe byte* buffer = (byte*)NativeMemory.Alloc(2);
        ByteBuffer.WriteUShort(buffer, 0, (ushort)ServerPacket.RemoveEntity);
        return new NativeBuffer(buffer, 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , RemoveEntity data, Entity entity)
    {
        var buffer = Serialize();

        if (EntitySocketMap.TryGet(entity.Id, out var socket))
             socket.Send(buffer);
    }
}

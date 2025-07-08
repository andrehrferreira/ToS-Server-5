// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class CheckIntegrityPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override NativeBuffer Serialize(object? data = null)
    {
        var typedData = data is CheckIntegrity p ? p : throw new InvalidCastException("Invalid data type.");
        return Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe NativeBuffer Serialize(CheckIntegrity data)
    {
        uint offset = 0;
        byte* buffer = (byte*)NativeMemory.Alloc(3600);
        offset = ByteBuffer.WritePacketType(buffer, offset, PacketType.CheckIntegrity);
        offset = ByteBuffer.WriteUShort(buffer, offset, data.Index);
        offset = ByteBuffer.WriteUInt(buffer, offset, data.Version);
        return new NativeBuffer(buffer, 3600);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(Entity owner , CheckIntegrity data, Entity entity)
    {
        var buffer = Serialize(data);

        if (EntitySocketMap.TryGet(entity.Id, out var socket))
             socket.Send(buffer);
    }
}

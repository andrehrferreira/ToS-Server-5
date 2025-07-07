// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class CheckIntegrityPacket: Packet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ByteBuffer Serialize(object? data = null)
    {
        var typedData = data is CheckIntegrity p ? p : throw new InvalidCastException("Invalid data type.");
        return Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Serialize(CheckIntegrity data)
    {
        var buffer = ByteBuffer.CreateEmptyBuffer();
        buffer.Reliable = true;
        buffer.Write((byte)ServerPacket.CheckIntegrity);
        buffer.Write(data.Index);
        buffer.Write(data.Version);
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

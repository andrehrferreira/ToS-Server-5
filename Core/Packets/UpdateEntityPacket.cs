// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;
using System.Buffers;

public class UpdateEntityPacket: Packet
{
    public UpdateEntityPacket()
    {
        _buffer = new byte[9];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Serialize(object? data = null)
    {
        Serialize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize()
    {
        ByteBuffer.WriteUShort(_buffer, 0, (ushort)ServerPacket.UpdateEntity);
    }
}

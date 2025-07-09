// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class SyncStateIntPacket: Packet
{
    public SyncStateIntPacket()
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
        ByteBuffer.WriteUShort(_buffer, 0, (ushort)ServerPacket.SyncStateInt);
    }
}

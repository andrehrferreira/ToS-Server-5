using System.Runtime.CompilerServices;

public struct PingPacket : IPacketServer
{
    public ushort SentTimestamp;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref ByteBuffer buffer)
    {
        buffer.Write(SentTimestamp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer ToBuffer()
    {
        var buffer = new ByteBuffer();
        Serialize(ref buffer);
        return buffer;
    }
}
using System.Runtime.CompilerServices;

public struct PingPacket : IPacketServer
{
    public ushort SentTimestamp;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref TinyBuffer buffer)
    {
        buffer.Write(SentTimestamp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TinyBuffer ToBuffer()
    {
        var buffer = new TinyBuffer();
        Serialize(ref buffer);
        return buffer;
    }
}
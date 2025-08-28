using System.Runtime.CompilerServices;

public struct PongPacket : IPacketClient
{
    public ushort SentTimestamp;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref TinyBuffer buffer)
    {
        SentTimestamp = buffer.Read<ushort>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PongPacket FromBuffer(ref TinyBuffer buffer)
    {
        var packet = new PongPacket();
        packet.Deserialize(ref buffer);
        return packet;
    }
}
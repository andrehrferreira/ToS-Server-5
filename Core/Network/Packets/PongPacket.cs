using System.Runtime.CompilerServices;

namespace Wormhole.Packets
{
    public struct PongPacket : IPacketClient
    {
        public ushort SentTimestamp;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deserialize(ref ByteBuffer buffer)
        {
            SentTimestamp = buffer.Read<ushort>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PongPacket FromBuffer(ref ByteBuffer buffer)
        {
            var packet = new PongPacket();
            packet.Deserialize(ref buffer);
            return packet;
        }
    }
}
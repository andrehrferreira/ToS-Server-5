
using System.Diagnostics;

namespace Packets.Handler
{
    public class PongHandler : PacketHandler
    {
        public override PacketType Type => PacketType.Pong;

        public override void Consume(TinyBuffer buffer, PacketPack packet, UDPSocket connection)
        {
            var pongPacket = PongPacket.FromBuffer(ref buffer);
            ushort nowMs = (ushort)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000) % 65536);
            ushort rttMs = (ushort)((nowMs - pongPacket.SentTimestamp) & 0xFFFF);

            connection.Ping = rttMs;
            connection.TimeoutLeft = 30f;
        }
    }
}

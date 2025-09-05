
using System.Diagnostics;
using Wormhole.Packets;

namespace Wormhole.Handlers
{
    public class PongHandler : PacketHandler
    {
        public override PacketType Type => PacketType.Pong;

        public override void Consume(Messenger msg, Connection conn)
        {
            var pongPacket = PongPacket.FromBuffer(ref msg.Payload);
            ushort nowMs = (ushort)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000) % 65536);
            ushort rttMs = (ushort)((nowMs - pongPacket.SentTimestamp) & 0xFFFF);

            conn.Ping = rttMs;
            conn.TimeoutLeft = 30f;
        }
    }
}

using System.Runtime.CompilerServices;

namespace Wormhole.Packets
{
    public struct RetryTokenPacket : IPacketServer
    {
        public byte[] Token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref ByteBuffer buffer)
        {
            buffer.WriteBytes(Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteBuffer ToBuffer()
        {
            var buffer = new ByteBuffer();
            Serialize(ref buffer);
            return buffer;
        }
    }
}
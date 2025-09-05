using System.Runtime.CompilerServices;

namespace Wormhole.Packets
{
    public struct ConnectionAcceptedPacket : IPacketServer
    {
        public uint Id;
        public byte[] ServerPublicKey;
        public byte[] Salt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref ByteBuffer buffer)
        {
            buffer.Write(Id);
            buffer.WriteBytes(ServerPublicKey);
            buffer.WriteBytes(Salt);
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
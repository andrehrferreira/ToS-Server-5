/*
* Packaging
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using NanoSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wormhole
{
    public unsafe struct Messenger : IDisposable
    {
        public PacketFlags Flags;
        public PacketType Type;
        public uint Sequence;
        public ByteBuffer Payload;
        public Address Address;

        private bool _disposed;

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int sequenceLength = (Type == PacketType.Reliable) ? 4 : 0;
                return 1 + 1 + sequenceLength + (int)Payload.Length + 4;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Free();
                _disposed = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* Pack()
        {
            int sequenceLength = (Type == PacketType.Reliable) ? 4 : 0;
            int totalSize = 1 + 1 + sequenceLength + (int)Payload.Length + 4;
            byte* buffer = (byte*)Marshal.AllocHGlobal(totalSize);
            int offset = 0;

            buffer[offset++] = (byte)Flags;
            buffer[offset++] = (byte)Type;

            if (Type == PacketType.Reliable)
            {
                *(uint*)(buffer + offset) = Sequence;
                offset += sizeof(uint);
            }

            if (Payload.Length > 0)
            {
                Buffer.MemoryCopy(Payload._ptr, buffer + offset, Payload.Length, Payload.Length);
                offset += (int)Payload.Length;
            }

            uint crc = CRC32C.Compute(buffer, offset);
            *(uint*)(buffer + offset) = crc;

            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Messenger Unpack(byte* buffer, int length, Address address)
        {
            if (length < 1 + 1 + 4)
                throw new ArgumentException("Buffer too small to contain a valid Packaging.");

            int offset = 0;
            var flags = (PacketFlags)buffer[offset++];
            var type = (PacketType)buffer[offset++];

            uint sequence = 0;
            if (type == PacketType.Reliable)
            {
                sequence = *(uint*)(buffer + offset);
                offset += sizeof(uint);
            }

            int payloadLength = length - offset - sizeof(uint);

            if (payloadLength < 0)
                throw new ArgumentException("Invalid payload size.");

            uint expectedCrc = *(uint*)(buffer + length - 4);
            uint actualCrc = CRC32C.Compute(buffer, length - 4);

            if (expectedCrc != actualCrc)
                throw new InvalidOperationException("CRC32 validation failed.");

            var payload = ByteBuffer.From(buffer, (uint)payloadLength);

            return new Messenger
            {
                Flags = flags,
                Type = type,
                Sequence = sequence,
                Payload = payload,
                Address = address
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpackSecurityCompress(Connection connection)
        {
            if ((Flags & PacketFlags.Compressed) != 0 || (Flags & PacketFlags.Encrypted) != 0)
                connection.ParseSecurePacket(Flags, ref Payload);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free()
        {
            Payload.Free();
        }
    }
}

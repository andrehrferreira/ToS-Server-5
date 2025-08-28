using NanoSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe struct PacketPack : IDisposable
{
    public PacketHeader Header;
    public PacketType Type;
    public uint Sequence;
    public TinyBuffer Payload;
    public Address Address;

    private bool _disposed;

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int sequenceLength = (Type == PacketType.Reliable) ? 4 : 0;
            return 1 + 1 + sequenceLength + Payload.Position + 4;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* Pack()
    {
        int sequenceLength = (Type == PacketType.Reliable) ? 4 : 0;
        int totalSize = 1 + 1 + sequenceLength + Payload.Position + 4;
        byte* buffer = (byte*)Marshal.AllocHGlobal(totalSize);
        int offset = 0;

        buffer[offset++] = (byte)Header;
        buffer[offset++] = (byte)Type;

        if (Type == PacketType.Reliable)
        {
            *(uint*)(buffer + offset) = Sequence;
            offset += sizeof(uint);
        }

        if (Payload.Position > 0)
        {
            Buffer.MemoryCopy(Payload._ptr, buffer + offset, Payload.Position, Payload.Position);
            offset += Payload.Position;
        }

        uint crc = CRC32C.Compute(buffer, offset);
        *(uint*)(buffer + offset) = crc;

        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PacketPack Unpack(byte* buffer, int length, Address address)
    {
        if (length < 1 + 1 + 4)
            throw new ArgumentException("Buffer too small to contain a valid PacketPack.");

        int offset = 0;
        var header = (PacketHeader)buffer[offset++];
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

        var payload = new TinyBuffer(payloadLength);
        payload.Copy(buffer + offset, payloadLength);

        return new PacketPack
        {
            Header = header,
            Type = type,
            Sequence = sequence,
            Payload = payload,
            Address = address
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TinyBuffer UnpackSecurityCompress(UDPSocket connection)
    {
        if ((Header & PacketHeader.Compressed) != 0 && (Header & PacketHeader.Encrypted) != 0)
            return connection.ParseSecurePacket(Header, Payload);
        else
            return Payload;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free()
    {
        Payload.Free();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Free();
            _disposed = true;
        }
    }
}

using System.Runtime.InteropServices;
#if NETCOREAPP3_0_OR_GREATER || NETCOREAPP3_1 || NET5_0
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

public static class CRC32C
{
    public const int ChecksumSize = 4;
    private const uint Poly = 0x82F63B78u;
    private static readonly uint[] Table;

    static CRC32C()
    {
#if NETCOREAPP3_0_OR_GREATER || NETCOREAPP3_1 || NET5_0
        if (Sse42.IsSupported)
            return;
#endif
#if NET5_0_OR_GREATER || NET5_0
        if (Crc32.IsSupported)
            return;
#endif
        Table = new uint[16 * 256];

        for (uint i = 0; i < 256; i++)
        {
            uint res = i;
            for (int t = 0; t < 16; t++)
            {
                for (int k = 0; k < 8; k++)
                    res = (res & 1) == 1 ? Poly ^ (res >> 1) : (res >> 1);
                Table[t * 256 + i] = res;
            }
        }
    }

    public static unsafe uint Compute(byte* input, int length)
    {
        int offset = 0;
        uint crcLocal = uint.MaxValue;

#if NETCOREAPP3_0_OR_GREATER || NETCOREAPP3_1 || NET5_0
        if (Sse42.IsSupported)
        {
            int processed = 0;

            if (Sse42.X64.IsSupported && length > sizeof(ulong))
            {
                processed = length / sizeof(ulong) * sizeof(ulong);
                ulong crclong = crcLocal;

                for (int i = 0; i < processed; i += sizeof(ulong))
                {
                    ulong value = *(ulong*)(input + i);
                    crclong = Sse42.X64.Crc32(crclong, value);
                }

                crcLocal = (uint)crclong;
            }
            else if (length > sizeof(uint))
            {
                processed = length / sizeof(uint) * sizeof(uint);

                for (int i = 0; i < processed; i += sizeof(uint))
                {
                    uint value = *(uint*)(input + i);
                    crcLocal = Sse42.Crc32(crcLocal, value);
                }
            }

            for (int i = processed; i < length; i++)
                crcLocal = Sse42.Crc32(crcLocal, input[i]);

            return crcLocal ^ uint.MaxValue;
        }
#endif

#if NET5_0_OR_GREATER || NET5_0
        if (Crc32.IsSupported)
        {
            int processed = 0;

            if (Crc32.Arm64.IsSupported && length > sizeof(ulong))
            {
                processed = length / sizeof(ulong) * sizeof(ulong);
                for (int i = 0; i < processed; i += sizeof(ulong))
                {
                    ulong value = *(ulong*)(input + i);
                    crcLocal = Crc32.Arm64.ComputeCrc32C(crcLocal, value);
                }
            }
            else if (length > sizeof(uint))
            {
                processed = length / sizeof(uint) * sizeof(uint);
                for (int i = 0; i < processed; i += sizeof(uint))
                {
                    uint value = *(uint*)(input + i);
                    crcLocal = Crc32.ComputeCrc32C(crcLocal, value);
                }
            }

            for (int i = processed; i < length; i++)
                crcLocal = Crc32.ComputeCrc32C(crcLocal, input[i]);

            return crcLocal ^ uint.MaxValue;
        }
#endif

        int remaining = length;

        while (remaining >= 16)
        {
            var a = Table[(3 * 256) + input[offset + 12]]
                    ^ Table[(2 * 256) + input[offset + 13]]
                    ^ Table[(1 * 256) + input[offset + 14]]
                    ^ Table[(0 * 256) + input[offset + 15]];

            var b = Table[(7 * 256) + input[offset + 8]]
                    ^ Table[(6 * 256) + input[offset + 9]]
                    ^ Table[(5 * 256) + input[offset + 10]]
                    ^ Table[(4 * 256) + input[offset + 11]];

            var c = Table[(11 * 256) + input[offset + 4]]
                    ^ Table[(10 * 256) + input[offset + 5]]
                    ^ Table[(9 * 256) + input[offset + 6]]
                    ^ Table[(8 * 256) + input[offset + 7]];

            var d = Table[(15 * 256) + ((byte)crcLocal ^ input[offset])]
                    ^ Table[(14 * 256) + ((byte)(crcLocal >> 8) ^ input[offset + 1])]
                    ^ Table[(13 * 256) + ((byte)(crcLocal >> 16) ^ input[offset + 2])]
                    ^ Table[(12 * 256) + ((crcLocal >> 24) ^ input[offset + 3])];

            crcLocal = d ^ c ^ b ^ a;
            offset += 16;
            remaining -= 16;
        }

        while (remaining-- > 0)
            crcLocal = Table[(byte)(crcLocal ^ input[offset++])] ^ (crcLocal >> 8);

        return crcLocal ^ uint.MaxValue;
    }

    public static uint Compute(byte[] input, int length = 0)
    {
        int offset = 0;

        if (length == 0)
            length = input.Length;

        uint crcLocal = uint.MaxValue;

#if NETCOREAPP3_0_OR_GREATER || NETCOREAPP3_1 || NET5_0
        if (Sse42.IsSupported)
        {
            var data = input.AsSpan(0, length);
            int processed = 0;

            if (Sse42.X64.IsSupported && data.Length > sizeof(ulong))
            {
                processed = data.Length / sizeof(ulong) * sizeof(ulong);
                var ulongs = MemoryMarshal.Cast<byte, ulong>(data.Slice(0, processed));
                ulong crclong = crcLocal;

                for (int i = 0; i < ulongs.Length; i++)
                    crclong = Sse42.X64.Crc32(crclong, ulongs[i]);

                crcLocal = (uint)crclong;
            }
            else if (data.Length > sizeof(uint))
            {
                processed = data.Length / sizeof(uint) * sizeof(uint);
                var uints = MemoryMarshal.Cast<byte, uint>(data.Slice(0, processed));

                for (int i = 0; i < uints.Length; i++)
                    crcLocal = Sse42.Crc32(crcLocal, uints[i]);
            }

            for (int i = processed; i < data.Length; i++)
                crcLocal = Sse42.Crc32(crcLocal, data[i]);

            return crcLocal ^ uint.MaxValue;
        }
#endif

#if NET5_0_OR_GREATER || NET5_0
        if (Crc32.IsSupported)
        {
            var data = input.AsSpan(0, length);
            int processed = 0;

            if (Crc32.Arm64.IsSupported && data.Length > sizeof(ulong))
            {
                processed = data.Length / sizeof(ulong) * sizeof(ulong);
                var ulongs = MemoryMarshal.Cast<byte, ulong>(data.Slice(0, processed));
                for (int i = 0; i < ulongs.Length; i++)
                    crcLocal = Crc32.Arm64.ComputeCrc32C(crcLocal, ulongs[i]);
            }
            else if (data.Length > sizeof(uint))
            {
                processed = data.Length / sizeof(uint) * sizeof(uint);
                var uints = MemoryMarshal.Cast<byte, uint>(data.Slice(0, processed));
                for (int i = 0; i < uints.Length; i++)
                    crcLocal = Crc32.ComputeCrc32C(crcLocal, uints[i]);
            }

            for (int i = processed; i < data.Length; i++)
                crcLocal = Crc32.ComputeCrc32C(crcLocal, data[i]);

            return crcLocal ^ uint.MaxValue;
        }
#endif

        int remaining = length;

        while (remaining >= 16)
        {
            var a = Table[(3 * 256) + input[offset + 12]]
                    ^ Table[(2 * 256) + input[offset + 13]]
                    ^ Table[(1 * 256) + input[offset + 14]]
                    ^ Table[(0 * 256) + input[offset + 15]];

            var b = Table[(7 * 256) + input[offset + 8]]
                    ^ Table[(6 * 256) + input[offset + 9]]
                    ^ Table[(5 * 256) + input[offset + 10]]
                    ^ Table[(4 * 256) + input[offset + 11]];

            var c = Table[(11 * 256) + input[offset + 4]]
                    ^ Table[(10 * 256) + input[offset + 5]]
                    ^ Table[(9 * 256) + input[offset + 6]]
                    ^ Table[(8 * 256) + input[offset + 7]];

            var d = Table[(15 * 256) + ((byte)crcLocal ^ input[offset])]
                    ^ Table[(14 * 256) + ((byte)(crcLocal >> 8) ^ input[offset + 1])]
                    ^ Table[(13 * 256) + ((byte)(crcLocal >> 16) ^ input[offset + 2])]
                    ^ Table[(12 * 256) + ((crcLocal >> 24) ^ input[offset + 3])];

            crcLocal = d ^ c ^ b ^ a;
            offset += 16;
            remaining -= 16;
        }

        while (remaining-- > 0)
            crcLocal = Table[(byte)(crcLocal ^ input[offset++])] ^ (crcLocal >> 8);

        return crcLocal ^ uint.MaxValue;
    }
}

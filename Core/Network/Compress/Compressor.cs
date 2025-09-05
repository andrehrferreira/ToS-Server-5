/*
* Compressor
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Wormhole
{
    public sealed class Compressor
    {
        private Brotli brotli = new Brotli();

        private const byte Magic0 = 0x55;
        private const byte Magic1 = 0x43;
        private const byte Version = 1;

        private const byte FLAG_DELTA = 1 << 0;
        private const byte FLAG_RLE0 = 1 << 1;
        private const byte FLAG_BROTLI = 1 << 2;
        private const byte FLAG_RAW = 1 << 3;

        public int MinSizeToTryCompression { get; init; } = 512;     
        public int BrotliLevel { get; init; } = 2;                    
        public int BrotliWindow { get; init; } = 18;

        private readonly ConcurrentDictionary<ushort, byte[]> _lastByContext = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Compress(ReadOnlySpan<byte> input, ushort contextId = 0)
        {
            var rent = ArrayPool<byte>.Shared.Rent(input.Length + 256);

            try
            {
                var work = rent.AsSpan();

                Span<byte> header = work.Slice(0, 8);
                int cursor = 8;

                byte flags = 0;

                byte[]? last = null;
                bool usedDelta = false;
                if (contextId != 0 && _lastByContext.TryGetValue(contextId, out last) && last != null && last.Length == input.Length)
                {
                    usedDelta = true;
                    flags |= FLAG_DELTA;

                    for (int i = 0; i < input.Length; i++)
                        work[cursor + i] = (byte)(input[i] ^ last[i]);
                }
                else
                {
                    input.CopyTo(work.Slice(cursor, input.Length));
                }

                int lenAfterDelta = input.Length;
                int startAfterDelta = cursor;
                cursor += lenAfterDelta;

                Span<byte> rleDst = work.Slice(cursor, work.Length - cursor);
                int rleWritten = RLE.Encode(work.Slice(startAfterDelta, lenAfterDelta), rleDst);
                bool usedRle = rleWritten < lenAfterDelta; 
                int startAfterRle = usedRle ? cursor : startAfterDelta;
                int lenAfterRle = usedRle ? rleWritten : lenAfterDelta;
                if (usedRle) flags |= FLAG_RLE0;
                cursor = startAfterRle + lenAfterRle;

                ReadOnlySpan<byte> toCompress = work.Slice(startAfterRle, lenAfterRle);
                int brotliStart = cursor;
                int brotliWritten = 0;
                bool usedBrotli = false;

                if (toCompress.Length >= MinSizeToTryCompression)
                {
                    brotliWritten = brotli.Compress(toCompress, work.Slice(brotliStart));
                    usedBrotli = brotliWritten > 0 && brotliWritten < toCompress.Length;
                }

                int payloadStart = usedBrotli ? brotliStart : startAfterRle;
                int payloadLen = usedBrotli ? brotliWritten : lenAfterRle;
                if (usedBrotli) flags |= FLAG_BROTLI;

                if ((flags & (FLAG_DELTA | FLAG_RLE0 | FLAG_BROTLI)) == 0)
                {
                    flags |= FLAG_RAW;
                    payloadStart = startAfterDelta; 
                    payloadLen = lenAfterDelta;
                }

                header[0] = Magic0;
                header[1] = Magic1;
                header[2] = Version;
                header[3] = flags;
                WriteU16(header.Slice(4), (ushort)input.Length);
                WriteU16(header.Slice(6), contextId);

                var totalLen = 8 + payloadLen;
                var output = new byte[totalLen];
                header.CopyTo(output);
                work.Slice(payloadStart, payloadLen).CopyTo(output.AsSpan(8));

                if (contextId != 0)
                {
                    _lastByContext[contextId] = input.ToArray();
                }

                return output;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rent);
            }
        }

        public byte[] Decompress(ReadOnlySpan<byte> packet)
        {
            if (packet.Length < 8) throw new InvalidDataException("Pacote curto.");

            if (packet[0] != Magic0 || packet[1] != Magic1 || packet[2] != Version)
                throw new InvalidDataException("Header inválido.");

            byte flags = packet[3];
            int originalLen = ReadU16(packet.Slice(4));
            ushort contextId = (ushort)ReadU16(packet.Slice(6));

            var payload = packet.Slice(8);
            var rentA = ArrayPool<byte>.Shared.Rent(Math.Max(originalLen * 2, payload.Length * 2 + 256));
            var rentB = ArrayPool<byte>.Shared.Rent(Math.Max(originalLen * 2, payload.Length * 2 + 256));

            try
            {
                Span<byte> a = rentA.AsSpan();
                Span<byte> b = rentB.AsSpan();

                ReadOnlySpan<byte> cur = payload;

                if ((flags & FLAG_BROTLI) != 0)
                {
                    int written = brotli.Decompress(cur, a);
                    if (written <= 0) throw new InvalidDataException("Falha no Brotli.");
                    cur = a.Slice(0, written);
                }

                if ((flags & FLAG_RLE0) != 0)
                {
                    int written = RLE.Decode(cur, b);
                    if (written <= 0) throw new InvalidDataException("Falha no RLE0.");
                    cur = b.Slice(0, written);
                }

                if ((flags & FLAG_DELTA) != 0)
                {
                    if (!_lastByContext.TryGetValue(contextId, out var last) || last == null || last.Length != originalLen)
                        throw new InvalidDataException("Contexto para DELTA ausente ou incompatível.");

                    if (cur.Length != originalLen) throw new InvalidDataException("Tamanho pós-DELTA inválido.");

                    for (int i = 0; i < originalLen; i++)
                        a[i] = (byte)(cur[i] ^ last[i]);

                    cur = a.Slice(0, originalLen);
                }

                if ((flags & (FLAG_DELTA | FLAG_RLE0 | FLAG_BROTLI)) == 0)
                {
                    if (cur.Length != originalLen)
                    {
                        var raw = payload.ToArray();
                        if (raw.Length != originalLen)
                            throw new InvalidDataException("RAW size mismatch.");
                        cur = raw;
                    }
                }

                if (contextId != 0)
                    _lastByContext[contextId] = cur.ToArray();

                var result = new byte[originalLen];
                cur.Slice(0, originalLen).CopyTo(result);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentA);
                ArrayPool<byte>.Shared.Return(rentB);
            }
        }

        private static void WriteU16(Span<byte> dst, ushort v)
        {
            dst[0] = (byte)(v & 0xFF);
            dst[1] = (byte)((v >> 8) & 0xFF);
        }

        private static int ReadU16(ReadOnlySpan<byte> src)
        {
            return src[0] | (src[1] << 8);
        }
    }
}

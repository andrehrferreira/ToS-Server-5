/*
* Brotli
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Wormhole
{
    public class Brotli
    {
        public int MinSizeToTryCompression { get; init; } = 512;     
        public int BrotliLevel { get; init; } = 2;                    
        public int BrotliWindow { get; init; } = 18;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compress(ReadOnlySpan<byte> src, Span<byte> dst)
        {
            try
            {
                using var ms = new MemoryStream();

                using (var bro = new BrotliStream(ms, CompressionLevel.Fastest, leaveOpen: true))
                {
                    if (BrotliEncoder.TryCompress(src, dst, out int written, BrotliLevel, BrotliWindow))
                        return written;

                    bro.Write(src);
                }

                var buf = ms.ToArray();
                if (buf.Length >= dst.Length) return 0;
                buf.AsSpan().CopyTo(dst);
                return buf.Length;
            }
            catch
            {
                return 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Decompress(ReadOnlySpan<byte> src, Span<byte> dst)
        {
            try
            {
                if (BrotliDecoder.TryDecompress(src, dst, out int written))
                    return written;

                using var inMs = new MemoryStream(src.ToArray());
                using var outMs = new MemoryStream(dst.Length);
                using (var bro = new BrotliStream(inMs, CompressionMode.Decompress, leaveOpen: true))
                {
                    bro.CopyTo(outMs);
                }

                var buf = outMs.ToArray();
                if (buf.Length > dst.Length) return 0;
                buf.AsSpan().CopyTo(dst);
                return buf.Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}

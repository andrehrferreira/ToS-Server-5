/*
* RLE
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

namespace Wormhole
{
    public static class RLE
    {
        public static int Encode(ReadOnlySpan<byte> src, Span<byte> dst)
        {
            int si = 0, di = 0;

            while (si < src.Length)
            {
                byte b = src[si];

                if (b == 0)
                {
                    int run = 1;
                    int max = Math.Min(255, src.Length - si);
                    while (run < max && src[si + run] == 0) run++;

                    if (di + 2 > dst.Length) return 0;
                    dst[di++] = 0x00;
                    dst[di++] = (byte)run;
                    si += run;
                }
                else
                {
                    if (b == 0x00)
                    {
                        if (di + 2 > dst.Length) return 0;
                        dst[di++] = 0x00;
                        dst[di++] = 0x00;
                    }
                    else
                    {
                        if (di + 1 > dst.Length) return 0;
                        dst[di++] = b;
                    }
                    si++;
                }
            }

            return di;
        }

        public static int Decode(ReadOnlySpan<byte> src, Span<byte> dst)
        {
            int si = 0, di = 0;

            while (si < src.Length)
            {
                byte b = src[si++];
                if (b == 0x00)
                {
                    if (si >= src.Length) return 0;

                    byte n = src[si++];

                    if (n == 0)
                    {
                        if (di >= dst.Length) return 0;
                        dst[di++] = 0x00;
                    }
                    else
                    {
                        if (di + n > dst.Length) return 0;
                        dst.Slice(di, n).Clear();
                        di += n;
                    }
                }
                else
                {
                    if (di >= dst.Length) return 0;
                    dst[di++] = b;
                }
            }

            return di;
        }
    }
}

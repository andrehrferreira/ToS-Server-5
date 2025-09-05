/*
 * LZ4 Compression
 *
 * Author: Andre Ferreira
 *
 * Copyright (c) Uzmi Games. Licensed under the MIT License.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Runtime.CompilerServices;

namespace Wormhole
{
    public static class LZ4
    {
        private const int MinMatch = 4;
        private const int HashLog = 16;
        private const int HashSize = 1 << HashLog;
        private const uint HashMultiplier = 2654435761u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Hash(uint value)
        {
            return (int)((value * HashMultiplier) >> ((MinMatch * 8) - HashLog));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Compress(byte* src, int srcLength, byte* dst, int dstCapacity)
        {
            uint* hashTable = stackalloc uint[HashSize];

            for (int i = 0; i < HashSize; i++)
                hashTable[i] = 0;

            byte* ip = src;
            byte* anchor = ip;
            byte* iend = src + srcLength;
            byte* mflimit = iend - MinMatch;
            byte* op = dst;
            byte* oend = dst + dstCapacity;

            while (ip < mflimit)
            {
                uint sequence = *(uint*)ip;
                int h = Hash(sequence);
                byte* refp = src + hashTable[h];
                hashTable[h] = (uint)(ip - src);

                if (refp < ip && (ip - refp) < 0xFFFF && *(uint*)refp == sequence)
                {
                    byte* token = op++;
                    int literalLength = (int)(ip - anchor);

                    if (op + literalLength + 8 > oend)
                        return 0;

                    if (literalLength >= 15)
                    {
                        *token = 15 << 4;
                        int len = literalLength - 15;

                        while (len >= 255)
                        {
                            *op++ = 255;
                            len -= 255;
                        }

                        *op++ = (byte)len;
                    }
                    else
                    {
                        *token = (byte)(literalLength << 4);
                    }

                    for (int i = 0; i < literalLength; i++)
                        *op++ = anchor[i];

                    ushort offset = (ushort)(ip - refp);
                    *op++ = (byte)offset;
                    *op++ = (byte)(offset >> 8);

                    ip += MinMatch;
                    refp += MinMatch;

                    int matchLength = 0;

                    while (ip < mflimit && *(uint*)ip == *(uint*)refp)
                    {
                        ip += 4;
                        refp += 4;
                        matchLength += 4;
                    }

                    while (ip < iend && *ip == *refp)
                    {
                        ip++;
                        refp++;
                        matchLength++;
                    }

                    if (matchLength >= 15)
                    {
                        *token |= 15;
                        int len = matchLength - 15;

                        while (len >= 255)
                        {
                            *op++ = 255;
                            len -= 255;
                        }

                        *op++ = (byte)len;
                    }
                    else
                    {
                        *token |= (byte)matchLength;
                    }

                    anchor = ip;

                    if (op > oend - 5)
                        return 0;

                    continue;
                }

                ip++;
            }

            int lastLit = (int)(iend - anchor);

            if (op + lastLit + (lastLit / 255) + 1 > oend)
                return 0;

            if (lastLit >= 15)
            {
                *op++ = 15 << 4;
                int len = lastLit - 15;

                while (len >= 255)
                {
                    *op++ = 255;
                    len -= 255;
                }

                *op++ = (byte)len;
            }
            else
            {
                *op++ = (byte)(lastLit << 4);
            }

            for (int i = 0; i < lastLit; i++)
                *op++ = anchor[i];

            return (int)(op - dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Decompress(byte* src, int srcLength, byte* dst, int dstCapacity)
        {
            byte* ip = src;
            byte* iend = src + srcLength;
            byte* op = dst;
            byte* oend = dst + dstCapacity;

            while (ip < iend)
            {
                byte token = *ip++;
                int literalLength = token >> 4;

                if (literalLength == 15)
                {
                    byte s;

                    while (((s = *ip++) == 255) && ip < iend)
                        literalLength += 255;

                    literalLength += s;
                }

                if (op + literalLength > oend || ip + literalLength > iend)
                    return 0;

                for (int i = 0; i < literalLength; i++)
                    *op++ = *ip++;

                if (ip >= iend)
                    break;

                ushort offset = (ushort)(ip[0] | (ip[1] << 8));
                ip += 2;
                byte* match = op - offset;
                int matchLength = token & 0x0F;

                if (matchLength == 15)
                {
                    byte s;

                    while (((s = *ip++) == 255) && ip < iend)
                        matchLength += 255;

                    matchLength += s;
                }

                matchLength += 4;

                if (op + matchLength > oend)
                    return 0;

                for (int i = 0; i < matchLength; i++)
                    *op++ = match[i];
            }

            return (int)(op - dst);
        }
    }
}
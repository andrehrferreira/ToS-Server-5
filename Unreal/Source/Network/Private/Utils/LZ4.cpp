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

 #include "Utils/LZ4.h"
 #include <cstring>
 
 int32 FLZ4::Compress(const uint8* Src, int32 SrcSize, uint8* Dst, int32 DstCapacity)
 {
     uint32 HashTable[HASH_SIZE] = {0};
     const uint8* Ip = Src;
     const uint8* Anchor = Ip;
     const uint8* Iend = Src + SrcSize;
     const uint8* Mflimit = Iend - MINMATCH;
     uint8* Op = Dst;
     uint8* Oend = Dst + DstCapacity;
 
     auto Hash = [](uint32 V) { return (V * 2654435761u) >> ((MINMATCH * 8) - HASH_LOG); };
 
     while (Ip < Mflimit)
     {
         uint32 Sequence = *(const uint32*)Ip;
         int H = Hash(Sequence);
         const uint8* Ref = Src + HashTable[H];
         HashTable[H] = (uint32)(Ip - Src);
 
         if (Ref < Ip && (Ip - Ref) < 0xFFFF && *(const uint32*)Ref == Sequence)
         {
             uint8* Token = Op++;
             int LiteralLength = (int)(Ip - Anchor);
 
             if (Op + LiteralLength + 8 > Oend)
                 return 0;
 
             if (LiteralLength >= 15)
             {
                 *Token = 15 << 4;
                 int Len = LiteralLength - 15;
 
                 while (Len >= 255)
                 {
                     *Op++ = 255;
                     Len -= 255;
                 }
 
                 *Op++ = (uint8)Len;
             }
             else
             {
                 *Token = (uint8)(LiteralLength << 4);
             }
 
             FMemory::Memcpy(Op, Anchor, LiteralLength);
             Op += LiteralLength;
 
             uint16 Offset = (uint16)(Ip - Ref);
             *Op++ = (uint8)Offset;
             *Op++ = (uint8)(Offset >> 8);
 
             Ip += MINMATCH;
             Ref += MINMATCH;
 
             int MatchLength = 0;
 
             while (Ip < Mflimit && *(const uint32*)Ip == *(const uint32*)Ref)
             {
                 Ip += 4;
                 Ref += 4;
                 MatchLength += 4;
             }
 
             while (Ip < Iend && *Ip == *Ref)
             {
                 ++Ip;
                 ++Ref;
                 ++MatchLength;
             }
 
             if (MatchLength >= 15)
             {
                 *Token |= 15;
                 int Len = MatchLength - 15;
                 while (Len >= 255)
                 {
                     *Op++ = 255;
                     Len -= 255;
                 }
                 *Op++ = (uint8)Len;
             }
             else
             {
                 *Token |= (uint8)MatchLength;
             }
 
             Anchor = Ip;
 
             if (Op > Oend - 5)
                 return 0;
 
             continue;
         }
         ++Ip;
     }
 
     int LastLit = (int)(Iend - Anchor);
 
     if (Op + LastLit + (LastLit / 255) + 1 > Oend)
         return 0;
 
     if (LastLit >= 15)
     {
         *Op++ = 15 << 4;
         int Len = LastLit - 15;
 
         while (Len >= 255)
         {
             *Op++ = 255;
             Len -= 255;
         }
 
         *Op++ = (uint8)Len;
     }
     else
     {
         *Op++ = (uint8)(LastLit << 4);
     }
 
     FMemory::Memcpy(Op, Anchor, LastLit);
     Op += LastLit;
 
     return int32(Op - Dst);
 }
 
 int32 FLZ4::Decompress(const uint8* Src, int32 SrcSize, uint8* Dst, int32 DstCapacity)
 {
     const uint8* Ip = Src;
     const uint8* Iend = Src + SrcSize;
     uint8* Op = Dst;
     uint8* Oend = Dst + DstCapacity;
 
     while (Ip < Iend)
     {
         uint8 Token = *Ip++;
         int LiteralLength = Token >> 4;
 
         if (LiteralLength == 15)
         {
             uint8 S;
             while (((S = *Ip++) == 255) && Ip < Iend)
                 LiteralLength += 255;
             LiteralLength += S;
         }
 
         if (Op + LiteralLength > Oend || Ip + LiteralLength > Iend)
             return 0;
 
         FMemory::Memcpy(Op, Ip, LiteralLength);
         Ip += LiteralLength;
         Op += LiteralLength;
 
         if (Ip >= Iend)
             break;
 
         uint16 Offset = (uint16)(Ip[0] | (Ip[1] << 8));
         Ip += 2;
         uint8* Match = Op - Offset;
 
         int MatchLength = Token & 0x0F;
 
         if (MatchLength == 15)
         {
             uint8 S;
             while (((S = *Ip++) == 255) && Ip < Iend)
                 MatchLength += 255;
             MatchLength += S;
         }
 
         MatchLength += 4;
 
         if (Op + MatchLength > Oend)
             return 0;
 
         while (MatchLength--)
         {
             *Op++ = *Match++;
         }
     }
 
     return int32(Op - Dst);
 }
 
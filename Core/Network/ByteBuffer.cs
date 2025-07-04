/*
* ByteBuffer
* 
* Author: Diego Guedes
* Modified by: Andre Ferreira
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

using System.Buffers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Networking
{
    public unsafe class ByteBuffer : IDisposable
    {
        public volatile bool IsDestroyed = false;

        public UDPSocket? Connection;

        public ByteBuffer Next;

        public byte[] Data;

        public int Offset;

        public int Length;

        public volatile int Acked;

        public volatile bool Reliable;

        public bool HasData
        {
            get { return Offset > 0; }
        }

        public ByteBuffer()
        {
            Data = ArrayPool<byte>.Shared.Rent(3600);
            Offset = 0;
        }

        ~ByteBuffer()
        {
            if (Data != null)
            {
                ArrayPool<byte>.Shared.Return(Data);
                Data = null;
                IsDestroyed = true;
            }
        }

        public void Dispose()
        {
            if (Connection != null)
            {
                //if (Reliable)
                //    Connection.EndReliable();
                //else
                //    Connection.EndUnreliable();
            }
            else
            {
                ByteBufferPool.Release(this);
            }
        }

        public void Reset()
        {
            Offset = 0;
            Acked = 0;
            Reliable = false;
            Connection = null;
            Next = null;

            if (Data != null)
                Array.Clear(Data, 0, Data.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteBuffer CreateEmptyBuffer()
        {
            return ByteBufferPool.Acquire();
        }

        //Write

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(PacketType value)
        {
            Data[Offset++] = (byte)value;
            Length++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            Data[Offset++] = value;
            Length++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            if (Offset + 4 > Data.Length)
                throw new InvalidOperationException("Buffer overflow.");

            Data[Offset++] = (byte)(value & 0xFF);
            Data[Offset++] = (byte)((value >> 8) & 0xFF);
            Data[Offset++] = (byte)((value >> 16) & 0xFF);
            Data[Offset++] = (byte)((value >> 24) & 0xFF);
            Length += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            if (Offset + 8 > Data.Length)
                throw new InvalidOperationException("Buffer overflow.");

            Data[Offset++] = (byte)(value & 0xFF);
            Data[Offset++] = (byte)((value >> 8) & 0xFF);
            Data[Offset++] = (byte)((value >> 16) & 0xFF);
            Data[Offset++] = (byte)((value >> 24) & 0xFF);
            Data[Offset++] = (byte)((value >> 32) & 0xFF);
            Data[Offset++] = (byte)((value >> 40) & 0xFF);
            Data[Offset++] = (byte)((value >> 48) & 0xFF);
            Data[Offset++] = (byte)((value >> 56) & 0xFF);
            Length += 8;
        }

        //Read

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketType ReadPacketType()
        {
            return (PacketType)ReadByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (Offset + 1 > Length)
                throw new InvalidOperationException("No data to read.");

            return Data[Offset++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt()
        {
            if (Offset + 4 > Length)
                throw new InvalidOperationException("Insufficient data to read Int32.");

            int value =
                Data[Offset] |
                (Data[Offset + 1] << 8) |
                (Data[Offset + 2] << 16) |
                (Data[Offset + 3] << 24);

            Offset += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadLong()
        {
            if (Offset + 8 > Length)
                throw new InvalidOperationException("Insufficient data to read Int64.");

            long value =
                (long)Data[Offset] |
                ((long)Data[Offset + 1] << 8) |
                ((long)Data[Offset + 2] << 16) |
                ((long)Data[Offset + 3] << 24) |
                ((long)Data[Offset + 4] << 32) |
                ((long)Data[Offset + 5] << 40) |
                ((long)Data[Offset + 6] << 48) |
                ((long)Data[Offset + 7] << 56);

            Offset += 8;
            return value;
        }
    }
}

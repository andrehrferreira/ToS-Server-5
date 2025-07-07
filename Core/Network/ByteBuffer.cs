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
using System.Runtime.CompilerServices;

public class ByteBuffer : IDisposable
{
    private bool Disposed = false;

    public volatile bool IsDestroyed = false;

    public UDPSocket Connection;

    public ByteBuffer? Next;

    public byte[] Data;

    public int Offset;

    public int Length;

    public volatile int Acked;

    public volatile bool Reliable;

    public void Destroy()
    {
        if (IsDestroyed)
            return;

        ArrayPool<byte>.Shared.Return(Data);
        Data = Array.Empty<byte>();
        IsDestroyed = true;
        Disposed = true;
    }

    public bool HasData
    {
        get { return Offset > 0; }
    }

    public ByteBuffer()
    {
        Data = ArrayPool<byte>.Shared.Rent(3600);

        Offset = 0;
    }

    public ByteBuffer(byte[] data)
    {
        if (data.Length <= 0 || data.Length > 4096)
            throw new ArgumentException("Data length must be between 1 and 4096 bytes.");

        Data = data;
        Offset = 0;
        Length = data.Length;
    }

    public ByteBuffer(ByteBuffer other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        Data = ArrayPool<byte>.Shared.Rent(3600);
        Array.Copy(other.Data, 0, Data, 0, other.Data.Length);

        Offset = 0;
        Length = other.Length;

        ByteBufferPool.Release(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Assign(byte[] source, int length)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (length <= 0 || length > source.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Invalid length for Assign.");

        if (length > Data.Length)
            throw new InvalidOperationException($"Assigned data length ({length}) exceeds buffer capacity ({Data.Length}).");

        Buffer.BlockCopy(source, 0, Data, 0, length);

        Offset = 0;
        Length = length;

        ArrayPool<byte>.Shared.Return(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] GetBuffer()
    {
        if (Disposed)
            throw new ObjectDisposedException("ByteBuffer");

        return Data;
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            Disposed = true;

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
    }

    public void Reset()
    {
        Offset = 0;
        Acked = 0;
        Reliable = false;
        Connection = null;
        Next = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToHex()
    {
        if (Disposed)
            throw new ObjectDisposedException("ByteBuffer");

        uint hash = 0;

        for (int i = 0; i < Offset; i++)
            hash = (hash << 5) + hash + Data[i];

        return hash.ToString("X8");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetHashFast()
    {
        if (Disposed)
            throw new ObjectDisposedException("ByteBuffer");

        uint hash = 0;

        for (int i = 0; i < Offset; i++)
            hash = (hash << 5) + hash + Data[i];

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetOffset()
    {
        Offset = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteBuffer CreateEmptyBuffer()
    {
        return ByteBufferPool.Acquire();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteBuffer Pack(ByteBuffer buffer, ServerPacket packetType, PacketFlags flags = PacketFlags.None)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        int payloadLength = buffer.Length;

        ByteBuffer packedBuffer = ByteBuffer.CreateEmptyBuffer();

        var type = buffer.Reliable ? PacketType.Reliable : PacketType.Unreliable;
        packedBuffer.Write((byte)type);

        packedBuffer.Write((byte)packetType);

        packedBuffer.Write((ushort)flags);

        if (payloadLength > 0)
        {
            if (packedBuffer.Offset + payloadLength > packedBuffer.Data.Length)
                throw new InvalidOperationException("Payload too large for packed buffer.");

            Buffer.BlockCopy(buffer.Data, 0, packedBuffer.Data, packedBuffer.Offset, payloadLength);
            packedBuffer.Offset += payloadLength;
            packedBuffer.Length += payloadLength;
        }

        return packedBuffer;
    }

    //Write

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(byte value)
    {
        if (Offset + 1 > Data.Length)
            throw new InvalidOperationException("Buffer overflow");

        Data[Offset++] = value;
        Length++;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(PacketType value)
    {
        if (Offset + 1 > Data.Length)
            throw new InvalidOperationException("Buffer overflow");

        Data[Offset++] = (byte)value;
        Length++;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(bool value)
    {
        return Write((byte)(value ? 1 : 0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(int value)
    {
        if (Offset + 4 > Data.Length)
            throw new InvalidOperationException("Buffer overflow");

        Data[Offset++] = (byte)(value & 0xFF);
        Data[Offset++] = (byte)((value >> 8) & 0xFF);
        Data[Offset++] = (byte)((value >> 16) & 0xFF);
        Data[Offset++] = (byte)((value >> 24) & 0xFF);
        Length += 4;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(uint value)
    {
        if (Offset + 4 > Data.Length)
            throw new InvalidOperationException("Buffer overflow");

        Data[Offset++] = (byte)(value & 0xFF);
        Data[Offset++] = (byte)((value >> 8) & 0xFF);
        Data[Offset++] = (byte)((value >> 16) & 0xFF);
        Data[Offset++] = (byte)((value >> 24) & 0xFF);
        Length += 4;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(long value)
    {
        if (Offset + 8 > Data.Length)
            throw new InvalidOperationException("Buffer overflow");

        Data[Offset++] = (byte)(value & 0xFF);
        Data[Offset++] = (byte)((value >> 8) & 0xFF);
        Data[Offset++] = (byte)((value >> 16) & 0xFF);
        Data[Offset++] = (byte)((value >> 24) & 0xFF);
        Data[Offset++] = (byte)((value >> 32) & 0xFF);
        Data[Offset++] = (byte)((value >> 40) & 0xFF);
        Data[Offset++] = (byte)((value >> 48) & 0xFF);
        Data[Offset++] = (byte)((value >> 56) & 0xFF);
        Length += 8;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(float value)
    {
        if (Offset + 4 > Data.Length)
            throw new InvalidOperationException("Buffer overflow");

        int intValue = BitConverter.SingleToInt32Bits(value);

        Data[Offset++] = (byte)(intValue & 0xFF);
        Data[Offset++] = (byte)((intValue >> 8) & 0xFF);
        Data[Offset++] = (byte)((intValue >> 16) & 0xFF);
        Data[Offset++] = (byte)((intValue >> 24) & 0xFF);

        Length += 4;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(string value)
    {
        if (value == null)
            value = string.Empty;

        var encoding = System.Text.Encoding.UTF8;
        int maxByteCount = encoding.GetMaxByteCount(value.Length);

        if (Offset + maxByteCount + 4 > Data.Length)
            throw new InvalidOperationException("Buffer overflow");

        int bytesWritten = encoding.GetBytes(
            value.AsSpan(),
            Data.AsSpan(Offset + 4)
        );

        Write(bytesWritten);

        Offset += bytesWritten;
        Length += bytesWritten;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(ushort value)
    {
        if (Offset + 2 > Data.Length)
            throw new InvalidOperationException("Buffer overflow");

        Data[Offset++] = (byte)(value & 0xFF);
        Data[Offset++] = (byte)((value >> 8) & 0xFF);
        Length += 2;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(FVector value)
    {
        Write((int)value.X);
        Write((int)value.Y);
        Write((int)value.Z);

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer Write(FRotator value)
    {
        Write((int)value.Pitch);
        Write((int)value.Yaw);
        Write((int)value.Roll);

        return this;
    }

    //Read

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T>()
    {
        if (typeof(T) == typeof(byte))
            return (T)(object)ReadByte();
        else if (typeof(T) == typeof(int))
            return (T)(object)ReadInt();
        else if (typeof(T) == typeof(float))
            return (T)(object)ReadFloat();
        else if (typeof(T) == typeof(long))
            return (T)(object)ReadLong();
        else if (typeof(T) == typeof(bool))
            return (T)(object)ReadBool();
        else if (typeof(T) == typeof(string))
            return (T)(object)ReadString();
        else if (typeof(T) == typeof(FVector))
            return (T)(object)ReadFVector();
        else if (typeof(T) == typeof(FRotator))
            return (T)(object)ReadFRotator();
        else if (typeof(T).IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(byte))
            return (T)(object)ReadByte();
        else
            throw new NotSupportedException($"Type '{typeof(T)}' is not supported");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read(byte[] buffer, int offset, int count)
    {
        if (Offset + count > Length)
            throw new InvalidOperationException("Insufficient data to read bytes");

        Buffer.BlockCopy(Data, Offset, buffer, offset, count);
        Offset += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PacketType ReadPacketType()
    {
        return (PacketType)ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (Offset + 1 > Data.Length)
            throw new InvalidOperationException("Buffer underflow");

        return Data[Offset++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool()
    {
        return ReadByte() != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt()
    {
        if (Offset + 4 > Data.Length)
            throw new InvalidOperationException("Buffer underflow");

        int value =
            Data[Offset] |
            (Data[Offset + 1] << 8) |
            (Data[Offset + 2] << 16) |
            (Data[Offset + 3] << 24);

        Offset += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt()
    {
        if (Offset + 4 > Data.Length)
            throw new InvalidOperationException("Buffer underflow");

        uint value =
            (uint)Data[Offset] |
            ((uint)Data[Offset + 1] << 8) |
            ((uint)Data[Offset + 2] << 16) |
            ((uint)Data[Offset + 3] << 24);

        Offset += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong()
    {
        if (Offset + 8 > Data.Length)
            throw new InvalidOperationException("Buffer underflow");

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
        int intValue = ReadInt();
        return BitConverter.Int32BitsToSingle(intValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
        int len = ReadInt();

        if (Offset + len > Data.Length)
            throw new InvalidOperationException("Buffer underflow");

        string value = System.Text.Encoding.UTF8.GetString(Data, Offset, len);
        Offset += len;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
        if (Offset + 2 > Data.Length)
            throw new InvalidOperationException("Buffer underflow");

        ushort value = (ushort)(
            Data[Offset] |
            (Data[Offset + 1] << 8)
        );

        Offset += 2;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector ReadFVector()
    {
        float x = (float)ReadInt();
        float y = (float)ReadInt();
        float z = (float)ReadInt();
        return new FVector(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FRotator ReadFRotator()
    {
        float pitch = (float)ReadInt();
        float yaw = (float)ReadInt();
        float roll = (float)ReadInt();
        return new FRotator(pitch, yaw, roll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadSign()
    {
        if (Length < 4)
            throw new InvalidOperationException("Buffer too small to contain CRC32C signature.");

        int index = Length - 4;

        uint value =
            (uint)Data[index] |
            ((uint)Data[index + 1] << 8) |
            ((uint)Data[index + 2] << 16) |
            ((uint)Data[index + 3] << 24);

        return value;
    }
}

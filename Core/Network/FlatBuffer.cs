/*
* FlatBuffer
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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe struct FlatBuffer : IDisposable
{
    public byte* _ptr;
    private int _capacity;
    private int _offset;
    private bool _disposed;

    public int Position => _offset;
    public int Capacity => _capacity;
    public byte* Data => _ptr;
    public bool IsDisposed => _disposed;

    public FlatBuffer(int capacity)
    {
        _capacity = capacity;
        _offset = 0;
        _disposed = false;
        _ptr = (byte*)Marshal.AllocHGlobal(capacity);
    }

    public void Free() => Dispose();

    public void Reset() => _offset = 0;

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ptr != null)
            {
                Marshal.FreeHGlobal((IntPtr)_ptr);
                _ptr = null;
            }
            _disposed = true;
        }
    }

    private static uint EncodeZigZag(int value)
        => (uint)((value << 1) ^ (value >> 31));

    private static int DecodeZigZag(uint value)
        => (int)((value >> 1) ^ (-(int)(value & 1)));

    private static ulong EncodeZigZag(long value)
        => ((ulong)value << 1) ^ ((ulong)(value >> 63));

    private static long DecodeZigZag(ulong value)
        => (long)(value >> 1) ^ -((long)(value & 1));

    public void Write<T>(T value) where T : unmanaged
    {
        int size = sizeof(T);

        if (_offset + size > _capacity)
            return;

        *(T*)(_ptr + _offset) = value;
        _offset += size;
    }

    public T Read<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(int))
            return (T)(object)ReadInt();
        if (typeof(T) == typeof(uint))
            return (T)(object)ReadUInt();
        if (typeof(T) == typeof(long))
            return (T)(object)ReadLong();
        if (typeof(T) == typeof(ulong))
            return (T)(object)ReadULong();
        if (typeof(T) == typeof(short))
            return (T)(object)ReadShort();
        if (typeof(T) == typeof(ushort))
            return (T)(object)ReadUShort();

        int size = sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {size}");

        T val = *(T*)(_ptr + _offset);
        _offset += size;
        return val;
    }

    public void Write(int value)
        => WriteVarUInt(EncodeZigZag(value));

    public int ReadInt()
    {
        uint raw = ReadVarUInt();
        return DecodeZigZag(raw);
    }

    public void Write(uint value)
        => WriteVarUInt(value);

    public uint ReadUInt()
        => ReadVarUInt();

    public void Write(long value)
        => WriteVarULong(EncodeZigZag(value));

    public long ReadLong()
    {
        ulong raw = ReadVarULong();
        return DecodeZigZag(raw);
    }

    public void WriteVarInt(int value)
    {
        uint v = EncodeZigZag(value);
        while (v >= 0x80)
        {
            Write<byte>((byte)(v | 0x80));
            v >>= 7;
        }
        Write<byte>((byte)v);
    }

    private void WriteVarUInt(uint value)
    {
        uint v = value;
        while (v >= 0x80)
        {
            Write<byte>((byte)(v | 0x80));
            v >>= 7;
        }
        Write<byte>((byte)v);
    }

    public int ReadVarInt()
    {
        int shift = 0;
        uint result = 0;

        while (true)
        {
            byte b = Read<byte>();
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;

            if (shift > 35)
                throw new OverflowException("VarInt too long");
        }

        return DecodeZigZag(result);
    }

    private uint ReadVarUInt()
    {
        int shift = 0;
        uint result = 0;

        while (true)
        {
            byte b = Read<byte>();
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;

            if (shift > 35)
                throw new OverflowException("VarUInt too long");
        }

        return result;
    }

    public void WriteVarLong(long value)
    {
        ulong v = EncodeZigZag(value);
        while (v >= 0x80)
        {
            Write<byte>((byte)(v | 0x80));
            v >>= 7;
        }
        Write<byte>((byte)v);
    }

    public long ReadVarLong()
    {
        int shift = 0;
        ulong result = 0;

        while (true)
        {
            byte b = Read<byte>();
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;

            if (shift > 70)
                throw new OverflowException("VarLong too long");
        }

        return DecodeZigZag(result);
    }

    private void WriteVarULong(ulong value)
    {
        ulong v = value;
        while (v >= 0x80)
        {
            Write<byte>((byte)(v | 0x80));
            v >>= 7;
        }
        Write<byte>((byte)v);
    }

    private ulong ReadVarULong()
    {
        int shift = 0;
        ulong result = 0;

        while (true)
        {
            byte b = Read<byte>();
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;

            if (shift > 70)
                throw new OverflowException("VarULong too long");
        }

        return result;
    }

    public void Write(ulong value)
        => WriteVarULong(value);

    public ulong ReadULong()
        => ReadVarULong();

    public void Write(short value)
        => Write<ushort>((ushort)EncodeZigZag(value));

    public short ReadShort()
    {
        ushort raw = ReadUShort();
        return (short)DecodeZigZag(raw);
    }

    public void Write(ushort value)
        => Write<ushort>(value);

    public ushort ReadUShort()
    {
        int size = sizeof(ushort);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {size}");

        ushort val = *(ushort*)(_ptr + _offset);
        _offset += size;
        return val;
    }

    public void WriteQuantized(float value, float min, float max)
    {
        short quantized = Quantization.ToShort(value, min, max);
        Write(quantized);
    }

    public float ReadQuantizedFloat(float min, float max)
    {
        short quantized = ReadShort();
        return Quantization.ToFloat(quantized, min, max);
    }

    public void WriteQuantized(FVector value, float min, float max)
    {
        WriteQuantized(value.X, min, max);
        WriteQuantized(value.Y, min, max);
        WriteQuantized(value.Z, min, max);
    }

    public FVector ReadQuantizedFVector(float min, float max)
    {
        float x = ReadQuantizedFloat(min, max);
        float y = ReadQuantizedFloat(min, max);
        float z = ReadQuantizedFloat(min, max);
        return new FVector(x, y, z);
    }

    public void WriteQuantized(FRotator value, float min, float max)
    {
        WriteQuantized(value.Pitch, min, max);
        WriteQuantized(value.Yaw, min, max);
        WriteQuantized(value.Roll, min, max);
    }

    public FRotator ReadQuantizedFRotator(float min, float max)
    {
        float pitch = ReadQuantizedFloat(min, max);
        float yaw = ReadQuantizedFloat(min, max);
        float roll = ReadQuantizedFloat(min, max);
        return new FRotator(pitch, yaw, roll);
    }

    public void Write(FVector value)
    {
        Write((int)value.X);
        Write((int)value.Y);
        Write((int)value.Z);
    }

    public void Write(FRotator value)
    {
        Write((int)value.Pitch);
        Write((int)value.Yaw);
        Write((int)value.Roll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector ReadFVector()
    {
        int x = ReadInt();
        int y = ReadInt();
        int z = ReadInt();
        return new FVector(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FRotator ReadFRotator()
    {
        int pitch = ReadInt();
        int yaw = ReadInt();
        int roll = ReadInt();
        return new FRotator(pitch, yaw, roll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetHashFast()
    {
        uint hash = 0;

        for (int i = 0; i < _offset; i++)
            hash = (hash << 5) + hash + Data[i];

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToHex()
    {
        uint hash = 0;

        for (int i = 0; i < _offset; i++)
            hash = (hash << 5) + hash + Data[i];

        return hash.ToString("X8");
    }
}

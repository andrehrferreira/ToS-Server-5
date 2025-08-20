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

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public unsafe struct FlatBuffer : IDisposable
{
    public byte* _ptr;
    private int _capacity;
    private int _offset;
    private bool _disposed;
    private byte _writeBits;
    private int _writeBitIndex;
    private byte _readBits;
    private int _readBitIndex;

    public int Position => _offset;
    public int Capacity => _capacity;
    public byte* Data => _ptr;
    public bool IsDisposed => _disposed;

    public FlatBuffer(int capacity = 1500)
    {
        _capacity = capacity;
        _offset = 0;
        _disposed = false;
        _writeBits = 0;
        _writeBitIndex = 0;
        _readBits = 0;
        _readBitIndex = 0;
        _ptr = (byte*)Marshal.AllocHGlobal(capacity);
    }

    public void Resize(int newCapacity)
    {
        _capacity = newCapacity;
    }

    public void Free() => Dispose();

    public int LengthBits => (_offset * 8) + _writeBitIndex;

    public int SavePosition() => _offset;
    public void RestorePosition(int position) => _offset = position;

    public void Reset()
    {
        _offset = 0;
        _writeBits = 0;
        _writeBitIndex = 0;
        _readBits = 0;
        _readBitIndex = 0;
    }

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
        if (typeof(T) == typeof(FVector))
        {
            Write((FVector)(object)value, 0.1f);
            return;
        }
        if (typeof(T) == typeof(FRotator))
        {
            Write((FRotator)(object)value, 0.1f);
            return;
        }

        int size = sizeof(T);

        if (_offset + size > _capacity)
            return;

        *(T*)(_ptr + _offset) = value;
        _offset += size;
    }

    public T Read<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(FVector))
            return (T)(object)ReadFVector(0.1f);
        if (typeof(T) == typeof(FRotator))
            return (T)(object)ReadFRotator(0.1f);

        int size = sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {size}");

        T val = *(T*)(_ptr + _offset);
        _offset += size;
        return val;
    }

    public void WriteBytes(byte[] value)
    {
        int len = value.Length;

        if (_offset + len > _capacity)
            return;

        fixed (byte* src = value)
        {
            Buffer.MemoryCopy(src, _ptr + _offset, _capacity - _offset, len);
        }

        _offset += len;
    }

    public byte[] ReadBytes(int length)
    {
        if (_offset + length > _capacity)
            throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {length}");

        byte[] value = new byte[length];
        Marshal.Copy((IntPtr)(_ptr + _offset), value, 0, length);
        _offset += length;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByteDirect(byte value)
    {
        if (_offset >= _capacity)
            return;

        *(_ptr + _offset) = value;
        _offset++;
    }

    public void WriteVarInt(int value)
    {
        uint v = EncodeZigZag(value);

        while (v >= 0x80)
        {
            WriteByteDirect((byte)(v | 0x80));
            v >>= 7;
        }

        WriteByteDirect((byte)v);
    }

    private void WriteVarUInt(uint value)
    {
        uint v = value;

        while (v >= 0x80)
        {
            WriteByteDirect((byte)(v | 0x80));
            v >>= 7;
        }

        WriteByteDirect((byte)v);
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
            WriteByteDirect((byte)(v | 0x80));
            v >>= 7;
        }

        WriteByteDirect((byte)v);
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

    public void Write(FVector value, float factor = 0.1f)
    {
        Write<short>((short)MathF.Round(value.X / factor));
        Write<short>((short)MathF.Round(value.Y / factor));
        Write<short>((short)MathF.Round(value.Z / factor));
    }

    public void Write(FRotator value, float factor = 0.1f)
    {
        Write<short>((short)MathF.Round(value.Pitch / factor));
        Write<short>((short)MathF.Round(value.Yaw / factor));
        Write<short>((short)MathF.Round(value.Roll / factor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector ReadFVector(float factor = 0.1f)
    {
        short x = Read<short>();
        short y = Read<short>();
        short z = Read<short>();
        return new FVector(x * factor, y * factor, z * factor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FRotator ReadFRotator(float factor = 0.1f)
    {
        short pitch = Read<short>();
        short yaw = Read<short>();
        short roll = Read<short>();
        return new FRotator(pitch * factor, yaw * factor, roll * factor);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBit(bool value)
    {
        if (_writeBitIndex == 0)
        {
            if (_offset >= _capacity)
                return;
            _ptr[_offset] = 0;
        }

        if (value)
            _ptr[_offset] |= (byte)(1 << _writeBitIndex);

        _writeBitIndex++;

        if (_writeBitIndex == 8)
        {
            _writeBitIndex = 0;
            _offset++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBit()
    {
        if (_readBitIndex == 0)
            _readBits = Read<byte>();

        bool result = (_readBits & (1 << _readBitIndex)) != 0;
        _readBitIndex++;

        if (_readBitIndex == 8)
            _readBitIndex = 0;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AlignBits()
    {
        if (_writeBitIndex > 0)
        {
            _writeBitIndex = 0;
            _offset++;
        }

        if (_readBitIndex > 0)
            _readBitIndex = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peek<T>() where T : unmanaged
    {
        int size = sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException($"Peek exceeds buffer size ({_capacity}) at {_offset} with size {size}");

        return *(T*)(_ptr + _offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAsciiString(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        Write(bytes.Length);

        if (_offset + bytes.Length > _capacity)
            return;

        fixed (byte* ptr = bytes)
            Buffer.MemoryCopy(ptr, _ptr + _offset, _capacity - _offset, bytes.Length);

        _offset += bytes.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAsciiString()
    {
        int length = Read<int>();

        if (_offset + length > _capacity)
            throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {length}");

        string result = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(_ptr + _offset, length));
        _offset += length;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBytes(byte* src, int length)
    {
        if (src == null || length <= 0)
            return;

        if (_offset + length > _capacity)
            return;

        Buffer.MemoryCopy(src, _ptr + _offset, _capacity - _offset, length);
        _offset += length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUtf8String(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Write(bytes.Length);

        if (_offset + bytes.Length > _capacity)
            return;

        fixed (byte* ptr = bytes)
            Buffer.MemoryCopy(ptr, _ptr + _offset, _capacity - _offset, bytes.Length);

        _offset += bytes.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUtf8String()
    {
        int length = Read<int>();

        if (_offset + length > _capacity)
            throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {length}");

        string result = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(_ptr + _offset, length));
        _offset += length;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadSign(int len)
    {
        if (len < 4)
            throw new ArgumentOutOfRangeException(nameof(len), "Buffer too small to contain a CRC32C signature");

        return *(uint*)(_ptr + len - 4);
    }
}

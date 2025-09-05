/*
* ByteBuffer
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Wormhole
{
    public unsafe struct ByteBuffer : IDisposable
    {
        public byte* _ptr;
        private uint _capacity;
        private uint _offset;
        private bool _disposed;

        public bool IsDisposed => _disposed;
        public uint Length => _offset;
        public int Position => (int)_offset; //Compatibility
        public uint Capacity => _capacity;

        public void Free() => Dispose();
        public void Reset() => _offset = 0;
        public uint SavePosition() => _offset;
        public void RestorePosition(uint position) => _offset = position;

        private static Dictionary<Type, int> genericSizes = new Dictionary<Type, int>()
        {
            { typeof(bool),     sizeof(bool) },
            { typeof(float),    sizeof(float) },
            { typeof(double),   sizeof(double) },
            { typeof(sbyte),    sizeof(sbyte) },
            { typeof(byte),     sizeof(byte) },
            { typeof(short),    sizeof(short) },
            { typeof(ushort),   sizeof(ushort) },
            { typeof(int),      sizeof(int) },
            { typeof(uint),     sizeof(uint) },
            { typeof(ulong),    sizeof(ulong) },
            { typeof(long),     sizeof(long) },
        };

        public ByteBuffer(uint capacity = 1024)
        {
            _capacity = capacity;
            _offset = 0;
            _ptr = (byte*)Marshal.AllocHGlobal((int)capacity);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteBuffer From(byte* buffer, uint length)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (length == 0) throw new ArgumentException("Length must be greater than 0", nameof(length));

            return new ByteBuffer
            {
                _ptr = buffer,
                _capacity = length > 1024 ? length : 1024,
                _offset = length,
                _disposed = false
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Copy(byte* src, uint len)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ByteBuffer));
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (_offset + len > _capacity) throw new IndexOutOfRangeException();

            Buffer.MemoryCopy(src, _ptr + _offset, (long)(_capacity - _offset), (long)len);
            _offset += len;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Copy(ref ByteBuffer src)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ByteBuffer));
            if (src._ptr == null) throw new ArgumentNullException(nameof(src));
            if (_offset + src._offset > _capacity) throw new IndexOutOfRangeException();

            Buffer.MemoryCopy(src._ptr, _ptr + _offset, (long)(_capacity - _offset), (long)src._offset);
            _offset += src._offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            return genericSizes[typeof(T)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSupportedType<T>()
        {
            return genericSizes.ContainsKey(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T value) where T : unmanaged
        {
            if (!IsSupportedType<T>())
            {
                throw new ArgumentException("Cannot write type "
                    + typeof(T) + " into this buffer");
            }

            uint size = (uint)SizeOf<T>();

            if (_offset + size > _capacity)
                throw new IndexOutOfRangeException();

            *(T*)(_ptr + _offset) = value;
            _offset += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>() where T : unmanaged
        {
            if (!IsSupportedType<T>())
            {
                throw new ArgumentException("Cannot read type "
                    + typeof(T) + " into this buffer");
            }

            uint size = (uint)SizeOf<T>();

            if (_offset + size > _capacity)
                throw new IndexOutOfRangeException();

            T val = *(T*)(_ptr + _offset);
            _offset += size;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek<T>() where T : unmanaged
        {
            if (!IsSupportedType<T>())
            {
                throw new ArgumentException("Cannot read type "
                    + typeof(T) + " into this buffer");
            }

            uint size = (uint)SizeOf<T>();

            if (_offset + size > _capacity)
                throw new IndexOutOfRangeException($"Peek exceeds buffer size ({_capacity}) at {_offset} with size {size}");

            return *(T*)(_ptr + _offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan()
        {
            if (_disposed || _ptr == null) throw new ObjectDisposedException(nameof(ByteBuffer));

            return new ReadOnlySpan<byte>(_ptr, checked((int)_offset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan()
        {
            if (_disposed || _ptr == null) throw new ObjectDisposedException(nameof(ByteBuffer));

            return new Span<byte>(_ptr, checked((int)_capacity));
        }

        // Hash
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetHashFast()
        {
            uint hash = 0;

            for (int i = 0; i < _offset; i++)
                hash = (hash << 5) + hash + _ptr[i];

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToHex()
        {
            uint hash = 0;

            for (int i = 0; i < _offset; i++)
                hash = (hash << 5) + hash + _ptr[i];

            return hash.ToString("X8");
        }

        // ZigZag
        private static uint EncodeZigZag(int value)
        => (uint)((value << 1) ^ (value >> 31));

        private static int DecodeZigZag(uint value)
            => (int)((value >> 1) ^ (-(int)(value & 1)));

        private static ulong EncodeZigZag(long value)
            => ((ulong)value << 1) ^ ((ulong)(value >> 63));

        private static long DecodeZigZag(ulong value)
            => (long)(value >> 1) ^ -((long)(value & 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteByteDirect(byte value)
        {
            if (_offset >= _capacity)
                throw new IndexOutOfRangeException($"Write exceeds buffer capacity ({_capacity}) at {_offset} with size 1");

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

        // Custom Write/Read 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte[] value)
        {
            int len = value.Length;

            if (_offset + len > _capacity)
                throw new IndexOutOfRangeException($"Write exceeds buffer capacity ({_capacity}) at {_offset} with size {len}");

            fixed (byte* src = value)
            {
                Buffer.MemoryCopy(src, _ptr + _offset, _capacity - _offset, len);
            }

            _offset += (uint)len;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");

            if (_offset + length > _capacity)
                throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {length}");

            byte[] value = new byte[length];
            Marshal.Copy((IntPtr)(_ptr + _offset), value, 0, length);
            _offset += (uint)length;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAsciiString(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            Write(bytes.Length);

            if (_offset + bytes.Length > _capacity)
                throw new IndexOutOfRangeException($"Write exceeds buffer capacity ({_capacity}) at {_offset} with size {bytes.Length}");

            fixed (byte* ptr = bytes)
                Buffer.MemoryCopy(ptr, _ptr + _offset, _capacity - _offset, bytes.Length);

            _offset += (uint)bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiString()
        {
            int length = Read<int>();

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");

            if (_offset + length > _capacity)
                throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {length}");

            string result = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(_ptr + _offset, length));
            _offset += (uint)length;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf8String(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Write(bytes.Length);

            if (_offset + bytes.Length > _capacity)
                throw new IndexOutOfRangeException($"Write exceeds buffer capacity ({_capacity}) at {_offset} with size {bytes.Length}");

            fixed (byte* ptr = bytes)
                Buffer.MemoryCopy(ptr, _ptr + _offset, _capacity - _offset, bytes.Length);

            _offset += (uint)bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUtf8String()
        {
            int length = Read<int>();

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");

            if (_offset + length > _capacity)
                throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {length}");

            string result = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(_ptr + _offset, length));
            _offset += (uint)length;

            return result;
        }
    }
}

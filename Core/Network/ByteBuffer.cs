/*
* ByteBuffer
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

using System.Runtime.InteropServices;

public unsafe struct ByteBuffer : IDisposable
{
    public byte* _ptr;
    private uint _capacity;
    private uint _offset;
    private bool _disposed;

    public uint Position => _offset;
    public bool IsDisposed => _disposed;
    public uint Length => _capacity;

    public void Free() => Dispose();
    public void Reset() => _offset = 0;

    public ByteBuffer(uint capacity = 1200)
    {
        _capacity = capacity;
        _offset = 0;
        _ptr = (byte*)Marshal.AllocHGlobal((int)capacity);
    }

    public static ByteBuffer From(byte* buffer, uint length)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (length == 0) throw new ArgumentException("Length must be greater than 0", nameof(length));

        return new ByteBuffer
        {
            _ptr = buffer,
            _capacity = length > 1200 ? length : 1200,
            _offset = length,
            _disposed = false
        };
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

    public void Copy(byte* src, uint len)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ByteBuffer));
        if (src == null) throw new ArgumentNullException(nameof(src));
        if (_offset + len > _capacity) throw new IndexOutOfRangeException();

        Buffer.MemoryCopy(src, _ptr + _offset, (long)(_capacity - _offset), (long)len);
        _offset += len;
    }

    public void Copy(ref ByteBuffer src)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ByteBuffer));
        if (src._ptr == null) throw new ArgumentNullException(nameof(src));
        if (_offset + src._offset > _capacity) throw new IndexOutOfRangeException();

        Buffer.MemoryCopy(src._ptr, _ptr + _offset, (long)(_capacity - _offset), (long)src._offset);
        _offset += src._offset;
    }

    public void Write<T>(T value) where T : unmanaged
    {
        uint size = (uint)sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException();

        *(T*)(_ptr + _offset) = value;
        _offset += size;
    }

    public T Read<T>() where T : unmanaged
    {
        uint size = (uint)sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException();

        T val = *(T*)(_ptr + _offset);
        _offset += size;
        return val;
    }
}

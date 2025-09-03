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
    private int _capacity;
    private int _offset;

    private bool _disposed;

    public int Position => _offset;
    public bool IsDisposed => _disposed;

    public ByteBuffer(int capacity = 1200)
    {
        _capacity = capacity;
        _offset = 0;
        _ptr = (byte*)Marshal.AllocHGlobal(capacity);
    }

    public void Free() => Dispose();

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

    public void Copy(byte* buffer, int len)
    {
        if (_ptr != null && !_disposed)
            Buffer.MemoryCopy(buffer, _ptr, _capacity, len);
    }

    public void Write<T>(T value) where T : unmanaged
    {
        int size = sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException();

        *(T*)(_ptr + _offset) = value;
        _offset += size;
    }

    public T Read<T>() where T : unmanaged
    {
        int size = sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException();

        T val = *(T*)(_ptr + _offset);
        _offset += size;
        return val;
    }
}

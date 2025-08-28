using System;
using System.Runtime.InteropServices;

public unsafe struct TinyBuffer : IDisposable
{
    public byte* _ptr;
    private int _capacity;
    private int _offset;

    private bool _disposed;

    public int Position => _offset;
    public bool IsDisposed => _disposed;

    public TinyBuffer(int capacity = 1500)
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
        /*if (typeof(T) == typeof(FVector))
        {
            Write((FVector)(object)value, 0.1f);
            return;
        }
        if (typeof(T) == typeof(FRotator))
        {
            Write((FRotator)(object)value, 0.1f);
            return;
        }*/

        int size = sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException($"Write exceeds buffer capacity ({_capacity}) at {_offset} with size {size}");

        *(T*)(_ptr + _offset) = value;
        _offset += size;
    }

    public T Read<T>() where T : unmanaged
    {
        /*if (typeof(T) == typeof(FVector))
            return (T)(object)ReadFVector(0.1f);
        if (typeof(T) == typeof(FRotator))
            return (T)(object)ReadFRotator(0.1f);*/

        int size = sizeof(T);

        if (_offset + size > _capacity)
            throw new IndexOutOfRangeException($"Read exceeds buffer size ({_capacity}) at {_offset} with size {size}");

        T val = *(T*)(_ptr + _offset);
        _offset += size;
        return val;
    }

    public void Deserialize<T>(ref T packetType) where T : IPacketClient
    {
        packetType.Deserialize(ref this);
    }
}

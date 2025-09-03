/*
 * StructPool
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

public unsafe class StructPool<T> : IDisposable where T : unmanaged
{
    private T* _items;
    private bool[] _active;
    private int _capacity;
    private int _count;

    public int Count => _count;
    public int Capacity => _capacity;

    public StructPool(int capacity)
    {
        _capacity = capacity;
        _count = 0;
        _items = (T*)NativeMemory.Alloc((nuint)(capacity * sizeof(T)));
        _active = new bool[capacity];
    }

    public uint Create()
    {
        for (uint i = 0; i < _capacity; i++)
        {
            if (!_active[i])
            {
                _active[i] = true;
                _count++;
                return i;
            }
        }

        throw new Exception("StructPool capacity reached");
    }

    public void Destroy(uint id)
    {
        Destroy((int)id);
    }

    public void Destroy(int id)
    {
        if (id < 0 || id >= _capacity)
            return;

        if (_active[id])
        {
            _active[id] = false;
            _items[id] = default;
            _count--;
        }
    }

    public ref T Get(uint id)
    {
        if (id < 0 || id >= _capacity)
            throw new IndexOutOfRangeException();

        if (!_active[id])
            throw new InvalidOperationException($"Entity {id} is not active.");

        return ref _items[id];
    }

    public bool IsActive(uint id)
    {
        if (id < 0 || id >= _capacity)
            return false;

        return _active[id];
    }

    public void ForEach(Action<int, T> action)
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (_active[i])
            {
                action(i, _items[i]);
            }
        }
    }

    public void Dispose()
    {
        NativeMemory.Free(_items);
    }
}

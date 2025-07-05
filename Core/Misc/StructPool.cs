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

    public int Create()
    {
        for (int i = 0; i < _capacity; i++)
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

    public void Destroy(int id)
    {
        if (id < 0 || id >= _capacity) return;
        if (_active[id])
        {
            _active[id] = false;
            _items[id] = default;
            _count--;
        }
    }

    public ref T Get(int id)
    {
        if (id < 0 || id >= _capacity) throw new IndexOutOfRangeException();
        if (!_active[id]) throw new InvalidOperationException($"Entity {id} is not active.");
        return ref _items[id];
    }

    public bool IsActive(int id)
    {
        if (id < 0 || id >= _capacity) return false;
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

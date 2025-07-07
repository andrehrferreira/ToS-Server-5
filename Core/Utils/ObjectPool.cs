using System.Collections.Concurrent;

public class ObjectPool<T> where T : class, IPoolable, new()
{
    private readonly ConcurrentBag<T> _pool = new();

    public T Rent()
    {
        return _pool.TryTake(out var obj) ? obj : new T();
    }

    public void Return(T obj)
    {
        obj.Reset();
        _pool.Add(obj);
    }
}

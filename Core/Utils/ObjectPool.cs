/*
 * ObjectPool
 *
 * Author: Andre Ferreira
 *
 * Copyright (c) Uzmi Games. Licensed under the MIT License.
 */

using System.Collections.Concurrent;

namespace Wormhole
{
    public interface IPoolable
    {
        void Reset();
    }

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
}

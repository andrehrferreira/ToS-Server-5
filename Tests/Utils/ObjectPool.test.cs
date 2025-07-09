using System;

namespace Tests
{
    class PoolableItem : IPoolable
    {
        public bool ResetCalled { get; private set; }

        public void Reset()
        {
            ResetCalled = true;
        }
    }

    public class ObjectPoolTests : AbstractTest
    {
        public ObjectPoolTests()
        {
            Describe("ObjectPool", () =>
            {
                It("should rent new object when pool is empty", () =>
                {
                    var pool = new ObjectPool<PoolableItem>();
                    var item = pool.Rent();
                    Expect(item).NotToBeNull();
                });

                It("should reuse returned object", () =>
                {
                    var pool = new ObjectPool<PoolableItem>();
                    var item1 = pool.Rent();
                    pool.Return(item1);
                    var item2 = pool.Rent();
                    Expect(ReferenceEquals(item1, item2)).ToBeTrue();
                    Expect(item2.ResetCalled).ToBeTrue();
                });

                It("should handle multiple objects", () =>
                {
                    var pool = new ObjectPool<PoolableItem>();
                    var first = pool.Rent();
                    var second = pool.Rent();
                    pool.Return(first);
                    pool.Return(second);
                    var a = pool.Rent();
                    var b = pool.Rent();
                    Expect((ReferenceEquals(first, a) || ReferenceEquals(first, b))).ToBeTrue();
                    Expect((ReferenceEquals(second, a) || ReferenceEquals(second, b))).ToBeTrue();
                });
            });
        }
    }
}

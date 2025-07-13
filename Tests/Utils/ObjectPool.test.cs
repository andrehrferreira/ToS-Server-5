namespace Tests
{
    // Test object that implements IPoolable
    public class TestPoolableObject : IPoolable
    {
        public int Value { get; set; }
        public bool WasReset { get; private set; }

        public void Reset()
        {
            Value = 0;
            WasReset = true;
        }
    }

    // Complex test object
    public class ComplexPoolableObject : IPoolable
    {
        public string Name { get; set; }
        public int[] Numbers { get; set; }
        public bool IsActive { get; set; }
        public int ResetCount { get; private set; }

        public ComplexPoolableObject()
        {
            Name = "";
            Numbers = new int[0];
            IsActive = false;
            ResetCount = 0;
        }

        public void Reset()
        {
            Name = "";
            Numbers = new int[0];
            IsActive = false;
            ResetCount++;
        }
    }

    public class ObjectPoolTests : AbstractTest
    {
        public ObjectPoolTests()
        {
            Describe("ObjectPool Basic Operations", () =>
            {
                It("should create new object when pool is empty", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();

                    var obj = pool.Rent();

                    Expect(obj).NotToBeNull();
                    Expect(obj.Value).ToBe(0);
                    Expect(obj.WasReset).ToBe(false);
                });

                It("should return object to pool", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();
                    var obj = pool.Rent();
                    obj.Value = 42;

                    pool.Return(obj);

                    // Object should be reset
                    Expect(obj.Value).ToBe(0);
                    Expect(obj.WasReset).ToBe(true);
                });

                It("should reuse returned objects", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();

                    // Rent and return an object
                    var obj1 = pool.Rent();
                    obj1.Value = 42;
                    pool.Return(obj1);

                    // Rent again - should get the same object (reused)
                    var obj2 = pool.Rent();

                    Expect(obj2).ToBe(obj1);
                    Expect(obj2.Value).ToBe(0); // Should be reset
                    Expect(obj2.WasReset).ToBe(true);
                });

                It("should handle multiple rent and return cycles", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();

                    for (int i = 0; i < 10; i++)
                    {
                        var obj = pool.Rent();
                        obj.Value = i;

                        Expect(obj).NotToBeNull();
                        Expect(obj.Value).ToBe(i);

                        pool.Return(obj);

                        Expect(obj.Value).ToBe(0);
                        Expect(obj.WasReset).ToBe(true);
                    }
                });
            });

            Describe("ObjectPool with Complex Objects", () =>
            {
                It("should handle complex object pooling", () =>
                {
                    var pool = new ObjectPool<ComplexPoolableObject>();

                    var obj = pool.Rent();
                    obj.Name = "TestObject";
                    obj.Numbers = new int[] { 1, 2, 3, 4, 5 };
                    obj.IsActive = true;

                    Expect(obj.ResetCount).ToBe(0);

                    pool.Return(obj);

                    Expect(obj.Name).ToBe("");
                    Expect(obj.Numbers.Length).ToBe(0);
                    Expect(obj.IsActive).ToBe(false);
                    Expect(obj.ResetCount).ToBe(1);
                });

                It("should track reset count correctly", () =>
                {
                    var pool = new ObjectPool<ComplexPoolableObject>();
                    var obj = pool.Rent();

                    Expect(obj.ResetCount).ToBe(0);

                    // Return and rent multiple times
                    for (int i = 1; i <= 5; i++)
                    {
                        pool.Return(obj);
                        Expect(obj.ResetCount).ToBe(i);

                        obj = pool.Rent();
                    }
                });
            });

            Describe("ObjectPool Multiple Objects", () =>
            {
                It("should handle multiple objects in pool", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();

                    // Create and return multiple objects
                    var obj1 = pool.Rent();
                    var obj2 = pool.Rent();
                    var obj3 = pool.Rent();

                    obj1.Value = 1;
                    obj2.Value = 2;
                    obj3.Value = 3;

                    pool.Return(obj1);
                    pool.Return(obj2);
                    pool.Return(obj3);

                    // All should be reset
                    Expect(obj1.Value).ToBe(0);
                    Expect(obj2.Value).ToBe(0);
                    Expect(obj3.Value).ToBe(0);

                    // Rent again - should get objects from pool
                    var reused1 = pool.Rent();
                    var reused2 = pool.Rent();
                    var reused3 = pool.Rent();

                    Expect(reused1).NotToBeNull();
                    Expect(reused2).NotToBeNull();
                    Expect(reused3).NotToBeNull();
                });

                It("should maintain separate pools for different types", () =>
                {
                    var testPool = new ObjectPool<TestPoolableObject>();
                    var complexPool = new ObjectPool<ComplexPoolableObject>();

                    var testObj = testPool.Rent();
                    var complexObj = complexPool.Rent();

                    testObj.Value = 42;
                    complexObj.Name = "Complex";

                    testPool.Return(testObj);
                    complexPool.Return(complexObj);

                    var testObj2 = testPool.Rent();
                    var complexObj2 = complexPool.Rent();

                    Expect(testObj2).ToBe(testObj);
                    Expect(complexObj2).ToBe(complexObj);

                    Expect(testObj2.Value).ToBe(0);
                    Expect(complexObj2.Name).ToBe("");
                });
            });

            Describe("ObjectPool Performance", () =>
            {
                It("should handle large number of operations", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();
                    const int count = 1000;

                    // Rent many objects
                    var objects = new TestPoolableObject[count];
                    for (int i = 0; i < count; i++)
                    {
                        objects[i] = pool.Rent();
                        objects[i].Value = i;
                    }

                    // Return all objects
                    for (int i = 0; i < count; i++)
                    {
                        pool.Return(objects[i]);
                        Expect(objects[i].Value).ToBe(0);
                        Expect(objects[i].WasReset).ToBe(true);
                    }

                    // Rent again - should reuse objects
                    for (int i = 0; i < count; i++)
                    {
                        var obj = pool.Rent();
                        Expect(obj).NotToBeNull();
                        Expect(obj.Value).ToBe(0);
                        Expect(obj.WasReset).ToBe(true);
                    }
                });

                It("should handle rapid rent and return cycles", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();

                    for (int cycle = 0; cycle < 100; cycle++)
                    {
                        var obj = pool.Rent();
                        obj.Value = cycle;

                        Expect(obj.Value).ToBe(cycle);

                        pool.Return(obj);

                        Expect(obj.Value).ToBe(0);
                        Expect(obj.WasReset).ToBe(true);
                    }
                });
            });

            Describe("ObjectPool Edge Cases", () =>
            {
                It("should handle returning same object multiple times", () =>
                {
                    var pool = new ObjectPool<ComplexPoolableObject>();
                    var obj = pool.Rent();

                    obj.Name = "Test";
                    pool.Return(obj);

                    Expect(obj.ResetCount).ToBe(1);

                    // Return same object again
                    pool.Return(obj);

                    Expect(obj.ResetCount).ToBe(2);
                });

                It("should create new objects when pool cannot satisfy demand", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();

                    // Rent multiple objects without returning
                    var obj1 = pool.Rent();
                    var obj2 = pool.Rent();
                    var obj3 = pool.Rent();

                    // All should be different objects
                    Expect(obj1).NotToBe(obj2);
                    Expect(obj2).NotToBe(obj3);
                    Expect(obj1).NotToBe(obj3);

                    // All should be valid
                    Expect(obj1).NotToBeNull();
                    Expect(obj2).NotToBeNull();
                    Expect(obj3).NotToBeNull();
                });

                It("should handle empty pool rent operations", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();

                    // Multiple rents from empty pool
                    for (int i = 0; i < 10; i++)
                    {
                        var obj = pool.Rent();
                        Expect(obj).NotToBeNull();
                        Expect(obj.Value).ToBe(0);
                        Expect(obj.WasReset).ToBe(false);
                    }
                });

                It("should maintain object state until reset", () =>
                {
                    var pool = new ObjectPool<ComplexPoolableObject>();
                    var obj = pool.Rent();

                    obj.Name = "Persistent";
                    obj.Numbers = new int[] { 10, 20, 30 };
                    obj.IsActive = true;

                    // Before returning, state should be maintained
                    Expect(obj.Name).ToBe("Persistent");
                    Expect(obj.Numbers.Length).ToBe(3);
                    Expect(obj.Numbers[0]).ToBe(10);
                    Expect(obj.IsActive).ToBe(true);

                    pool.Return(obj);

                    // After returning, state should be reset
                    Expect(obj.Name).ToBe("");
                    Expect(obj.Numbers.Length).ToBe(0);
                    Expect(obj.IsActive).ToBe(false);
                });
            });

            Describe("ObjectPool Reset Behavior", () =>
            {
                It("should call reset on every return", () =>
                {
                    var pool = new ObjectPool<ComplexPoolableObject>();
                    var obj = pool.Rent();

                    Expect(obj.ResetCount).ToBe(0);

                    pool.Return(obj);
                    Expect(obj.ResetCount).ToBe(1);

                    obj = pool.Rent(); // Same object
                    obj.Name = "Modified";

                    pool.Return(obj);
                    Expect(obj.ResetCount).ToBe(2);
                    Expect(obj.Name).ToBe("");
                });

                It("should ensure objects are clean when rented", () =>
                {
                    var pool = new ObjectPool<TestPoolableObject>();

                    // First use
                    var obj = pool.Rent();
                    obj.Value = 999;
                    pool.Return(obj);

                    // Second use - should be clean
                    var cleanObj = pool.Rent();
                    Expect(cleanObj.Value).ToBe(0);
                    Expect(cleanObj.WasReset).ToBe(true);
                });
            });
        }
    }
}

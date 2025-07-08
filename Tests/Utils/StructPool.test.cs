namespace Tests
{
    public struct TestStruct
    {
        public int Id;
        public float Value;
        public byte Status;
    }

    public class StructPoolTests : AbstractTest
    {
        public StructPoolTests()
        {
            Describe("StructPool Operations", () =>
            {
                It("should create pool with correct capacity", () =>
                {
                    using var pool = new StructPool<TestStruct>(10);
                    
                    Expect(pool.Capacity).ToBe(10);
                    Expect(pool.Count).ToBe(0);
                });

                It("should create new items and return valid IDs", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    uint id1 = pool.Create();
                    uint id2 = pool.Create();
                    uint id3 = pool.Create();
                    
                    Expect(id1).ToBe(0);
                    Expect(id2).ToBe(1);
                    Expect(id3).ToBe(2);
                    Expect(pool.Count).ToBe(3);
                });

                It("should throw exception when capacity is reached", () =>
                {
                    using var pool = new StructPool<TestStruct>(2);
                    
                    pool.Create(); // ID 0
                    pool.Create(); // ID 1
                    
                    try
                    {
                        pool.Create(); // Should throw
                        Expect(false).ToBeTrue(); // Should not reach here
                    }
                    catch (Exception ex)
                    {
                        Expect(ex.Message).ToBe("StructPool capacity reached");
                    }
                });

                It("should destroy items and update count", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    uint id1 = pool.Create();
                    uint id2 = pool.Create();
                    
                    Expect(pool.Count).ToBe(2);
                    
                    pool.Destroy((int)id1);
                    
                    Expect(pool.Count).ToBe(1);
                    Expect(pool.IsActive(id1)).ToBe(false);
                    Expect(pool.IsActive(id2)).ToBe(true);
                });

                It("should reuse destroyed slots", () =>
                {
                    using var pool = new StructPool<TestStruct>(3);
                    
                    uint id1 = pool.Create(); // ID 0
                    uint id2 = pool.Create(); // ID 1
                    
                    pool.Destroy(id1);
                    
                    uint id3 = pool.Create(); // Should reuse ID 0
                    
                    Expect(id3).ToBe(0);
                    Expect(pool.Count).ToBe(2);
                });

                It("should handle destroy with invalid IDs gracefully", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    // Should not throw or crash
                    pool.Destroy(-1);
                    pool.Destroy(10);
                    pool.Destroy(0); // Not created yet
                    
                    Expect(pool.Count).ToBe(0);
                });

                It("should get and modify struct references", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    uint id = pool.Create();
                    ref var item = ref pool.Get(id);
                    
                    item.Id = 42;
                    item.Value = 3.14f;
                    item.Status = 255;
                    
                    ref var retrieved = ref pool.Get(id);
                    
                    Expect(retrieved.Id).ToBe(42);
                    Expect(retrieved.Value).ToBe(3.14f);
                    Expect(retrieved.Status).ToBe((byte)255);
                });

                It("should throw exception when getting inactive item", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    uint id = pool.Create();
                    pool.Destroy(id);
                    
                    try
                    {
                        ref var item = ref pool.Get(id);
                        Expect(false).ToBeTrue();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Expect(ex.Message).ToBe($"Entity {id} is not active.");
                    }
                });

                It("should throw exception when getting out of range ID", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    try
                    {
                        ref var item = ref pool.Get(10);
                        Expect(false).ToBeTrue();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Expect(true).ToBeTrue(); 
                    }
                });

                It("should report correct IsActive status", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    uint id1 = pool.Create();
                    uint id2 = pool.Create();
                    
                    Expect(pool.IsActive(id1)).ToBe(true);
                    Expect(pool.IsActive(id2)).ToBe(true);
                    Expect(pool.IsActive(2)).ToBe(false);
                    
                    pool.Destroy(id1);
                    
                    Expect(pool.IsActive(id1)).ToBe(false);
                    Expect(pool.IsActive(id2)).ToBe(true);
                });

                It("should return false for IsActive with invalid IDs", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    Expect(pool.IsActive(10)).ToBe(false);
                });

                It("should iterate only active items with ForEach", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    uint id1 = pool.Create();
                    uint id2 = pool.Create();
                    uint id3 = pool.Create();
                    
                    pool.Get(id1).Id = 10;
                    pool.Get(id2).Id = 20;
                    pool.Get(id3).Id = 30;
                    
                    pool.Destroy(id2);
                    
                    var visitedIds = new List<int>();
                    var visitedValues = new List<int>();
                    
                    pool.ForEach((id, item) =>
                    {
                        visitedIds.Add(id);
                        visitedValues.Add(item.Id);
                    });
                    
                    Expect(visitedIds.Count).ToBe(2);
                    Expect(visitedIds.Contains((int)id1)).ToBe(true);
                    Expect(visitedIds.Contains((int)id3)).ToBe(true);
                    Expect(visitedIds.Contains((int)id2)).ToBe(false);
                    
                    Expect(visitedValues.Contains(10)).ToBe(true);
                    Expect(visitedValues.Contains(30)).ToBe(true);
                    Expect(visitedValues.Contains(20)).ToBe(false);
                });

                It("should handle ForEach with no active items", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    int visitCount = 0;
                    pool.ForEach((id, item) =>
                    {
                        visitCount++;
                    });
                    
                    Expect(visitCount).ToBe(0);
                });

                It("should reset struct to default when destroyed", () =>
                {
                    using var pool = new StructPool<TestStruct>(5);
                    
                    uint id = pool.Create();
                    ref var item = ref pool.Get(id);
                    
                    item.Id = 42;
                    item.Value = 3.14f;
                    item.Status = 255;
                    
                    pool.Destroy(id);
                    
                    uint newId = pool.Create();
                    Expect(newId).ToBe(id);
                    
                    ref var newItem = ref pool.Get(newId);
                    
                    Expect(newItem.Id).ToBe(0);
                    Expect(newItem.Value).ToBe(0.0f);
                    Expect(newItem.Status).ToBe((byte)0);
                });

                It("should handle multiple create/destroy cycles", () =>
                {
                    using var pool = new StructPool<TestStruct>(3);
                    
                    for (int cycle = 0; cycle < 10; cycle++)
                    {
                        uint id1 = pool.Create();
                        uint id2 = pool.Create();
                        uint id3 = pool.Create();
                        
                        Expect(pool.Count).ToBe(3);
                        
                        pool.Destroy(id1);
                        pool.Destroy(id2);
                        pool.Destroy(id3);
                        
                        Expect(pool.Count).ToBe(0);
                    }
                });

                It("should work with different struct types", () =>
                {
                    using var intPool = new StructPool<int>(5);
                    using var floatPool = new StructPool<float>(5);
                    
                    uint intId = intPool.Create();
                    uint floatId = floatPool.Create();
                    
                    intPool.Get(intId) = 42;
                    floatPool.Get(floatId) = 3.14f;
                    
                    Expect(intPool.Get(intId)).ToBe(42);
                    Expect(floatPool.Get(floatId)).ToBe(3.14f);
                });

                It("should dispose and free memory properly", () =>
                {
                    var pool = new StructPool<TestStruct>(5);
                    
                    uint id = pool.Create();
                    pool.Get(id).Id = 42;
                    pool.Dispose();
                    
                    Expect(true).ToBeTrue();
                });
            });
        }
    }
} 

namespace Tests
{
    public class QueueStructLinkedTests : AbstractTest
    {
        public QueueStructLinkedTests()
        {
            Describe("QueueStructLinked Core Operations", () =>
            {
                It("should create empty queue", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    Expect(queue.Head).ToBeNull();
                    Expect(queue.Tail).ToBeNull();
                    Expect(queue.Length).ToBe(0);
                });

                It("should add single item", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    queue.Add(42);

                    Expect(queue.Head).NotToBeNull();
                    Expect(queue.Tail).NotToBeNull();
                    Expect(queue.Head).ToBe(queue.Tail);
                    Expect(queue.Head.Value).ToBe(42);
                    Expect(queue.Length).ToBe(1);
                });

                It("should add multiple items in LIFO order", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    queue.Add(1);
                    queue.Add(2);
                    queue.Add(3);

                    Expect(queue.Head.Value).ToBe(3);
                    Expect(queue.Tail.Value).ToBe(1);
                    Expect(queue.Length).ToBe(3);
                });

                It("should take single item from queue", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(42);

                    var node = queue.Take();

                    Expect(node).NotToBeNull();
                    Expect(node.Value).ToBe(42);
                    Expect(queue.Head).ToBeNull();
                    Expect(queue.Tail).ToBeNull();
                    Expect(queue.Length).ToBe(0);
                });

                It("should take items in LIFO order", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(1);
                    queue.Add(2);
                    queue.Add(3);

                    var node1 = queue.Take();
                    var node2 = queue.Take();
                    var node3 = queue.Take();

                    Expect(node1.Value).ToBe(3);
                    Expect(node2.Value).ToBe(2);
                    Expect(node3.Value).ToBe(1);
                    Expect(queue.Length).ToBe(0);
                });

                It("should return null when taking from empty queue", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    var node = queue.Take();

                    Expect(node).ToBeNull();
                });
            });

            Describe("QueueStructLinked Clear Operations", () =>
            {
                It("should clear empty queue", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    var result = queue.Clear();

                    Expect(result).ToBeNull();
                    Expect(queue.Head).ToBeNull();
                    Expect(queue.Tail).ToBeNull();
                });

                It("should clear queue with items", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(1);
                    queue.Add(2);
                    queue.Add(3);

                    var result = queue.Clear();

                    Expect(result).NotToBeNull();
                    Expect(result.Value).ToBe(3); // Head was 3
                    Expect(queue.Head).ToBeNull();
                    Expect(queue.Tail).ToBeNull();
                    Expect(queue.Length).ToBe(0);
                });

                It("should return entire chain when clearing", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(1);
                    queue.Add(2);
                    queue.Add(3);

                    var chain = queue.Clear();

                    // Verify the chain is intact
                    Expect(chain.Value).ToBe(3);
                    Expect(chain.Next.Value).ToBe(2);
                    Expect(chain.Next.Next.Value).ToBe(1);
                    Expect(chain.Next.Next.Next).ToBeNull();
                });
            });

            Describe("QueueStructLinked Merge Operations", () =>
            {
                It("should merge empty queue with empty queue", () =>
                {
                    var queue1 = new QueueStructLinked<int>();
                    var queue2 = new QueueStructLinked<int>();

                    queue1.Merge(queue2);

                    Expect(queue1.Head).ToBeNull();
                    Expect(queue1.Tail).ToBeNull();
                    Expect(queue1.Length).ToBe(0);
                    Expect(queue2.Head).ToBeNull();
                    Expect(queue2.Tail).ToBeNull();
                });

                It("should merge empty queue with non-empty queue", () =>
                {
                    var queue1 = new QueueStructLinked<int>();
                    var queue2 = new QueueStructLinked<int>();
                    queue2.Add(1);
                    queue2.Add(2);

                    queue1.Merge(queue2);

                    Expect(queue1.Length).ToBe(2);
                    Expect(queue1.Head.Value).ToBe(2);
                    Expect(queue1.Tail.Value).ToBe(1);
                    Expect(queue2.Head).ToBeNull();
                    Expect(queue2.Tail).ToBeNull();
                });

                It("should merge non-empty queue with empty queue", () =>
                {
                    var queue1 = new QueueStructLinked<int>();
                    var queue2 = new QueueStructLinked<int>();
                    queue1.Add(1);
                    queue1.Add(2);

                    queue1.Merge(queue2);

                    Expect(queue1.Length).ToBe(2);
                    Expect(queue1.Head.Value).ToBe(2);
                    Expect(queue1.Tail.Value).ToBe(1);
                    Expect(queue2.Head).ToBeNull();
                    Expect(queue2.Tail).ToBeNull();
                });

                It("should merge two non-empty queues", () =>
                {
                    var queue1 = new QueueStructLinked<int>();
                    var queue2 = new QueueStructLinked<int>();

                    queue1.Add(1);
                    queue1.Add(2);
                    queue2.Add(3);
                    queue2.Add(4);

                    queue1.Merge(queue2);

                    Expect(queue1.Length).ToBe(4);
                    Expect(queue1.Head.Value).ToBe(2);
                    Expect(queue1.Tail.Value).ToBe(3);
                    Expect(queue2.Head).ToBeNull();
                    Expect(queue2.Tail).ToBeNull();
                });

                It("should maintain correct order after merge", () =>
                {
                    var queue1 = new QueueStructLinked<string>();
                    var queue2 = new QueueStructLinked<string>();

                    queue1.Add("A");
                    queue1.Add("B");
                    queue2.Add("C");
                    queue2.Add("D");

                    queue1.Merge(queue2);

                    var node1 = queue1.Take();
                    var node2 = queue1.Take();
                    var node3 = queue1.Take();
                    var node4 = queue1.Take();

                    Expect(node1.Value).ToBe("B");
                    Expect(node2.Value).ToBe("A");
                    Expect(node3.Value).ToBe("D");
                    Expect(node4.Value).ToBe("C");
                });
            });

            Describe("QueueStructLinked Node Pool", () =>
            {
                It("should reuse nodes from pool", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    // Add and clear to populate pool
                    queue.Add(1);
                    queue.Add(2);
                    var chain = queue.Clear();
                    QueueStructLinked<int>.ReleaseChain(chain);

                    // Add again - should reuse nodes
                    queue.Add(3);
                    queue.Add(4);

                    Expect(queue.Length).ToBe(2);
                    Expect(queue.Head.Value).ToBe(4);
                    Expect(queue.Tail.Value).ToBe(3);
                });

                It("should release chain of nodes", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(1);
                    queue.Add(2);
                    queue.Add(3);

                    var chain = queue.Clear();

                    // Should not throw
                    QueueStructLinked<int>.ReleaseChain(chain);

                    // Verify queue is still empty
                    Expect(queue.Head).ToBeNull();
                    Expect(queue.Tail).ToBeNull();
                });

                It("should handle null chain release", () =>
                {
                    // Should not throw
                    QueueStructLinked<int>.ReleaseChain(null);

                    // Should still work normally
                    var queue = new QueueStructLinked<int>();
                    queue.Add(42);
                    Expect(queue.Head.Value).ToBe(42);
                });
            });

            Describe("QueueStructLinked Length Calculation", () =>
            {
                It("should calculate length correctly for empty queue", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    Expect(queue.Length).ToBe(0);
                });

                It("should calculate length correctly for single item", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(42);

                    Expect(queue.Length).ToBe(1);
                });

                It("should calculate length correctly for multiple items", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    for (int i = 0; i < 10; i++)
                    {
                        queue.Add(i);
                        Expect(queue.Length).ToBe(i + 1);
                    }
                });

                It("should update length correctly after taking items", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(1);
                    queue.Add(2);
                    queue.Add(3);

                    Expect(queue.Length).ToBe(3);

                    queue.Take();
                    Expect(queue.Length).ToBe(2);

                    queue.Take();
                    Expect(queue.Length).ToBe(1);

                    queue.Take();
                    Expect(queue.Length).ToBe(0);
                });
            });

            Describe("QueueStructLinked Edge Cases", () =>
            {
                It("should handle rapid add and take operations", () =>
                {
                    var queue = new QueueStructLinked<int>();

                    for (int i = 0; i < 100; i++)
                    {
                        queue.Add(i);
                        var node = queue.Take();
                        Expect(node.Value).ToBe(i);
                        Expect(queue.Length).ToBe(0);
                    }
                });

                It("should handle different data types", () =>
                {
                    var stringQueue = new QueueStructLinked<string>();
                    stringQueue.Add("hello");
                    stringQueue.Add("world");

                    var node1 = stringQueue.Take();
                    var node2 = stringQueue.Take();

                    Expect(node1.Value).ToBe("world");
                    Expect(node2.Value).ToBe("hello");
                });

                It("should handle complex objects", () =>
                {
                    var queue = new QueueStructLinked<(int, string)>();
                    queue.Add((1, "first"));
                    queue.Add((2, "second"));

                    var node1 = queue.Take();
                    var node2 = queue.Take();

                    Expect(node1.Value.Item1).ToBe(2);
                    Expect(node1.Value.Item2).ToBe("second");
                    Expect(node2.Value.Item1).ToBe(1);
                    Expect(node2.Value.Item2).ToBe("first");
                });

                It("should handle large number of items", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    const int count = 1000;

                    // Add many items
                    for (int i = 0; i < count; i++)
                    {
                        queue.Add(i);
                    }

                    Expect(queue.Length).ToBe(count);

                    // Take all items
                    for (int i = count - 1; i >= 0; i--)
                    {
                        var node = queue.Take();
                        Expect(node.Value).ToBe(i);
                    }

                    Expect(queue.Length).ToBe(0);
                    Expect(queue.Take()).ToBeNull();
                });
            });
        }
    }
}

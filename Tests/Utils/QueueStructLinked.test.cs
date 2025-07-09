namespace Tests
{
    public class QueueStructLinkedTests : AbstractTest
    {
        public QueueStructLinkedTests()
        {
            Describe("QueueStructLinked", () =>
            {
                It("should add items and maintain head and tail", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(1);
                    Expect(queue.Head.Value).ToBe(1);
                    Expect(queue.Tail.Value).ToBe(1);
                    Expect(queue.Length).ToBe(1);

                    queue.Add(2);
                    Expect(queue.Head.Value).ToBe(2);
                    Expect(queue.Tail.Value).ToBe(1);
                    Expect(queue.Length).ToBe(2);
                });

                It("should take items in order", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(1);
                    queue.Add(2);

                    var n1 = queue.Take();
                    Expect(n1.Value).ToBe(2);
                    Expect(queue.Length).ToBe(1);

                    var n2 = queue.Take();
                    Expect(n2.Value).ToBe(1);
                    Expect(queue.Length).ToBe(0);
                });

                It("should clear items", () =>
                {
                    var queue = new QueueStructLinked<int>();
                    queue.Add(1);
                    queue.Add(2);
                    var oldHead = queue.Clear();
                    Expect(oldHead.Value).ToBe(2);
                    Expect(queue.Head).ToBeNull();
                    Expect(queue.Tail).ToBeNull();
                    Expect(queue.Length).ToBe(0);
                });

                It("should merge queues", () =>
                {
                    var q1 = new QueueStructLinked<int>();
                    q1.Add(1);
                    q1.Add(2);

                    var q2 = new QueueStructLinked<int>();
                    q2.Add(3);
                    q2.Add(4);

                    q1.Merge(q2);
                    Expect(q1.Length).ToBe(4);
                    Expect(q2.Length).ToBe(0);

                    var r1 = q1.Take();
                    var r2 = q1.Take();
                    var r3 = q1.Take();
                    var r4 = q1.Take();

                    Expect(r1.Value).ToBe(2);
                    Expect(r2.Value).ToBe(1);
                    Expect(r3.Value).ToBe(4);
                    Expect(r4.Value).ToBe(3);
                    Expect(q1.Length).ToBe(0);
                });
            });
        }
    }
}

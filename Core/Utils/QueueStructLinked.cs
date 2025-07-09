using System.Collections.Concurrent;

public class QueueStructLinked<T>
{
    public class Node
    {
        public T Value;
        public Node Next;
    }

    private static readonly ConcurrentStack<Node> Pool = new();

    public static void ReleaseChain(Node node)
    {
        while (node != null)
        {
            var next = node.Next;
            node.Next = null;
            node.Value = default!;
            Pool.Push(node);
            node = next;
        }
    }

    private static Node RentNode(T value)
    {
        if (!Pool.TryPop(out var node))
            node = new Node();

        node.Value = value;
        node.Next = null;
        return node;
    }

    public Node Head;
    public Node Tail;

    public void Add(T item)
    {
        var node = RentNode(item);
        node.Next = Head;
        if (Tail == null)
            Tail = node;
        Head = node;
    }

    public Node Clear()
    {
        var result = Head;
        Head = null;
        Tail = null;
        return result;
    }

    public Node Take()
    {
        if (Head == null)
            return null;

        var result = Head;
        if (Head == Tail)
        {
            Head = null;
            Tail = null;
        }
        else
        {
            Head = Head.Next;
        }
        return result;
    }

    public int Length
    {
        get
        {
            int val = 0;
            var current = Head;
            while (current != null)
            {
                current = current.Next;
                ++val;
            }
            return val;
        }
    }

    public void Merge(QueueStructLinked<T> other)
    {
        if (Head == null)
        {
            Head = other.Head;
            Tail = other.Tail;
        }
        else if (other.Head != null)
        {
            Tail.Next = other.Head;
            Tail = other.Tail;
        }
        other.Head = null;
        other.Tail = null;
    }
}

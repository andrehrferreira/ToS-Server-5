public class QueueStructLinked<T>
{
    public class Node
    {
        public T Value;
        public Node Next;

        public Node(T value)
        {
            Value = value;
        }
    }

    public Node Head;
    public Node Tail;

    public void Add(T item)
    {
        var node = new Node(item);
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

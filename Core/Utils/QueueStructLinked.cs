/*
 * QueueStructLinked
 * 
 * Author: Andre Ferreira
 * 
 * Copyright (c) Uzmi Games. Licensed under the MIT License.
 *    
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Concurrent;

public class QueueStructLinked<T>
{
    public class Node
    {
        public T Value;
        public Node Next;
    }

    private static readonly ConcurrentStack<Node> Pool = new();
    private const int MaxPoolSize = 1024;
    private static int _poolCount = 0;

    public static void ReleaseChain(Node node)
    {
        while (node != null)
        {
            var next = node.Next;
            node.Next = null;
            node.Value = default!;
            if (Interlocked.Increment(ref _poolCount) <= MaxPoolSize)
            {
                Pool.Push(node);
            }
            else
            {
                Interlocked.Decrement(ref _poolCount);
            }
            node = next;
        }
    }

    private static Node RentNode(T value)
    {
        if (!Pool.TryPop(out var node))
        {
            node = new Node();
        }
        else
        {
            Interlocked.Decrement(ref _poolCount);
        }

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

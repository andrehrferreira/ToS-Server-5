/*
* ByteBufferPool
* 
* Author: Diego Guedes
* Modified by: Andre Ferreira
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

namespace Networking
{
    public static class ByteBufferPool
    {
        static ByteBufferLinked Global = new ByteBufferLinked();

        [ThreadStatic]
        static ByteBufferLinked Local;

        public static ByteBuffer Acquire()
        {
            ByteBuffer buffer;

            if (Local == null)
            {
                lock (Global)
                {
                    buffer = Global.Take();
                }
            }
            else
            {
                buffer = Local.Take();

                if (buffer == null)
                {
                    lock (Global)
                    {
                        buffer = Global.Take();
                    }
                }
            }

            if (buffer == null)
                buffer = new ByteBuffer();

            return buffer;
        }

        public static void Release(ByteBuffer buffer)
        {
            if (Local == null)
                Local = new ByteBufferLinked();

            buffer.Reset();
            Local.Add(buffer);
        }

        public static void Merge()
        {
            if (Local != null && Local.Head != null)
            {
                lock (Global)
                {
                    Global.Merge(Local);
                }
            }
        }

        public static ByteBuffer Clear()
        {
            lock (Global)
            {
                return Global.Clear();
            }
        }
    }

    public class ByteBufferLinked
    {
        public ByteBuffer Head;
        public ByteBuffer Tail;

        public static readonly ByteBufferLinked Global = new ByteBufferLinked();

        [ThreadStatic]
        public static ByteBufferLinked Local;

        public void Add(ByteBuffer buffer)
        {
            buffer.Next = Head;

            if (Tail == null)
            {
                Tail = buffer;
            }

            Head = buffer;
        }

        public ByteBuffer Clear()
        {
            ByteBuffer result = Head;

            Head = null;
            Tail = null;

            return result;
        }

        public ByteBuffer Take()
        {
            if (Head == null)
            {
                return null;
            }
            else
            {
                ByteBuffer result = Head;

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
        }

        public int Length
        {
            get
            {
                int val = 0;

                ByteBuffer current = Head;

                while (current != null)
                {
                    current = current.Next;

                    ++val;
                }

                return val;
            }
        }

        public void Merge(ByteBufferLinked other)
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
}

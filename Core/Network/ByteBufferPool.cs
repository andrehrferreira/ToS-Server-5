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

public static class ByteBufferPool
{
    static ByteBufferLinked Global = new ByteBufferLinked();

    [ThreadStatic]
    static ByteBufferLinked Local;

    private static int _createdCount = 0;
    private static int _pooledCount = 0;

    public static (int created, int pooled) GetStats()
        => (_createdCount, _pooledCount);

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
        {
            buffer = new ByteBuffer();
            Interlocked.Increment(ref _createdCount);
        }
        else
        {
            Interlocked.Decrement(ref _pooledCount);
        }

        return buffer;
    }

    public static void Release(ByteBuffer buffer)
    {
        if (Local == null)
            Local = new ByteBufferLinked();

        buffer.Reset();
        Local.Add(buffer);
        Interlocked.Increment(ref _pooledCount);
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
            var res = Global.Clear();
            _pooledCount = 0;
            return res;
        }
    }

    public static void PreAllocate(int count)
    {
        if (Local == null)
            Local = new ByteBufferLinked();

        for (int i = 0; i < count; i++)
        {
            var buffer = new ByteBuffer();
            Local.Add(buffer);
            Interlocked.Increment(ref _createdCount);
            Interlocked.Increment(ref _pooledCount);
        }

        Merge();
    }

    public static void TrimExcess(int minCount)
    {
        Merge();

        lock (Global)
        {
            while (_pooledCount > minCount)
            {
                var buffer = Global.Take();

                if (buffer == null)
                    break;

                buffer.Destroy();
                Interlocked.Decrement(ref _createdCount);
                Interlocked.Decrement(ref _pooledCount);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

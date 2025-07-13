/*
* UdpBatchIO
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
* furnished to do so, subject to the following conditions.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using NanoSockets;

public unsafe struct PacketPointer
{
    public Address Addr;
    public byte* DataPtr;
    public int Length;
}

public struct PacketMemory
{
    public Address Addr;
    public Memory<byte> Buffer;
    public int Length;
}

public static class UdpBatchIO
{
    public static unsafe int ReceiveBatch(Socket socket, int maxMessages, Action<Address, byte[], int> callback)
    {
        int received = 0;

        for (int i = 0; i < maxMessages; i++)
        {
            int pollResult = UDP.Poll(socket, 0);
            if (pollResult <= 0)
                break;

            byte[] buffer = new byte[4096];
            Address address = default;

            fixed (byte* bufferPtr = buffer)
            {
                int len = UDP.Unsafe.Receive(socket, &address, bufferPtr, buffer.Length);

                if (len > 0)
                {
                    callback(address, buffer, len);
                    received++;
                }
                else
                {
                    break;
                }
            }
        }

        return received;
    }

    public static unsafe int SendBatch(Socket socket, ReadOnlySpan<(Address addr, byte[] data, int length)> packets)
    {
        int sent = 0;

        foreach (var packet in packets)
        {
            var address = packet.addr;
            fixed (byte* dataPtr = packet.data)
            {
                int result = UDP.Unsafe.Send(socket, &address, dataPtr, packet.length);

                if (result > 0)
                    sent++;
            }
        }

        return sent;
    }

    public static unsafe void SendBatch(Socket socket, ReadOnlySpan<PacketPointer> packets)
    {
        foreach (var packet in packets)
        {
            var address = packet.Addr;
            UDP.Unsafe.Send(socket, &address, packet.DataPtr, packet.Length);
        }
    }

    public static unsafe int SendBatch(Socket socket, List<(Address addr, byte[] data, int length)> packets)
    {
        int sent = 0;

        foreach (var packet in packets)
        {
            var address = packet.addr;
            fixed (byte* dataPtr = packet.data)
            {
                int result = UDP.Unsafe.Send(socket, &address, dataPtr, packet.length);

                if (result > 0)
                    sent++;
            }
        }

        return sent;
    }

    public static unsafe void SendBatch(Socket socket, ReadOnlySpan<PacketMemory> packets)
    {
        foreach (var packet in packets)
        {
            var address = packet.Addr;
            var span = packet.Buffer.Span;

            fixed (byte* dataPtr = span)
            {
                UDP.Unsafe.Send(socket, &address, dataPtr, packet.Length);
            }
        }
    }
}

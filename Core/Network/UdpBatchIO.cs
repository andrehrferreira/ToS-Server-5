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

using System.Buffers;
using System.Net;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

public struct PacketPointer
{
    public EndPoint Addr;
    public IntPtr DataPtr;
    public int Length;
}

public struct PacketMemory
{
    public EndPoint Addr;
    public Memory<byte> Buffer;
    public int Length;
}

public static class UdpBatchIO
{
#if WINDOWS
    private static bool _initialized = false;
    private static Action<object, byte[], int> _receiveCallback;
#endif

    public static int ReceiveBatch(Socket socket, int maxMessages, Action<EndPoint, byte[], int> callback)
    {
#if LINUX
        int fd = (int)socket.Handle;
        return UdpBatchIO_Linux.ReceiveBatch(fd, maxMessages, (addr, data, len) =>
        {
            var endPoint = ConvertToEndPoint(addr);
            callback(endPoint, data, len);
        });
#elif WINDOWS
        if (!_initialized)
        {
            UdpBatchIO_Windows.Initialize(socket, callback);
            _initialized = true;
        }
        return 0;
#else
        byte[] buffer = new byte[4096];
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            int received = socket.ReceiveFrom(buffer, ref remoteEP);
            callback(remoteEP, buffer, received);
            return 1;
        }
        catch (SocketException)
        {
            return 0;
        }
#endif
    }

    public static int SendBatch(Socket socket, ReadOnlySpan<(EndPoint addr, byte[] data, int length)> packets)
    {
    #if LINUX
        int fd = (int)socket.Handle;
        var nativePackets = new List<(UdpBatchIO_Linux.sockaddr_in, byte[], int)>(packets.Length);

        foreach (var p in packets)
        {
            var nativeAddr = FormatAddress(p.addr);
            if (nativeAddr is UdpBatchIO_Linux.sockaddr_in sockAddr)
                nativePackets.Add((sockAddr, p.data, p.length));
        }

        return UdpBatchIO_Linux.SendBatch(fd, nativePackets);

    #elif WINDOWS
            UdpBatchIO_Windows.SendBatch(socket, packets);
            return packets.Length;
    #else
        int sent = 0;
        foreach (var p in packets)
        {
            if (p.addr is EndPoint ep)
            {
                socket.SendTo(p.data, 0, p.length, SocketFlags.None, ep);
                sent++;
            }
        }
        return sent;
    #endif
    }

    public static unsafe void SendBatch(Socket socket, ReadOnlySpan<PacketPointer> packets)
    {
#if WINDOWS
        UdpBatchIO_Windows.SendBatch(socket, packets);
#elif LINUX
        // Convert PacketPointer to the format expected by Linux implementation
        var nativePackets = new List<(UdpBatchIO_Linux.sockaddr_in, byte[], int)>(packets.Length);

        foreach (var packet in packets)
        {
            var nativeAddr = FormatAddress(packet.Addr);
            if (nativeAddr is UdpBatchIO_Linux.sockaddr_in sockAddr)
            {
                byte[] data = new byte[packet.Length];
                fixed (byte* dest = data)
                {
                    Buffer.MemoryCopy((byte*)packet.DataPtr, dest, packet.Length, packet.Length);
                }
                nativePackets.Add((sockAddr, data, packet.Length));
            }
        }

        int fd = (int)socket.Handle;
        UdpBatchIO_Linux.SendBatch(fd, nativePackets);
#else
        // fallback implementation
#endif
    }

    private static EndPoint ConvertToEndPoint(object addr)
    {
#if LINUX
        if (addr is UdpBatchIO_Linux.sockaddr_in linuxAddr)
        {
            uint ip = linuxAddr.sin_addr;
            byte[] ipBytes = new byte[]
            {
                (byte)((ip >> 24) & 0xFF),
                (byte)((ip >> 16) & 0xFF),
                (byte)((ip >> 8) & 0xFF),
                (byte)(ip & 0xFF)
            };
            var ipAddress = new IPAddress(ipBytes);
            int port = (ushort)IPAddress.NetworkToHostOrder((short)linuxAddr.sin_port);
            return new IPEndPoint(ipAddress, port);
        }
#endif
        return addr as EndPoint ?? new IPEndPoint(IPAddress.Any, 0);
    }

    private static object FormatAddress(EndPoint ep)
    {
#if LINUX
        if (ep is IPEndPoint ip)
        {
            byte[] bytes = ip.Address.MapToIPv4().GetAddressBytes();
            uint ipUint = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
            return new UdpBatchIO_Linux.sockaddr_in
            {
                sin_family = (ushort)AddressFamily.InterNetwork,
                sin_port = (ushort)IPAddress.HostToNetworkOrder((short)ip.Port),
                sin_addr = ipUint
            };
        }
#endif
        return ep;
    }

#if WINDOWS
    public static void Shutdown()
    {
        UdpBatchIO_Windows.Shutdown();
        _initialized = false;
    }
#endif
}

#if LINUX
public static unsafe class UdpBatchIO_Linux
{
    private const int MaxDatagramSize = 65535;

    [StructLayout(LayoutKind.Sequential)]
    public struct iovec
    {
        public void* iov_base;
        public UIntPtr iov_len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct msghdr
    {
        public void* msg_name;
        public uint msg_namelen;
        public iovec* msg_iov;
        public UIntPtr msg_iovlen;
        public void* msg_control;
        public UIntPtr msg_controllen;
        public int msg_flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct mmsghdr
    {
        public msghdr msg_hdr;
        public uint msg_len;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct sockaddr_in
    {
        public ushort sin_family;
        public ushort sin_port;
        public uint sin_addr;
        public fixed byte sin_zero[8];
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int recvmmsg(int sockfd, mmsghdr* msgvec, uint vlen, int flags, IntPtr timeout);

    [DllImport("libc", SetLastError = true)]
    private static extern int sendmmsg(int sockfd, mmsghdr* msgvec, uint vlen, int flags);

    public static int ReceiveBatch(int socketFd, int maxMessages, Action<sockaddr_in, byte[], int> callback)
    {
        var msgVec = new mmsghdr[maxMessages];
        var iovecs = new iovec[maxMessages];
        var buffers = new byte[maxMessages][];
        var handles = new GCHandle[maxMessages];
        var addrs = new sockaddr_in[maxMessages];

        for (int i = 0; i < maxMessages; i++)
        {
            buffers[i] = new byte[MaxDatagramSize];
            handles[i] = GCHandle.Alloc(buffers[i], GCHandleType.Pinned);
        }

        int received = 0;

        fixed (sockaddr_in* addrPtr = addrs)
        fixed (mmsghdr* msgs = msgVec)
        fixed (iovec* iovPtr = iovecs)
        {
            for (int i = 0; i < maxMessages; i++)
            {
                iovPtr[i].iov_base = (void*)handles[i].AddrOfPinnedObject();
                iovPtr[i].iov_len = (UIntPtr)buffers[i].Length;

                msgs[i].msg_hdr.msg_name = addrPtr + i;
                msgs[i].msg_hdr.msg_namelen = (uint)sizeof(sockaddr_in);
                msgs[i].msg_hdr.msg_iov = &iovPtr[i];
                msgs[i].msg_hdr.msg_iovlen = (UIntPtr)1;
                msgs[i].msg_hdr.msg_control = null;
                msgs[i].msg_hdr.msg_controllen = UIntPtr.Zero;
                msgs[i].msg_hdr.msg_flags = 0;
            }

            received = recvmmsg(socketFd, msgs, (uint)maxMessages, 0, IntPtr.Zero);

            if (received > 0)
            {
                for (int i = 0; i < received; i++)
                {
                    int len = (int)msgs[i].msg_len;
                    callback(addrs[i], buffers[i], len);
                }
            }
        }

        for (int i = 0; i < maxMessages; i++)
            if (handles[i].IsAllocated)
                handles[i].Free();

        return received;
    }

    public static int SendBatch(int socketFd, List<(sockaddr_in addr, byte[] data, int length)> packets)
    {
        int count = packets.Count;
        var msgVec = new mmsghdr[count];
        var iovecs = new iovec[count];
        var handles = new GCHandle[count];
        var addrs = new sockaddr_in[count];

        for (int i = 0; i < count; i++)
            addrs[i] = packets[i].addr;

        int sent = 0;

        fixed (sockaddr_in* addrPtr = addrs)
        fixed (mmsghdr* msgs = msgVec)
        fixed (iovec* iovPtr = iovecs)
        {
            for (int i = 0; i < count; i++)
            {
                handles[i] = GCHandle.Alloc(packets[i].data, GCHandleType.Pinned);

                iovPtr[i].iov_base = (void*)handles[i].AddrOfPinnedObject();
                iovPtr[i].iov_len = (UIntPtr)packets[i].length;

                msgs[i].msg_hdr.msg_name = addrPtr + i;
                msgs[i].msg_hdr.msg_namelen = (uint)sizeof(sockaddr_in);
                msgs[i].msg_hdr.msg_iov = &iovPtr[i];
                msgs[i].msg_hdr.msg_iovlen = (UIntPtr)1;
                msgs[i].msg_hdr.msg_control = null;
                msgs[i].msg_hdr.msg_controllen = UIntPtr.Zero;
                msgs[i].msg_hdr.msg_flags = 0;
                msgs[i].msg_len = (uint)packets[i].length;
            }

            sent = sendmmsg(socketFd, msgs, (uint)count, 0);
        }

        for (int i = 0; i < count; i++)
            if (handles[i].IsAllocated)
                handles[i].Free();

        return sent;
    }
}
#endif

#if WINDOWS
public static class UdpBatchIO_Windows
{
    private const int BufferSize = 2048;
    private const int ConcurrentReceives = 256;

    private static Socket _socket;
    private static Action<EndPoint, byte[], int> _callback;
    private static Action<EndPoint, ReadOnlyMemory<byte>, int> _callbackMemory;
    private static List<SocketAsyncEventArgs> _receivePool = new();
    private static ConcurrentStack<SocketAsyncEventArgs> _sendPool = new();

    private static SocketAsyncEventArgs RentSendArgs()
    {
        if (!_sendPool.TryPop(out var args))
        {
            args = new SocketAsyncEventArgs();
            args.Completed += IOCompleted;
        }
        return args;
    }

    private static void ReturnSendArgs(SocketAsyncEventArgs args)
    {
        args.SetBuffer(null, 0, 0);
        args.RemoteEndPoint = null;
        args.UserToken = null;
        _sendPool.Push(args);
    }
    public static void Initialize(Socket socket, Action<EndPoint, byte[], int> callback)
    {
        _socket = socket;
        _callback = callback;

        for (int i = 0; i < ConcurrentReceives; i++)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0)
            };

            args.SetBuffer(buffer, 0, BufferSize);
            args.Completed += IOCompleted;

            _receivePool.Add(args);

            if (!_socket.ReceiveFromAsync(args))
                ProcessReceive(args);
        }
    }

    public static void Initialize(Socket socket, Action<EndPoint, ReadOnlyMemory<byte>, int> callback)
    {
        _socket = socket;
        _callbackMemory = callback;

        for (int i = 0; i < ConcurrentReceives; i++)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0)
            };

            args.SetBuffer(buffer, 0, BufferSize);
            args.Completed += IOCompleted;

            _receivePool.Add(args);

            if (!_socket.ReceiveFromAsync(args))
                ProcessReceive(args);
        }
    }

    private static void IOCompleted(object sender, SocketAsyncEventArgs e)
    {
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.ReceiveFrom:
                ProcessReceive(e);
                break;
            case SocketAsyncOperation.SendTo:
                ProcessSend(e);
                break;
        }
    }

    private static void ProcessReceive(SocketAsyncEventArgs e)
    {
        if(_socket == null)
            return;

        if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
        {
            _callback?.Invoke(e.RemoteEndPoint, e.Buffer, e.BytesTransferred);

            _callbackMemory?.Invoke(e.RemoteEndPoint, e.Buffer.AsMemory(0, e.BytesTransferred), e.BytesTransferred);
        }

        try
        {
            if (!_socket.ReceiveFromAsync(e))
                ProcessReceive(e);
        }
        catch (ObjectDisposedException)
        {
            // Ignore on shutdown
        }
    }

    public static void SendBatch(Socket socket, List<(EndPoint addr, byte[] data, int length)> packets)
    {
        foreach (var packet in packets)
        {
            var args = RentSendArgs();
            args.RemoteEndPoint = packet.addr;
            args.SetBuffer(packet.data, 0, packet.length);

            if (!socket.SendToAsync(args))
                ProcessSend(args);
        }
    }

    public static void SendBatch(Socket socket, ReadOnlySpan<(EndPoint addr, byte[] data, int length)> packets)
    {
        foreach (var packet in packets)
        {
            var args = RentSendArgs();
            args.RemoteEndPoint = packet.addr;
            args.SetBuffer(packet.data, 0, packet.length);

            if (!socket.SendToAsync(args))
                ProcessSend(args);
        }
    }

    public static unsafe void SendBatch(Socket socket, ReadOnlySpan<PacketPointer> packets)
    {
        foreach (var packet in packets)
        {
            byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(packet.Length);

            try
            {
                fixed (byte* dest = tempBuffer)
                {
                    Buffer.MemoryCopy((byte*)packet.DataPtr, dest, packet.Length, packet.Length);
                }

                var args = RentSendArgs();
                args.RemoteEndPoint = packet.Addr;
                args.SetBuffer(tempBuffer, 0, packet.Length);
                args.UserToken = tempBuffer;

                if (!socket.SendToAsync(args))
                {
                    ProcessSend(args);
                }
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(tempBuffer);
                throw;
            }
        }
    }

    public static void SendBatch(Socket socket, ReadOnlySpan<PacketMemory> packets)
    {
        foreach (var packet in packets)
        {
            var args = RentSendArgs();
            args.RemoteEndPoint = packet.Addr;

            args.SetBuffer(packet.Buffer.Slice(0, packet.Length));

            if (!socket.SendToAsync(args))
                ProcessSend(args);
        }
    }

    private static void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.UserToken is byte[] buffer)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            e.UserToken = null;
        }

        ReturnSendArgs(e);
    }

    public static void Shutdown()
    {
        foreach (var args in _receivePool)
        {
            ArrayPool<byte>.Shared.Return(args.Buffer);
            args.Dispose();
        }

        _receivePool.Clear();

        while (_sendPool.TryPop(out var sendArgs))
            sendArgs.Dispose();
    }
}
#endif

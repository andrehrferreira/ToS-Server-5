// UdpBatchIO_Linux.cs
#if LINUX
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

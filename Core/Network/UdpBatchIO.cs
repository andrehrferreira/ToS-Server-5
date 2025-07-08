using System.Net;
using System.Net.Sockets;

public static class UdpBatchIO
{
#if WINDOWS
    private static bool _initialized = false;
    private static Action<object, byte[], int> _receiveCallback;
#endif

    public static int ReceiveBatch(Socket socket, int maxMessages, Action<object, byte[], int> callback)
    {
#if LINUX
        int fd = (int)socket.Handle;
        return UdpBatchIO_Linux.ReceiveBatch(fd, maxMessages, (addr, data, len) =>
        {
            callback(addr, data, len);
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

    public static int SendBatch(Socket socket, ReadOnlySpan<(object addr, byte[] data, int length)> packets)
    {
#if LINUX
    int fd = (int)socket.Handle;
    var nativePackets = new List<(UdpBatchIO_Linux.sockaddr_in, byte[], int)>(packets.Length);

    foreach (var p in packets)
    {
        if (p.addr is UdpBatchIO_Linux.sockaddr_in nativeAddr)
            nativePackets.Add((nativeAddr, p.data, p.length));
    }

    return UdpBatchIO_Linux.SendBatch(fd, nativePackets);

#elif WINDOWS
        var formattedPackets = new List<(EndPoint addr, byte[] data, int length)>(packets.Length);
        foreach (var p in packets)
        {
            if (p.addr is EndPoint ep)
                formattedPackets.Add((ep, p.data, p.length));
        }

        UdpBatchIO_Windows.SendBatch(socket, formattedPackets);
        return formattedPackets.Count;

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


#if WINDOWS
    public static void Shutdown()
    {
        UdpBatchIO_Windows.Shutdown();
        _initialized = false;
    }
#endif
}

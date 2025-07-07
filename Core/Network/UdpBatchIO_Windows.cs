#if WINDOWS
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

public static class UdpBatchIO_Windows
{
    private const int BufferSize = 2048;
    private const int ConcurrentReceives = 256;

    private static Socket _socket;
    private static Action<EndPoint, byte[], int> _callback;
    private static List<SocketAsyncEventArgs> _receivePool = new();

    public static void Initialize(Socket socket, Action<EndPoint, byte[], int> callback)
    {
        _socket = socket;
        _callback = callback;

        for (int i = 0; i < ConcurrentReceives; i++)
        {
            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0)
            };
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            args.SetBuffer(buffer, 0, BufferSize);
            args.Completed += IOCompleted;

            _receivePool.Add(args);

            if (!_socket.ReceiveFromAsync(args))
                ProcessReceive(args);
        }

        Console.WriteLine($"[UdpBatchIO_Windows] Initialized with {ConcurrentReceives} concurrent receives.");
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
        if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
        {
            _callback?.Invoke(e.RemoteEndPoint, e.Buffer, e.BytesTransferred);
        }
        else if (e.SocketError != SocketError.Success)
        {
            Console.WriteLine($"[UdpBatchIO_Windows] Receive error: {e.SocketError}");
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
            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = packet.addr
            };
            args.SetBuffer(packet.data, 0, packet.length);
            args.Completed += IOCompleted;

            if (!socket.SendToAsync(args))
                ProcessSend(args);
        }
    }

    private static void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success)
            Console.WriteLine($"[UdpBatchIO_Windows] Send error: {e.SocketError}");

        e.Dispose();
    }

    public static void Shutdown()
    {
        foreach (var args in _receivePool)
        {
            ArrayPool<byte>.Shared.Return(args.Buffer);
            args.Dispose();
        }
        _receivePool.Clear();

        Console.WriteLine("[UdpBatchIO_Windows] Shutdown completed.");
    }
}
#endif

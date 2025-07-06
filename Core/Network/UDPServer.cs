using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public class UDPServerOptions
{
    public bool UseXOREncode { get; set; } = false;
    public byte[] XORKey { get; set; } = null;
    public bool UseEncryption { get; set; } = false;
    public byte[] EncryptionKey { get; set; } = null;
    public byte[] EncryptionIV { get; set; } = null;
}


public sealed class UDPServer
{
    private static UDPServerOptions _options;

    private static PacketFlags _baseFlags = PacketFlags.None;

    private static Socket ServerSocket;

    private static bool Running = true;

    private static ConcurrentDictionary<EndPoint, UDPSocket> Clients =
        new ConcurrentDictionary<EndPoint, UDPSocket>();

    private static ByteBufferLinked GlobalSendQueue = new ByteBufferLinked();

    [ThreadStatic]
    static ByteBufferLinked LocalSendQueue;

    private static Thread SendThread;

    private static Thread ReceiveThread;

    private static AutoResetEvent SendEvent;

    public static TimeSpan ReliableTimeout = TimeSpan.FromMilliseconds(250f);

    public delegate bool ConnectionHandler(UDPSocket socket, string token);

    private static ConnectionHandler _connectionHandler;

    private static World _worldRef;

    public static void SetWorld(World worldRef)
    {
        _worldRef = worldRef;
    }

    public static World GetWorld()
    {
        return _worldRef;
    }

    public static int GetRandomId()
    {
        return BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & 0x7FFFFFFF;
    }

    public static bool Init(
        int port,
        ConnectionHandler connectionHandler,
        UDPServerOptions options = null
    )
    {
        _options = options ?? new UDPServerOptions();
        _connectionHandler = connectionHandler;

        if (_options.UseXOREncode)
            _baseFlags = PacketFlagsUtils.AddFlag(_baseFlags, PacketFlags.XOR);

        if (_options.UseEncryption)
            _baseFlags = PacketFlagsUtils.AddFlag(_baseFlags, PacketFlags.Encrypted);

        try
        {
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            ServerSocket.Bind(new IPEndPoint(IPAddress.Any, port));

            Running = true;

            Console.WriteLine($"UDP server started on port {port}");

            SendOnBackgroundThread();

            ReceiveOnBackgroundThread();

            RunMainLoop();

            return true;
        }
        catch (Exception ex)
        {
            Running = false;
            Console.WriteLine($"Error initializing UDP server: {ex.Message}");
            return false;
        }
    }

    public static void Stop()
    {
        Running = false;
        ServerSocket?.Close();
        Console.WriteLine("UDP server stopped.");
    }

    public static void ReceiveOnBackgroundThread()
    {
        if (ReceiveThread == null)
        {
            ReceiveThread = new Thread(() =>
            {
                ReceiveThread = Thread.CurrentThread;

                Stopwatch pingTimer = Stopwatch.StartNew();
                Stopwatch cleanupTimer = Stopwatch.StartNew();

                try
                {
                    while (true)
                    {
                        Pool(100);

                        if (pingTimer.Elapsed >= TimeSpan.FromSeconds(5))
                        {
                            foreach (var kv in Clients)
                            {
                                kv.Value.PingSentAt = DateTime.UtcNow;

                                ByteBuffer pingBuffer = ByteBufferPool.Acquire();
                                pingBuffer.Reliable = false;
                                pingBuffer.Write(PacketType.Ping);
                                long microTimestamp = Stopwatch.GetTimestamp();
                                pingBuffer.Write(microTimestamp);
                                kv.Value.Send(pingBuffer);
                            }

                            pingTimer.Restart();
                        }

                        if (cleanupTimer.Elapsed >= TimeSpan.FromSeconds(5))
                        {
                            DateTime now = DateTime.UtcNow;

                            foreach (var kv in Clients)
                            {
                                if (kv.Value.TimeoutLeft <= 0)
                                {
                                    if (Clients.TryRemove(kv.Key, out var client))
                                    {
                                        client.OnDisconnect();
                                        Console.WriteLine($"Client timeout/disconnected: {kv.Key}");
                                    }
                                }
                            }

                            cleanupTimer.Restart();
                        }

                        MergeSendQueue();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro: {ex.Message}");
                }
            });

            ReceiveThread.IsBackground = true;

            ReceiveThread.Start();
        }
    }

    public static void SendOnBackgroundThread()
    {
        SendEvent = new AutoResetEvent(false);

        SendThread = new Thread(() =>
        {
            try
            {
                while (true)
                {
                    bool sent;

                    do
                    {
                        sent = false;

                        ByteBuffer buffer = null;

                        EndPoint addr;

                        lock (GlobalSendQueue)
                        {
                            buffer = GlobalSendQueue.Clear();
                        }

                        if (buffer != null)
                        {
                            sent = true;

                            do
                            {
                                ByteBuffer next = buffer.Next;

                                try
                                {
                                    if (buffer.Connection != null)
                                    {
                                        addr = buffer.Connection.RemoteEndPoint;

                                        if (buffer.Data != null && !buffer.IsDestroyed && buffer.Length > 0)
                                            ServerSocket.SendTo(buffer.Data, 0, buffer.Length, SocketFlags.None, addr);

                                        ByteBufferPool.Release(buffer);
                                    }
                                    else
                                    {
                                        ByteBufferPool.Release(buffer);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ByteBufferPool.Release(buffer);
                                }

                                buffer = next;
                            }
                            while (buffer != null);
                        }

                        ByteBufferPool.Merge();
                    }
                    while (sent);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                lock (GlobalSendQueue)
                {
                    GlobalSendQueue = new ByteBufferLinked();
                }
            }
        });

        SendThread.IsBackground = true;

        SendThread.Start();
    }

    public static bool Pool(int timeout)
    {
        EndPoint address = new IPEndPoint(IPAddress.Any, 0);
        byte[] bufferRaw = ArrayPool<byte>.Shared.Rent(3600);
        int receivedBytes = 0;

        try
        {
            ServerSocket.ReceiveTimeout = timeout;
            receivedBytes = ServerSocket.ReceiveFrom(bufferRaw, ref address);
            ServerSocket.ReceiveTimeout = 0;
        }
        catch (SocketException ex)
        {
            ArrayPool<byte>.Shared.Return(bufferRaw);
            return false;
        }

        if (receivedBytes > 0)
        {
            UDPSocket conn;
            ByteBuffer buffer = ByteBufferPool.Acquire();

            buffer.Data = bufferRaw;
            buffer.Length = receivedBytes;
            buffer.Offset = 0;

            PacketType type = buffer.ReadPacketType();

            switch (type)
            {
                case PacketType.Connect:
                    {
                        string token = null;

                        if (buffer.Length - buffer.Offset > 0)
                        {
                            int tokenLen = buffer.ReadByte();
                            if (tokenLen > 0 && buffer.Length - buffer.Offset >= tokenLen)
                            {
                                byte[] tokenBytes = new byte[tokenLen];
                                buffer.Read(tokenBytes, 0, tokenLen);
                                token = System.Text.Encoding.UTF8.GetString(tokenBytes);
                            }
                        }

                        lock (Clients)
                        {
                            if (!Clients.ContainsKey(address))
                            {
                                UDPSocket newSocket = new UDPSocket
                                {
                                    Id = GetRandomId(),
                                    RemoteEndPoint = address,
                                    ServerSocket = ServerSocket,
                                    TimeoutLeft = 30f,
                                    State = ConnectionState.Connecting,
                                    Flags = _baseFlags
                                };

                                QueueBuffer.AddSocket(newSocket.Id.ToString(), newSocket);
                                bool valid = _connectionHandler?.Invoke(newSocket, token) ?? true;

                                if (valid)
                                {
                                    Clients.TryAdd(address, newSocket);
                                }
                            }
                        }

                        int ID = GetRandomId();

                        if (Clients.TryGetValue(address, out conn))
                        {
                            conn.Send(PacketConnectionAccepted.Serialize(ID));
                            conn.State = ConnectionState.Connected;
                        }

                        Console.WriteLine($"Client connected: {address.ToString()} ID:{ID}");
                        ByteBufferPool.Release(buffer);
                    }
                    break;
                case PacketType.Ping:
                    if (Clients.TryGetValue(address, out conn) && conn.ServerSocket != null)
                    {
                        conn.TimeoutLeft = 30f;
                        conn.PingSentAt = DateTime.UtcNow;
                        conn.Send(PacketPong.Serialize());
                    }

                    ByteBufferPool.Release(buffer);
                    break;
                case PacketType.Pong:
                    if (Clients.TryGetValue(address, out conn) && conn.ServerSocket != null)
                        PacketPong.Deserialize(buffer, conn);

                    ByteBufferPool.Release(buffer);
                    break;
                case PacketType.Disconnect:
                    if (Clients.TryRemove(address, out conn))
                    {
                        conn.OnDisconnect();
                        Console.WriteLine($"Client disconnected: {address}");
                    }
                    ByteBufferPool.Release(buffer);
                    break;
                case PacketType.Reliable:
                case PacketType.Unreliable:
                case PacketType.Ack:
                    if (Clients.TryGetValue(address, out conn) && conn.ServerSocket != null)
                    {
                    }

                    ByteBufferPool.Release(buffer);
                    break;
                default:
                    Console.WriteLine($"Unknown packet type: {type}");
                    ByteBufferPool.Release(buffer);
                    break;
            }
        }

        MergeSendQueue();

        return true;
    }

    public static void Send(byte[] data, UDPSocket socket)
    {
        if (LocalSendQueue == null)
            LocalSendQueue = new ByteBufferLinked();

        if(data.Length > 0){
            ByteBuffer buffer = ByteBufferPool.Acquire();
            buffer.Data = data;
            buffer.Length = data.Length;
            buffer.Connection = socket;
            LocalSendQueue.Add(buffer);
        }
    }

    public static void Send(ByteBuffer buffer)
    {
        if (LocalSendQueue == null)
            LocalSendQueue = new ByteBufferLinked();

        if(buffer.Length > 0){
            LocalSendQueue.Add(buffer);
        }
    }

    private static void MergeSendQueue()
    {
        if (LocalSendQueue != null && LocalSendQueue.Head != null)
        {
            lock (GlobalSendQueue)
            {
                GlobalSendQueue.Merge(LocalSendQueue);
            }
        }
    }

    public static void Flush()
    {
        SendEvent.Set();
    }

    public static void Update(float delta)
    {
        MergeSendQueue();

        lock (Clients)
        {
            foreach (var kv in Clients)
            {
                kv.Value.Update(delta);
            }
        }
    }

    public static void RunMainLoop()
    {
        const int targetFps = 60;
        const double targetFrameTime = 1000.0 / targetFps;
        Stopwatch sw = new Stopwatch();
        sw.Start();
        double lastTime = sw.Elapsed.TotalMilliseconds;

        while (Running)
        {
            double now = sw.Elapsed.TotalMilliseconds;
            double delta = now - lastTime;
            lastTime = now;

            Update((float)(delta / 1000.0));

            double elapsed = sw.Elapsed.TotalMilliseconds - now;
            int sleep = (int)(targetFrameTime - elapsed);

            if (sleep > 0)
                Thread.Sleep(sleep);
        }
    }
}

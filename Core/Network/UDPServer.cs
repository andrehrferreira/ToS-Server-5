using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public class UDPServerOptions
{
    public int ReceiveTimeout { get; set; } = 10;
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

    private static IntegrityKeyTable _integrityKeyTable;

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

    public static void SetWorld(World worldRef) => _worldRef = worldRef;
    public static World GetWorld() => _worldRef;

    public static int GetRandomId() =>
        BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & 0x7FFFFFFF;

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
            if (IntegritySystem.GetCurrentTable(out var table))
            {
                _integrityKeyTable = table;

                ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                ServerSocket.Bind(new IPEndPoint(IPAddress.Any, port));

                ServerSocket.ReceiveTimeout = _options.ReceiveTimeout;

                Running = true;

#if DEBUG
                Console.WriteLine($"UDP server started on port {port}");
#endif

                SendOnBackgroundThread();

                ReceiveOnBackgroundThread();

                new Thread(RunMainLoop) { IsBackground = true }.Start();
            }

            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine($"Error initializing UDP server: {ex.Message}");
#endif
            Running = false;

            return false;
        }
    }

    public static void Stop()
    {
        Running = false;
        ServerSocket?.Close();

#if DEBUG
        Console.WriteLine("UDP server stopped.");
#endif
    }

    public static void ReceiveOnBackgroundThread()
    {
        if (ReceiveThread != null) return;

        ReceiveThread = new Thread(() =>
        {
            ReceiveThread = Thread.CurrentThread;

            Stopwatch pingTimer = Stopwatch.StartNew();
            Stopwatch checkIntegrityTimer = Stopwatch.StartNew();
            Stopwatch cleanupTimer = Stopwatch.StartNew();

            try
            {
                while (true)
                {
                    Pool(10);

                    if (pingTimer.Elapsed >= TimeSpan.FromSeconds(5))
                    {
                        foreach (var kv in Clients)
                        {
                            kv.Value.PingSentAt = DateTime.UtcNow;
                            kv.Value.Send(PacketPing.Serialize(Stopwatch.GetTimestamp()));
                        }

                        pingTimer.Restart();
                    }

                    if (checkIntegrityTimer.Elapsed >= TimeSpan.FromSeconds(30))
                    {
                        foreach (var kv in Clients)
                        {
                            kv.Value.IntegrityCheckSentAt = DateTime.UtcNow;
                            kv.Value.IntegrityCheck = IntegrityKeyTableGenerator.GenerateIndex();
                            kv.Value.Send(PacketCheckIntegrity.Serialize(kv.Value.IntegrityCheck));
                        }

                        checkIntegrityTimer.Restart();
                    }

                    if (cleanupTimer.Elapsed >= TimeSpan.FromSeconds(5))
                    {
                        foreach (var kv in Clients)
                        {
                            if (kv.Value.TimeoutLeft <= 0)
                            {
                                if (Clients.TryRemove(kv.Key, out var client))
                                {
                                    client.OnDisconnect();
#if DEBUG
                                    Console.WriteLine($"Client timeout/disconnected: {kv.Key}");
#endif
                                }
                            }
                        }

                        cleanupTimer.Restart();
                    }

                    MergeSendQueue();

                    Thread.Sleep(1);
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

    public static void SendOnBackgroundThread()
    {
        SendEvent = new AutoResetEvent(false);

        SendThread = new Thread(() =>
        {
            while (Running)
            {
                SendEvent.WaitOne(1);

                ByteBuffer buffer;
                lock (GlobalSendQueue)
                {
                    buffer = GlobalSendQueue.Clear();
                }

                while (buffer != null)
                {
                    var next = buffer.Next;
                    try
                    {
                        if (buffer.Connection != null)
                        {
                            var addr = buffer.Connection.RemoteEndPoint;

                            //if (_options.UseXOREncode && _options.XORKey != null)
                            //    BufferUtils.ApplyXOR(buffer.Data, buffer.Length, _options.XORKey);

                            //if (_options.UseEncryption && _options.EncryptionKey != null && _options.EncryptionIV != null)
                            //    BufferUtils.ApplyEncryption(buffer.Data, buffer.Length, _options.EncryptionKey, _options.EncryptionIV);

                            if (buffer.Data != null && !buffer.IsDestroyed && buffer.Length > 0)
                                ServerSocket.SendTo(buffer.Data, 0, buffer.Length, SocketFlags.None, addr);
                        }
                    }
                    catch { }
                    finally
                    {
                        ByteBufferPool.Release(buffer);
                    }

                    buffer = next;
                }

                ByteBufferPool.Merge();
            }
        });

        SendThread.IsBackground = true;
        SendThread.Start();
    }

    public static bool Pool(int timeout)
    {
        EndPoint address = new IPEndPoint(IPAddress.Any, 0);
        byte[] bufferRaw = ArrayPool<byte>.Shared.Rent(3600);

        try
        {
            int receivedBytes = ServerSocket.ReceiveFrom(bufferRaw, ref address);

            /*if (!WAFRateLimiter.AllowPacket(address))
            {
#if DEBUG
                Console.WriteLine($"[WAF] Packet dropped due to rate limit from {address}");
#endif

                ArrayPool<byte>.Shared.Return(bufferRaw);
                return false;
            }*/

            var buffer = ByteBufferPool.Acquire();
            buffer.Assign(bufferRaw, receivedBytes);
            var type = buffer.ReadPacketType();

            // Optionally decrypt / XOR here if needed on receive
            //if (_options.UseXOREncode && _options.XORKey != null)
            //    BufferUtils.ApplyXOR(buffer.Data, buffer.Length, _options.XORKey);

            //if (_options.UseEncryption && _options.EncryptionKey != null && _options.EncryptionIV != null)
            //    BufferUtils.ApplyDecryption(buffer.Data, buffer.Length, _options.EncryptionKey, _options.EncryptionIV);

            HandlePacket(type, buffer, address);

            return true;
        }
        catch (SocketException)
        {
            ArrayPool<byte>.Shared.Return(bufferRaw);
            return false;
        }
    }

    private static void HandlePacket(PacketType type, ByteBuffer buffer, EndPoint address)
    {
        UDPSocket conn;

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

                if (!Clients.TryGetValue(address, out conn))
                {
                    var newSocket = new UDPSocket
                    {
                        Id = GetRandomId(),
                        RemoteEndPoint = address,
                        ServerSocket = ServerSocket,
                        TimeoutLeft = 30f,
                        State = ConnectionState.Connecting,
                        Flags = _baseFlags
                    };

                    bool valid = _connectionHandler?.Invoke(newSocket, token) ?? true;

                    if (Clients.TryAdd(address, newSocket))
                    {
                        QueueBuffer.AddSocket(newSocket.Id.ToString(), newSocket);

                        int ID = GetRandomId();
                        newSocket.Send(PacketConnectionAccepted.Serialize(ID));
                        newSocket.State = ConnectionState.Connected;

            #if DEBUG
                        Console.WriteLine($"Client connected: {address} ID:{ID}");
            #endif
                    }
                }
                else if (conn.ServerSocket != null)
                {
                    int ID = GetRandomId();
                    conn.Send(PacketConnectionAccepted.Serialize(ID));
                    conn.State = ConnectionState.Connected;

            #if DEBUG
                    Console.WriteLine($"Client reconnected: {address} ID:{ID}");
            #endif
                }

                ByteBufferPool.Release(buffer);
            }
            break;
            case PacketType.Pong:
            {
                if (Clients.TryGetValue(address, out conn) && conn.ServerSocket != null)
                    PacketPong.Deserialize(buffer, conn);

                ByteBufferPool.Release(buffer);
            }
            break;
            case PacketType.Disconnect:
            {
                if (Clients.TryRemove(address, out conn))
                {
                    conn.OnDisconnect();
#if DEBUG
                    Console.WriteLine($"Client disconnected: {address}");
#endif
                }

                ByteBufferPool.Release(buffer);
            }
            break;
            case PacketType.Reliable:
            case PacketType.Unreliable:
            case PacketType.Ack:
            {
                if (Clients.TryGetValue(address, out conn) && conn.ServerSocket != null)
                {
                }

                ByteBufferPool.Release(buffer);
            }
            break;
            case PacketType.CheckIntegrity:
            {
                ushort integrityKey = buffer.ReadUShort();

                if (Clients.TryGetValue(address, out conn) && conn.ServerSocket != null)
                {
                    short key = _integrityKeyTable.Keys[conn.IntegrityCheck];

                    if (integrityKey != key)
                    {
                        conn.Disconnect(DisconnectReason.InvalidIntegrity);
#if DEBUG
                        Console.WriteLine($"The Client did not respond to the key correctly {address}");
#endif
                    }
                    else
                    {
                        conn.TimeoutIntegrityCheck = 120f;
                    }
                }

                ByteBufferPool.Release(buffer);
            }
            break;
            default:
            {
#if DEBUG
                Console.WriteLine($"Unknown packet type: {type}");
#endif
                ByteBufferPool.Release(buffer);
            }
            break;
        }

        MergeSendQueue();
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
            Flush();
        }
    }

    public static void Send(ByteBuffer buffer)
    {
        if (LocalSendQueue == null)
            LocalSendQueue = new ByteBufferLinked();

        if(buffer.Length > 0)
        {
            LocalSendQueue.Add(buffer);
            Flush();
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

    public static void Flush() => SendEvent?.Set();

    public static void Update(float delta)
    {
        MergeSendQueue();

        foreach (var kv in Clients.Values)
            kv.Update(delta);
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

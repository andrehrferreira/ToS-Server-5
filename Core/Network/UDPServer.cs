using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

public class UDPServerOptions
{
    public uint Version { get; set; } = 1;
    public bool EnableWAF { get; set; } = true;
    public bool EnableIntegrityCheck { get; set; } = true;
    public int ReceiveTimeout { get; set; } = 10;
    public bool UseXOREncode { get; set; } = false;
    public byte[] XORKey { get; set; } = null;
    public bool UseEncryption { get; set; } = false;
    public byte[] EncryptionKey { get; set; } = null;
    public byte[] EncryptionIV { get; set; } = null;
    public uint MaxConnections { get; set; } = 25000;
    public int ReceiveBufferSize { get; set; } = 512 * 1024;
    public int SendBufferSize { get; set; } = 512 * 1024;
}

public sealed class UDPServer
{
    private static UDPServerOptions _options;

    private static PacketFlags _baseFlags = PacketFlags.None;

    private static IntegrityKeyTable _integrityKeyTable;

    private static Socket ServerSocket;

    private static int ServerFD;

    private static bool Running = true;

    private static ConcurrentDictionary<EndPoint, UDPSocket> Clients =
        new ConcurrentDictionary<EndPoint, UDPSocket>();

    private static ByteBufferLinked GlobalSendQueue = new ByteBufferLinked();

    [ThreadStatic]
    static ByteBufferLinked LocalSendQueue;

    private static long _packetsSent = 0;
    private static long _bytesSent = 0;
    private static long _packetsReceived = 0;
    private static long _bytesReceived = 0;

    private static Thread SendThread;

    private static Thread ReceiveThread;

    private static AutoResetEvent SendEvent;

    public static TimeSpan ReliableTimeout = TimeSpan.FromMilliseconds(250f);


    public delegate bool ConnectionHandler(UDPSocket socket, string token);

    private static ConnectionHandler _connectionHandler;

    private static World _worldRef;

    public static void SetWorld(World worldRef) => _worldRef = worldRef;
    public static World GetWorld() => _worldRef;

    public static int ConnectionCount => Clients.Count;
    public static long PacketsSent => Interlocked.Read(ref _packetsSent);
    public static long PacketsReceived => Interlocked.Read(ref _packetsReceived);
    public static long BytesSent => Interlocked.Read(ref _bytesSent);
    public static long BytesReceived => Interlocked.Read(ref _bytesReceived);

    public static uint GetRandomId() =>
        (uint)BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & 0x7FFFFFFF;

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

                ServerFD = (int)ServerSocket.Handle;

                ServerSocket.ReceiveTimeout = _options.ReceiveTimeout;

                ServerSocket.ReceiveBufferSize = _options.ReceiveBufferSize;

                ServerSocket.SendBufferSize = _options.SendBufferSize;

                Running = true;

                PacketRegistration.RegisterPackets();

#if DEBUG
                ServerMonitor.Log($"UDP server started on port {port}");
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
            ServerMonitor.Log($"Error initializing UDP server: {ex.Message}");
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
        ServerMonitor.Log("UDP server stopped.");
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
                    Poll(100);

                    if (pingTimer.Elapsed >= TimeSpan.FromSeconds(1))
                    {
                        if (PacketManager.TryGet(PacketType.Ping, out var packet))
                        {
                            foreach (var kv in Clients)
                            {
                                kv.Value.PingSentAt = DateTime.UtcNow;
                                kv.Value.Send(packet.Serialize(new Ping {
                                    SentTimestamp = Stopwatch.GetTimestamp()
                                }));
                            }
                        }

                        pingTimer.Restart();
                    }

                    if (_options.EnableIntegrityCheck)
                    {
                        if (checkIntegrityTimer.Elapsed >= TimeSpan.FromSeconds(30))
                        {
                            if (PacketManager.TryGet(PacketType.CheckIntegrity, out var packet))
                            {
                                foreach (var kv in Clients)
                                {
                                    kv.Value.IntegrityCheckSentAt = DateTime.UtcNow;
                                    kv.Value.IntegrityCheck = IntegrityKeyTableGenerator.GenerateIndex();
                                    kv.Value.Send(packet.Serialize(new CheckIntegrity {
                                        Index = kv.Value.IntegrityCheck,
                                        Version = _options.Version,
                                    }));
                                }
                            }

                            checkIntegrityTimer.Restart();
                        }
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
                                }
                            }
                        }

                        cleanupTimer.Restart();
                    }

                    MergeSendQueue();

                    Flush();

                    ByteBufferPool.Merge();

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                ServerMonitor.Log($"Erro: {ex.Message}");
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

                var batch = new List<(object addr, byte[] data, int length)>();

                while (buffer != null)
                {
                    var next = buffer.Next;

                    try
                    {
                        if (buffer.Connection != null)
                        {
                            var address = buffer.Connection.RemoteEndPoint;

                            if (buffer.Length > 0 && buffer.Data != null)
                            {
                                if (
                                    buffer.Data[0] == (byte)PacketType.Reliable ||
                                    buffer.Data[0] == (byte)PacketType.Unreliable ||
                                    buffer.Data[0] == (byte)PacketType.Ack
                                )
                                {
                                    uint crc32c = CRC32C.Compute(buffer.Data);

                                    if (buffer.Reliable)
                                        buffer.Connection.AddReliablePacket(crc32c, buffer);

                                    buffer.Write(crc32c);
                                }

                                if (!buffer.IsDestroyed && buffer.Length > 0)
                                {
                                    batch.Add((FormatAddress(address), buffer.Data, buffer.Length));
                                }
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        ByteBufferPool.Release(buffer);
                    }

                    buffer = next;
                }

                if (batch.Count > 0)
                {
                    UdpBatchIO.SendBatch(ServerSocket, batch);

                    foreach (var p in batch)
                    {
                        Interlocked.Increment(ref _packetsSent);
                        Interlocked.Add(ref _bytesSent, p.length);
                    }

                    batch.Clear();
                }

                ByteBufferPool.Merge();
            }
        });

        SendThread.IsBackground = true;

        SendThread.Start();
    }

    public static bool Poll(int timeout)
    {
        bool received = false;

        UdpBatchIO.ReceiveBatch(ServerSocket, 32, (addrObj, data, len) =>
        {
            var address = ConvertToEndPoint(addrObj);

            if (_options.EnableWAF && !WAFRateLimiter.AllowPacket(address))
            {
#if DEBUG
                ServerMonitor.Log($"[WAF] Packet dropped due to rate limit from {address}");
#endif
                return;
            }

            PacketType packetType = (PacketType)data[0];
            HandlePacket(packetType, data, len, address);

            Interlocked.Increment(ref _packetsReceived);
            Interlocked.Add(ref _bytesReceived, len);

            received = true;
        });

        return received;
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

    private static void HandlePacket(PacketType type, byte[] data, int len, EndPoint address)
    {
        UDPSocket conn;

        switch (type)
        {
            case PacketType.Connect:
            {
                string token = null;

                /*if (buffer.Length - buffer.Offset > 0)
                {
                    int tokenLen = buffer.ReadByte();

                    if (tokenLen > 0 && buffer.Length - buffer.Offset >= tokenLen)
                    {
                        byte[] tokenBytes = new byte[tokenLen];
                        buffer.Read(tokenBytes, 0, tokenLen);
                        token = System.Text.Encoding.UTF8.GetString(tokenBytes);
                    }
                }

                if(token == null || token.Length < 90 || Clients.Count >= _options.MaxConnections)
                {
                    if (PacketManager.TryGet(PacketType.ConnectionDenied, out var packet))
                    {
                        ByteBuffer bufferConnectionDenied = packet.Serialize();
                        ServerSocket.SendTo(buffer.Data, 0, buffer.Length, SocketFlags.None, address);
                        System.Threading.Interlocked.Increment(ref _packetsSent);
                        System.Threading.Interlocked.Add(ref _bytesSent, buffer.Length);
                    }

                    ByteBufferPool.Release(buffer);
                    return;
                }*/

                if (!Clients.TryGetValue(address, out conn))
                {
                    var newSocket = new UDPSocket(ServerSocket)
                    {
                        Id = GetRandomId(),
                        RemoteEndPoint = address,
                        TimeoutLeft = 30f,
                        State = ConnectionState.Connecting,
                        Flags = _baseFlags,
                        EnableIntegrityCheck = _options.EnableIntegrityCheck
                    };

                    bool valid = _connectionHandler?.Invoke(newSocket, token) ?? true;

                    if (Clients.TryAdd(address, newSocket))
                    {
                        QueueBuffer.AddSocket(newSocket.Id, newSocket);

                        uint ID = GetRandomId();

                        if(PacketManager.TryGet(PacketType.ConnectionAccepted, out var packet))
                        {
                            newSocket.Send(packet.Serialize(new ConnectionAccepted {
                                Id = ID
                            }));
                        }

                        newSocket.State = ConnectionState.Connected;

            #if DEBUG
                        //ServerMonitor.Log($"Client connected: {address} ID:{ID}");
            #endif
                    }
                }
            }
            break;
            case PacketType.Pong:
            {
                if (Clients.TryGetValue(address, out conn))
                {
                    long sentTimestamp = ByteBuffer.ReadLong(data, 1, len);
                    long nowTimestamp = Stopwatch.GetTimestamp();
                    long elapsedTicks = nowTimestamp - sentTimestamp;

                    double rttMs = (elapsedTicks * 1000.0) / Stopwatch.Frequency;

                    conn.Ping = (uint)rttMs;
                    conn.TimeoutLeft = 30f;
                }
            }
            break;
            case PacketType.Disconnect:
            {
                if (Clients.TryRemove(address, out conn))
                {
                    conn.OnDisconnect();
#if DEBUG
                    ServerMonitor.Log($"Client disconnected: {address}");
#endif
                }
            }
            break;
            case PacketType.Reliable:
            case PacketType.Unreliable:
            case PacketType.Ack:
            {
                if (Clients.TryGetValue(address, out conn))
                {
                    var crc32c = CRC32C.Compute(data, data.Length - 4);
                    uint receivedCrc32c = ByteBuffer.ReadSign(data, len);

                    if (receivedCrc32c == crc32c)
                    {
                        if (conn.State == ConnectionState.Connecting)
                            conn.State = ConnectionState.Connected;

                        var buffer = ByteBufferPool.Acquire();
                        buffer.Assign(data, len);
                        buffer.Length -= 4;

                        buffer.Connection = conn;

                        //conn.ProcessPacket(type, buffer);
                    }
                }
            }
            break;
            case PacketType.CheckIntegrity:
            {
                if (Clients.TryGetValue(address, out conn))
                {
                    ushort integrityKey = ByteBuffer.ReadUShort(data, 1, len);
                    short key = _integrityKeyTable.Keys[conn.IntegrityCheck];

                    if (integrityKey != key)
                    {
                        conn.Disconnect(DisconnectReason.InvalidIntegrity);
#if DEBUG
                        ServerMonitor.Log($"The Client did not respond to the key correctly {address}");
#endif
                    }
                    else
                    {
                        conn.TimeoutIntegrityCheck = 120f;
                    }
                }
            }
            break;
            case PacketType.BenckmarkTest:
            {
                if (Clients.TryGetValue(address, out conn))
                {
                    
                }
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
            Send(buffer);
        }
    }

    public static void Send(ByteBuffer buffer)
    {
        if (LocalSendQueue == null)
            LocalSendQueue = new ByteBufferLinked();

        if (buffer.Length > 0)
        {
            LocalSendQueue.Add(buffer);
            Flush();
        }
    }

    public static void Send(ServerPacket packetType, ByteBuffer buffer)
    {
        Send(ByteBuffer.Pack(buffer, packetType, _baseFlags)); 
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
        QueueBuffer.Tick();

        MergeSendQueue();

        foreach (var kv in Clients.Values)
            kv.Update(delta);
    }

    public static void RunMainLoop()
    {
        const int targetFps = 20;
        const double targetFrameTime = 1000.0 / targetFps;
        Stopwatch sw = new Stopwatch();
        sw.Start();
        double lastTime = sw.Elapsed.TotalMilliseconds;
        Stopwatch memLog = Stopwatch.StartNew();
        Stopwatch trimTimer = Stopwatch.StartNew();

        while (Running)
        {
            double now = sw.Elapsed.TotalMilliseconds;
            double delta = now - lastTime;
            lastTime = now;

            Update((float)(delta / 1000.0));

            if (memLog.Elapsed >= TimeSpan.FromSeconds(10))
            {
                MemoryUsageLogger.Log();
                memLog.Restart();
            }

            /*if (trimTimer.Elapsed >= TimeSpan.FromSeconds(60))
            {
                ByteBufferPool.TrimExcess(10);
                trimTimer.Restart();
            }*/
          
            double elapsed = sw.Elapsed.TotalMilliseconds - now;
            int sleep = (int)(targetFrameTime - elapsed);

            if (sleep > 0)
                Thread.Sleep(sleep);
        }
    }
}

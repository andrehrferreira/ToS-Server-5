/*
* UDPServer
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

public unsafe struct NativeBuffer
{
    public byte* Data;
    public int Length;

    public NativeBuffer(byte* data, int length)
    {
        Data = data;
        Length = length;
    }
}

public struct SendPacket
{
    public byte[] Buffer;
    public int Length;
    public EndPoint Address;
    public bool Pooled;
}

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

    private static bool Running = true;

    private static ConcurrentDictionary<EndPoint, UDPSocket> Clients =
        new ConcurrentDictionary<EndPoint, UDPSocket>();

    private static QueueStructLinked<SendPacket> GlobalSendQueue = new QueueStructLinked<SendPacket>();

    [ThreadStatic]
    static QueueStructLinked<SendPacket> LocalSendQueue;

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

                ServerSocket.ReceiveTimeout = _options.ReceiveTimeout;

                ServerSocket.ReceiveBufferSize = _options.ReceiveBufferSize;

                ServerSocket.SendBufferSize = _options.SendBufferSize;

                Running = true;

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
    }

    public unsafe static void ReceiveOnBackgroundThread()
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
                        if(Clients.Count > 0)
                        {
                            var buffer = new FlatBuffer(9);
                            var pingPacket = new PingPacket { SentTimestamp = Stopwatch.GetTimestamp() };
                            pingPacket.Serialize(ref buffer);

                            var sendBatch = new PacketPointer[Clients.Count];
                            int count = 0;

                            foreach (var kv in Clients)
                            {
                                sendBatch[count++] = new PacketPointer
                                {
                                    Addr = kv.Value.RemoteEndPoint,
                                    DataPtr = (IntPtr)buffer.Data,
                                    Length = buffer.Position
                                };

                                kv.Value.PingSentAt = DateTime.UtcNow;

                                Interlocked.Increment(ref _packetsSent);
                            }

                            UdpBatchIO.SendBatch(ServerSocket, sendBatch.AsSpan(0, count));

                            buffer.Free();
                        }

                        pingTimer.Restart();
                    }

                    /*if (_options.EnableIntegrityCheck)
                    {
                        if (checkIntegrityTimer.Elapsed >= TimeSpan.FromSeconds(30))
                        {
                            if (Clients.Count > 0)
                            {
                                if (PacketManager.TryGet(PacketType.CheckIntegrity, out var packet))
                                {
                                    foreach (var kv in Clients)
                                    {
                                        kv.Value.IntegrityCheckSentAt = DateTime.UtcNow;
                                        kv.Value.IntegrityCheck = IntegrityKeyTableGenerator.GenerateIndex();
                                        packet.Serialize(new CheckIntegrity { Index = kv.Value.IntegrityCheck, Version = _options.Version });
                                        kv.Value.Send(packet.Buffer);
                                    }
                                }
                            }

                            checkIntegrityTimer.Restart();
                        }
                    }*/

                    if (cleanupTimer.Elapsed >= TimeSpan.FromSeconds(5))
                    {
                        if(Clients.Count > 0)
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

                QueueStructLinked<SendPacket>.Node node;

                lock (GlobalSendQueue)
                {
                    node = GlobalSendQueue.Clear();
                }

                var batch = new List<SendPacket>();

                while (node != null)
                {
                    var next = node.Next;
                    var packet = node.Value;

                    if (packet.Buffer != null && packet.Length > 0)                    
                        batch.Add(packet);
                    
                    node = next;
                }

                if (batch.Count > 0)
                {
                    var sendBatch = new (EndPoint, byte[], int)[batch.Count];
                    int count = 0;

                    foreach (var p in batch)
                        sendBatch[count++] = (p.Address, p.Buffer, p.Length);

                    UdpBatchIO.SendBatch(ServerSocket, sendBatch);

                    foreach (var p in batch)
                    {
                        Interlocked.Increment(ref _packetsSent);
                        Interlocked.Add(ref _bytesSent, p.Length);

                        if (p.Pooled)
                            ArrayPool<byte>.Shared.Return(p.Buffer);
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

        UdpBatchIO.ReceiveBatch(ServerSocket, 32, (address, data, len) =>
        {
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
                        uint ID = GetRandomId();

                        newSocket.Send(new ConnectionAcceptedPacket { Id = ID });

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
                    try
                    {
                        var newLocation = ByteBuffer.ReadFVector(data, 1, len);
                        var newRotato = ByteBuffer.ReadFRotator(data, 13, len);

                        var values = Clients.Values.ToArray();
                        if (values.Length == 0)
                            break;

                        var rnd = new Random();
                        int limit = Math.Min(250, values.Length);

                        for (int i = 0; i < limit; i++)
                        {
                            var randomValue = values[rnd.Next(values.Length)];

                            randomValue.Send(new BenchmarkPacket
                            {
                                Id = conn.Id,
                                Positon = newLocation,
                                Rotator = newRotato
                            });
                        }
                    }
                    catch { }                    
                }
            }
            break;
        }

        MergeSendQueue();
    }

    private static byte[] AddSignature(byte[] data)
    {
        if (data == null || data.Length == 0)
            return data;

        PacketType t = (PacketType)data[0];
        if (t == PacketType.Reliable || t == PacketType.Unreliable || t == PacketType.Ack)
        {
            uint crc32c = CRC32C.Compute(data, data.Length);
            byte[] result = ArrayPool<byte>.Shared.Rent(data.Length + 4);
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            ByteBuffer.WriteUInt(result, (uint)data.Length, crc32c);
            return result;
        }

        return data;
    }

    public static void Send(byte[] data, UDPSocket socket, bool pooled = false)
    {
        if (LocalSendQueue == null)
            LocalSendQueue = new QueueStructLinked<SendPacket>();

        if (data.Length > 0)
        {
            var signed = AddSignature(data);
            bool poolSigned = pooled || !ReferenceEquals(signed, data);

            if (!ReferenceEquals(signed, data) && pooled)
                ArrayPool<byte>.Shared.Return(data);

            var packet = new SendPacket
            {
                Buffer = signed,
                Length = signed.Length,
                Address = socket.RemoteEndPoint,
                Pooled = poolSigned
            };

            LocalSendQueue.Add(packet);
            Flush();
        }
    }

    public static void Send(ByteBuffer buffer)
    {
        if (LocalSendQueue == null)
            LocalSendQueue = new QueueStructLinked<SendPacket>();

        if (buffer.Length > 0 && buffer.Connection != null)
        {
            byte[] data = ArrayPool<byte>.Shared.Rent(buffer.Length);
            Buffer.BlockCopy(buffer.Data, 0, data, 0, buffer.Length);

            var signed = AddSignature(data);
            bool pooled = true;

            if (!ReferenceEquals(signed, data))
                ArrayPool<byte>.Shared.Return(data);

            var packet = new SendPacket
            {
                Buffer = signed,
                Length = signed.Length,
                Address = buffer.Connection.RemoteEndPoint,
                Pooled = pooled
            };

            LocalSendQueue.Add(packet);
            Flush();
        }

        ByteBufferPool.Release(buffer);
    }

    public static void Send(ServerPacket packetType, ByteBuffer buffer)
    {
        var packed = ByteBuffer.Pack(buffer, packetType, _baseFlags);
        ByteBufferPool.Release(buffer);
        Send(packed);
    }

    public static unsafe void Send(FlatBuffer buffer, int length, UDPSocket socket)
    {
        if (LocalSendQueue == null)
            LocalSendQueue = new QueueStructLinked<SendPacket>();

        if (length > 0 && socket != null)
        {
            byte[] managedBuffer = new byte[length];

            fixed (byte* dst = managedBuffer)
            {
                Buffer.MemoryCopy(buffer.Data, dst, length, length);
            }

            var packet = new SendPacket
            {
                Buffer = managedBuffer,
                Length = length,
                Address = socket.RemoteEndPoint,
                Pooled = false
            };

            buffer.Free();

            LocalSendQueue.Add(packet);
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

        ByteBufferPool.Merge();
    }
}

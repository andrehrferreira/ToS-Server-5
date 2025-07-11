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

using NanoSockets;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;

public unsafe struct SendPacket
{
    public byte* Buffer;
    public int Length;
    public Address Address;
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
    public int SendThreadCount { get; set; } = 1;
    public int MTU = 3600;
}

public sealed class UDPServer
{
    private static UDPServerOptions _options;

    private static PacketFlags _baseFlags = PacketFlags.None;

    private static IntegrityKeyTable _integrityKeyTable;

    private static NanoSockets.Socket ServerSocket;

    private static bool Running = true;

    public static ConcurrentDictionary<Address, UDPSocket> Clients =
        new ConcurrentDictionary<Address, UDPSocket>();

    private static Channel<SendPacket> GlobalSendChannel;

    [ThreadStatic]
    static Channel<SendPacket> LocalSendChannel;

    public static long _packetsSent = 0;
    public static long _bytesSent = 0;
    public static long _packetsReceived = 0;
    public static long _bytesReceived = 0;
    public static int _tickRate = 0;

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
    public static int TickRate => _tickRate;

    public static uint GetRandomId() =>
        (uint)BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & 0x7FFFFFFF;

    public static int Mtu
    {
        get
        {
            return _options?.MTU ?? 1472;
        }
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
            if (IntegritySystem.GetCurrentTable(out var table))
            {
                _integrityKeyTable = table;

                // Initialize NanoSockets
                if (UDP.Initialize() != Status.OK)
                {
#if DEBUG
                    ServerMonitor.Log("Failed to initialize NanoSockets");
#endif
                    return false;
                }

                // Create NanoSockets socket
                ServerSocket = UDP.Create(_options.SendBufferSize, _options.ReceiveBufferSize);

                if (!ServerSocket.IsCreated)
                {
#if DEBUG
                    ServerMonitor.Log("Failed to create NanoSockets socket");
#endif
                    return false;
                }

                // Create address and bind
                var bindAddress = Address.CreateFromIpPort("0.0.0.0", (ushort)port);

                if (UDP.Bind(ServerSocket, ref bindAddress) != 0)
                {
#if DEBUG
                    ServerMonitor.Log($"Failed to bind to port {port}");
#endif
                    return false;
                }

                // Set socket to non-blocking
                UDP.SetNonBlocking(ServerSocket, false);

                Running = true;

#if DEBUG
                ServerMonitor.Log($"NanoSockets UDP server started on port {port}");
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

        if (ServerSocket.IsCreated)
        {
            UDP.Destroy(ref ServerSocket);
        }

        UDP.Deinitialize();
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
                while (Running)
                {
                    Poll(100);

                    int packetCount = 0;

                    while (Poll(0) && packetCount < 1000)
                    {
                        ++packetCount;
                    }

                    if (pingTimer.Elapsed >= TimeSpan.FromSeconds(5))
                    {
                        if(Clients.Count > 0)
                        {
                            var buffer = new FlatBuffer(9);
                            var pingPacket = new PingPacket { SentTimestamp = Stopwatch.GetTimestamp() };
                            pingPacket.Serialize(ref buffer);

                            foreach (var kv in Clients)
                            {
                                kv.Value.PingSentAt = DateTime.UtcNow;

                                kv.Value.Send(ref buffer, false);

                                Interlocked.Increment(ref _packetsSent);

                                Interlocked.Add(ref UDPServer._bytesSent, pingPacket.Size);
                            }

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

                    Flush();

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

    public static unsafe void SendOnBackgroundThread()
    {
        GlobalSendChannel = Channel.CreateUnbounded<SendPacket>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        SendEvent = new AutoResetEvent(false);

        SendThread = new Thread(() =>
        {
            while (Running)
            {
                SendEvent.WaitOne(1);

                while (GlobalSendChannel.Reader.TryRead(out var packet))
                {
                    if (packet.Buffer != null && packet.Length > 0)
                    {
                        var address = packet.Address;
                        int sent = UDP.Unsafe.Send(ServerSocket, &address, packet.Buffer, packet.Length);

                        if (sent > 0)
                        {
                            Interlocked.Increment(ref _packetsSent);
                            Interlocked.Add(ref _bytesSent, sent);
                        }
                    }
                }

                Thread.Sleep(1);
            }
        });

        SendThread.IsBackground = true;

        SendThread.Start();
    }

    public static unsafe bool Poll(int timeout)
    {
        try
        {
            int pollResult = UDP.Poll(ServerSocket, timeout);

            if (pollResult <= 0)
                return false;

            bool received = false;
            Address address = default;
            FlatBuffer buffer = new FlatBuffer(Mtu);

            int len = UDP.Unsafe.Receive(ServerSocket, &address, buffer.Data, buffer.Capacity);

            if (len > 0)
            {
                PacketType packetType = (PacketType)buffer.Data[0];
                HandlePacket(packetType, buffer, len, address);
                Interlocked.Increment(ref _packetsReceived);
                Interlocked.Add(ref _bytesReceived, len);
                received = true;
            }

            return received;
        }
        catch (Exception ex)
        {
            ServerMonitor.Log($"Socket exception occurred while polling for packets: {ex.Message}");
            return false;
        }
    }

    private static unsafe Task HandlePacket(PacketType type, FlatBuffer data, int len, Address address)
    {
        UDPSocket conn;

        switch (type)
        {
            case PacketType.Connect:
            {
                string token = null;

                if (!Clients.TryGetValue(address, out conn))
                {
                    var newSocket = new UDPSocket(ServerSocket)
                    {
                        Id = GetRandomId(),
                        RemoteAddress = address,
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
                    long sentTimestamp = data.Read<long>();
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
                /*if (Clients.TryGetValue(address, out conn))
                {
                    fixed (byte* dataPtr = data)
                    {
                        var crc32c = CRC32C.Compute(dataPtr, data.Length - 4);
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
                }*/
            }
            break;
            case PacketType.CheckIntegrity:
            {
                if (Clients.TryGetValue(address, out conn))
                {
                    ushort integrityKey = data.Read<ushort>();
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
                    data.Reset();
                    conn.EventQueue.Writer.TryWrite(data);
                }
            }
            break;
        }

        return Task.CompletedTask;
    }

    private static unsafe byte* AddSignature(byte* data, int length, out int newLength)
    {
        if (data == null || length == 0)
        {
            newLength = length;
            return data;
        }

        PacketType t = (PacketType)data[0];

        if (t == PacketType.Reliable || t == PacketType.Unreliable || t == PacketType.Ack)
        {
            uint crc32c = CRC32C.Compute(data, length);
            byte* result = (byte*)Marshal.AllocHGlobal(length + 4);

            Buffer.MemoryCopy(data, result, length + 4, length);

            *(uint*)(result + length) = crc32c;

            newLength = length + 4;
            return result;
        }

        newLength = length;
        return data;
    }

    public static unsafe void Send(ref FlatBuffer buffer, int length, UDPSocket socket, bool flush = true)
    {
        if (length > 0 && socket != null)
        {
            var packet = new SendPacket
            {
                Buffer = buffer.Data,
                Length = length,
                Address = socket.RemoteAddress,
                Pooled = true
            };

            GlobalSendChannel.Writer.TryWrite(packet);

            if(flush)
                buffer.Reset();

            Flush();
        }
    }

    public static void Flush()
    {
        if (SendEvent != null)
        {
            SendEvent.Set();
        }
    }

    public static async Task Update(float delta)
    {
        foreach (var kv in Clients.Values)
            kv.Update(delta);
    }

    public static void RunMainLoop()
    {
        const int targetFps = 20;
        int fps = 0;
        const double targetFrameTime = 1000.0 / targetFps;
        Stopwatch sw = new Stopwatch();
        sw.Start();
        double lastTime = sw.Elapsed.TotalMilliseconds;
        Stopwatch memLog = Stopwatch.StartNew();
        Stopwatch trimTimer = Stopwatch.StartNew();
        Stopwatch latence = Stopwatch.StartNew();

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

            if (latence.Elapsed >= TimeSpan.FromSeconds(1))
            {
                _tickRate = fps;
                fps = 0;
                latence.Restart();
            }

            double elapsed = sw.Elapsed.TotalMilliseconds - now;
            int sleep = (int)(targetFrameTime - elapsed);
            fps++;

            if (sleep > 0)
                Thread.Sleep(sleep);
        }
    }
}

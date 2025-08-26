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
using Org.BouncyCastle.Bcpg;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Channels;
using Core.Config;

public unsafe struct SendPacket
{
    public byte* Buffer;
    public int Length;
    public Address Address;
    public bool Pooled;
}

public readonly struct ReceivedPacket
{
    public readonly FlatBuffer Buffer;
    public readonly Address Address;

    public ReceivedPacket(FlatBuffer buffer, Address address)
    {
        Buffer = buffer;
        Address = address;
    }
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
    public int MTU = 1200;
}

public sealed class UDPServer
{
    private static UDPServerOptions _options;

    private static PacketFlags _baseFlags = PacketFlags.None;

    private static IntegrityKeyTable _integrityKeyTable;

    private static Socket ServerSocket;

    private static bool Running = true;

    public static ConcurrentDictionary<Address, UDPSocket> Clients =
        new ConcurrentDictionary<Address, UDPSocket>();

    private static Channel<SendPacket> GlobalSendChannel;

    [ThreadStatic]
    static Channel<SendPacket> LocalSendChannel;

    public static Channel<ReceivedPacket> GlobalReceiveChannel = Channel.CreateUnbounded<ReceivedPacket>(
    new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });

    public static long _packetsSent = 0;
    public static long _bytesSent = 0;
    public static long _packetsReceived = 0;
    public static long _bytesReceived = 0;
    public static int _tickRate = 0;
    public static int _sendQueueCount = 0;

    private static Thread SendThread;

    private static Thread ReceiveThread;

    private static AutoResetEvent SendEvent;

    public static TimeSpan ReliableTimeout { get; private set; } = TimeSpan.FromMilliseconds(250f);

    public delegate bool ConnectionHandler(UDPSocket socket, string token);

    private static ConnectionHandler _connectionHandler;

    public static int ConnectionCount => Clients.Count;

    public static long PacketsSent => Interlocked.Read(ref _packetsSent);
    public static long PacketsReceived => Interlocked.Read(ref _packetsReceived);
    public static long BytesSent => Interlocked.Read(ref _bytesSent);
    public static long BytesReceived => Interlocked.Read(ref _bytesReceived);
    public static int TickRate => _tickRate;

    public static uint GetRandomId() =>
        (uint)BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & 0x7FFFFFFF;

    /// <summary>
    /// Create UDPServerOptions from ServerConfig
    /// </summary>
    private static UDPServerOptions CreateOptionsFromConfig(ServerConfig config)
    {
        return new UDPServerOptions
        {
            Version = 1,
            EnableWAF = config.Security.EnableWAF,
            EnableIntegrityCheck = config.Security.EnableIntegrityCheck,
            ReceiveTimeout = 10,
            UseXOREncode = false,
            XORKey = null,
            UseEncryption = config.Security.EnableEndToEndEncryption,
            EncryptionKey = null,
            EncryptionIV = null,
            MaxConnections = (uint)config.Server.MaxPlayers,
            ReceiveBufferSize = config.Network.ReceiveBufferSize,
            SendBufferSize = config.Network.SendBufferSize,
            SendThreadCount = config.Network.SendThreadCount,
            MTU = config.Network.MaxPacketSize
        };
    }

    public static int Mtu
    {
        get
        {
            return _options?.MTU ?? 1200;
        }
    }

    public static bool Init(
        int port,
        ConnectionHandler connectionHandler,
        UDPServerOptions options = null
    )
    {
        // Load server configuration first
        var config = ServerConfig.LoadConfig();

        // Create options from configuration if not provided
        if (options == null)
        {
            options = CreateOptionsFromConfig(config);
        }

                _options = options;
        _connectionHandler = connectionHandler;

        // Apply configuration values
        port = config.Network.Port;
        ReliableTimeout = TimeSpan.FromMilliseconds(config.Performance.ReliableTimeoutMs);

        if (_options.UseXOREncode)
            _baseFlags = PacketFlagsUtils.AddFlag(_baseFlags, PacketFlags.XOR);

        if (_options.UseEncryption)
            _baseFlags = PacketFlagsUtils.AddFlag(_baseFlags, PacketFlags.Encrypted);

        try
        {
            if (IntegritySystem.GetCurrentTable(out var table))
            {
                _integrityKeyTable = table;

                if (UDP.Initialize() != Status.OK)
                {
#if DEBUG
                    ServerMonitor.Log("Failed to initialize NanoSockets");
#endif
                    return false;
                }

                ServerSocket = UDP.Create(_options.SendBufferSize, _options.ReceiveBufferSize);

                if (!ServerSocket.IsCreated)
                {
#if DEBUG
                    ServerMonitor.Log("Failed to create NanoSockets socket");
#endif
                    return false;
                }

                var bindAddress = Address.CreateFromIpPort("0.0.0.0", (ushort)port);

                if (UDP.Bind(ServerSocket, ref bindAddress) != 0)
                {
#if DEBUG
                    ServerMonitor.Log($"Failed to bind to port {port}");
#endif
                    return false;
                }

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
            UDP.Destroy(ref ServerSocket);

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

                    var heartbeatInterval = ServerConfig.IsLoaded ?
                        TimeSpan.FromMilliseconds(ServerConfig.Instance.Security.HeartbeatIntervalMs) :
                        TimeSpan.FromSeconds(5);

                    if (pingTimer.Elapsed >= heartbeatInterval && ServerConfig.Instance.Security.EnableHeartbeat)
                    {
                        if(Clients.Count > 0)
                        {
                            var sentTimestamp = Stopwatch.GetTimestamp();
                            ushort timestampMs = (ushort)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000) % 65536);
                            var buffer = new FlatBuffer(9);
                            var pingPacket = new PingPacket { SentTimestamp = timestampMs };
                            pingPacket.Serialize(ref buffer);

                            foreach (var kv in Clients)
                            {
                                // Only send ping after crypto handshake is complete
                                if (kv.Value.CryptoHandshakeComplete)
                                {
                                    kv.Value.PingSentAt = DateTime.UtcNow;

                                    kv.Value.Send(ref buffer, false);

                                    Interlocked.Increment(ref _packetsSent);

                                    Interlocked.Add(ref UDPServer._bytesSent, pingPacket.Size);
                                }
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
                                // More lenient timeout for connections still in handshake
                                float timeoutThreshold = kv.Value.CryptoHandshakeComplete ? 0 : -30f;

                                if (kv.Value.TimeoutLeft <= timeoutThreshold)
                                {
                                    if (Clients.TryRemove(kv.Key, out var client))
                                    {
#if DEBUG
                                        ServerMonitor.Log($"Client timeout disconnected: {kv.Key}, handshake complete: {client.CryptoHandshakeComplete}");
#endif
                                        client.OnDisconnect();
                                    }
                                }

                                kv.Value.CleanupFragments(TimeSpan.FromSeconds(10));
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

    public static void SendOnBackgroundThread(int numThreads = 4)
    {
        GlobalSendChannel = Channel.CreateUnbounded<SendPacket>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        for (int i = 0; i < numThreads; i++)
        {
            Task.Run(async () =>
            {
                while (Running)
                {
                    try
                    {
                        if (GlobalSendChannel.Reader.TryRead(out var packet))
                        {
                            var address = packet.Address;
                            unsafe
                            {
                                int sent = UDP.Unsafe.Send(ServerSocket, &address, packet.Buffer, packet.Length);

                                if (sent > 0)
                                {
                                    Interlocked.Decrement(ref _sendQueueCount);
                                    Interlocked.Increment(ref _packetsSent);
                                    Interlocked.Add(ref _bytesSent, sent);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SendWorker-{i}] Error: {ex.Message}");
                    }
                }
            });
        }
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
                ProcessPacket(buffer, len, address);
                Interlocked.Increment(ref _packetsReceived);
                Interlocked.Add(ref _bytesReceived, len);
                received = true;
            }
            else
            {
                buffer.Free();
            }

            return received;
        }
        catch (Exception ex)
        {
            ServerMonitor.Log($"Socket exception occurred while polling for packets: {ex.Message}");
            return false;
        }
    }

    private static unsafe void HandlePacket(PacketType type, FlatBuffer data, int len, Address address)
    {
        UDPSocket conn;

        try
        {
            switch (type)
            {
                case PacketType.Connect:
                    {
                        if (!Clients.TryGetValue(address, out conn))
                        {
                            if (data.Position + 32 == len)
                            {
                                var cookie = CookieManager.GenerateCookie(address);
                                var helloBuffer = new FlatBuffer(49);
                                helloBuffer.Write(PacketType.Cookie);
                                helloBuffer.WriteBytes(cookie);

                                var helloLen = helloBuffer.Position;
                                UDP.Unsafe.Send(ServerSocket, &address, helloBuffer.Data, helloLen);
                                helloBuffer.Free();
                            }
                            else if (data.Position + 32 + 48 == len)
                            {
                                byte[] clientPub = new byte[32];
                                for (int i = 0; i < 32; i++)
                                    clientPub[i] = data.Read<byte>();

                                byte[] cookie = new byte[48];
                                for (int i = 0; i < 48; i++)
                                    cookie[i] = data.Read<byte>();

                                if (!CookieManager.ValidateCookie(cookie, address))
                                {
                                    data.Free();
                                    break;
                                }

                                uint connectionId = GetRandomId();
                                var (serverPub, salt, session) = SecureSession.CreateAsServer(clientPub, connectionId);

                                var newSocket = new UDPSocket(ServerSocket)
                                {
                                    Id = connectionId,
                                    RemoteAddress = address,
                                    TimeoutLeft = 30f,
                                    State = ConnectionState.Connecting,
                                    Flags = _baseFlags,
                                    EnableIntegrityCheck = _options.EnableIntegrityCheck,
                                    Session = session
                                };

                                bool valid = _connectionHandler?.Invoke(newSocket, null) ?? true;
                                //valid &&

                                if (Clients.TryAdd(address, newSocket))
                                {
                                    newSocket.Send(new ConnectionAcceptedPacket
                                    {
                                        Id = connectionId,
                                        ServerPublicKey = serverPub,
                                        Salt = salt
                                    });

                                    newSocket.State = ConnectionState.Connected;
                                }
                            }
                        }

                        data.Free();
                    }
                    break;
                case PacketType.Pong:
                    {
                        if (Clients.TryGetValue(address, out conn))
                        {
                            ushort sentTimestampMs = data.Read<ushort>();
                            ushort nowMs = (ushort)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000) % 65536);
                            ushort rttMs = (ushort)((nowMs - sentTimestampMs) & 0xFFFF);
                            conn.Ping = rttMs;
                            conn.TimeoutLeft = 30f;
                        }

                        data.Free();
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

                        data.Free();
                    }
                    break;
                case PacketType.Reliable:
                case PacketType.Unreliable:
                    {
                        if (Clients.TryGetValue(address, out conn))
                        {
                            uint signature = data.ReadSign(len);
                            uint crc32c = CRC32C.Compute(data.Data, len - 4);

                            if (signature == crc32c)
                            {
                                data.Resize(len - 4);
                                conn.TimeoutLeft = 30f;

                                                                                                // Original structure: EPacketType + ClientPackets + Data
                                // Handler expects to read from position 0
                                data.RestorePosition(0);

                                // Original structure: EPacketType(1) + ClientPackets(2) + Data(21) = 24 bytes total
                                // Peek at ClientPackets for logging (without consuming)
                                data.Read<byte>(); // Skip PacketType
                                ClientPackets clientPacket = (ClientPackets)data.Read<short>();
                                data.RestorePosition(0); // Reset for handler to position 0

                                FileLogger.Log($"[SERVER] 🎯 Processing {type} packet - ClientPacket: {clientPacket}");
                                FileLogger.Log($"[SERVER] 📦 Buffer position before handler: {data.Position}, capacity: {data.Capacity}");

                                if(PlayerController.TryGet(conn.Id, out var controller))
                                {
                                    PacketHandler.HandlePacket(controller, ref data, clientPacket);
                                    FileLogger.Log($"[SERVER] ✅ Routed {clientPacket} to PacketHandler");
                                }
                                else
                                {
                                    FileLogger.Log($"[SERVER] ❌ No PlayerController found for connection {conn.Id}");
                                }
                            }
                        }

                        data.Free();
                    }
                    break;
                case PacketType.CryptoTest:
                    {
                        if (Clients.TryGetValue(address, out conn))
                        {
                            uint value = data.Read<uint>();
#if DEBUG
                            ServerMonitor.Log($"Received CryptoTest {value} from {address}");
#endif

                            FileLogger.Log($"=== CRYPTO HANDSHAKE RECEIVED ===");
                            FileLogger.Log($"[SERVER] 🔐 Received CryptoTest: {value}");
                            FileLogger.Log($"[SERVER] 🔐 From: {address}");
                            FileLogger.Log($"[SERVER] 🔐 Session exists: {(conn.Session.ConnectionId != 0 ? "YES" : "NO")}");

                            conn.ClientTestValue = value;
                            var ackBuf = new FlatBuffer(5);
                            ackBuf.Write(PacketType.CryptoTestAck);
                            ackBuf.Write(value);

                            FileLogger.Log($"[SERVER] 🔐 Sending CryptoTestAck: {value}");
                            FileLogger.Log($"[SERVER] 🔐 Buffer size: {ackBuf.Capacity} bytes");

                            conn.Send(ref ackBuf, false);
                            ackBuf.Free();
#if DEBUG
                            ServerMonitor.Log($"Sent CryptoTestAck {value} to {address}");
#endif
                            conn.ClientCryptoConfirmed = true;

                            if (!conn.ServerCryptoConfirmed && !conn.CryptoHandshakeComplete)
                            {
                                conn.ServerTestValue = 0x1A2B3C4D;
                                var testBuf = new FlatBuffer(5);
                                testBuf.Write(PacketType.CryptoTest);
                                testBuf.Write(conn.ServerTestValue);
                                conn.Send(ref testBuf, false);
                                testBuf.Free();
#if DEBUG
                                ServerMonitor.Log($"Sent CryptoTest {conn.ServerTestValue} to {address}");
#endif
                            }

                            if (conn.CryptoHandshakeComplete)
                            {
#if DEBUG
                                ServerMonitor.Log($"Crypto handshake complete for {address}");
#endif
                            }
                        }

                        data.Free();
                    }
                    break;
                case PacketType.CryptoTestAck:
                    {
                        if (Clients.TryGetValue(address, out conn))
                        {
                            uint value = data.Read<uint>();
#if DEBUG
                            ServerMonitor.Log($"Received CryptoTestAck {value} from {address}");
#endif

                            FileLogger.Log($"=== CRYPTO ACK RECEIVED ===");
                            FileLogger.Log($"[SERVER] 🔐 Received CryptoTestAck: {value}");
                            FileLogger.Log($"[SERVER] 🔐 Expected: {conn.ServerTestValue}");
                            FileLogger.Log($"[SERVER] 🔐 Match: {(value == conn.ServerTestValue ? "YES" : "NO")}");

                            if (value == conn.ServerTestValue)
                                conn.ServerCryptoConfirmed = true;

                            if (conn.CryptoHandshakeComplete)
                            {
#if DEBUG
                                ServerMonitor.Log($"Crypto handshake complete for {address}");
#endif
                            }
                        }

                        data.Free();
                    }
                    break;
                                case PacketType.Ack:
                    {
                        if (Clients.TryGetValue(address, out conn))
                        {
                            // Read ulong as two uint parts (as sent by client)
                            uint low = data.Read<uint>();
                            uint high = data.Read<uint>();
                            ulong seq = ((ulong)high << 32) | low;
                            conn.AcknowledgeReliablePacket(seq);
                            ServerMonitor.Log($"[ACK] Server received ACK for sequence {seq}");
                        }

                        data.Free();
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
                            }
                            else
                            {
                                conn.TimeoutIntegrityCheck = 120f;
                            }
                        }

                        data.Free();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            ServerMonitor.Log($"Error handling packet: {ex.Message}");
        }
    }

    private static unsafe void HandleFragment(FlatBuffer data, int len, Address address)
    {
        if (!Clients.TryGetValue(address, out var conn))
        {
            data.Free();
            return;
        }

        ushort fragmentId = data.Read<ushort>();
        ushort offset = data.Read<ushort>();
        uint totalSize = data.Read<uint>();
        int payloadLen = len - data.Position;

        var fragment = conn.Fragments.GetOrAdd(fragmentId, _ => new UDPSocket.FragmentInfo
        {
            Buffer = new FlatBuffer((int)totalSize),
            TotalSize = totalSize,
            Received = 0,
            LastUpdate = DateTime.UtcNow
        });

        unsafe
        {
            Buffer.MemoryCopy(data.Data + data.Position, fragment.Buffer.Data + offset, fragment.Buffer.Capacity - offset, payloadLen);
        }

        fragment.Received += payloadLen;
        fragment.LastUpdate = DateTime.UtcNow;

        if (fragment.Received >= fragment.TotalSize)
        {
            fragment.Buffer.RestorePosition(0);
            PacketType original = (PacketType)fragment.Buffer.Read<byte>();
            HandlePacket(original, fragment.Buffer, (int)fragment.TotalSize, address);
            fragment.Buffer.Free();
            conn.Fragments.TryRemove(fragmentId, out _);
        }

        data.Free();
    }

    internal static void ProcessPacket(FlatBuffer buffer, int len, Address address)
    {
        // Check if packet is encrypted (has encryption header)
        if (len > 12 && Clients.TryGetValue(address, out var conn) && conn.Session.ConnectionId != 0)
        {
            FileLogger.Log($"[SERVER] 🔄 Processing encrypted packet ({len} bytes) using LEGACY header-based method");
            // Try to process as encrypted packet first
            if (ProcessEncryptedPacket_Legacy(buffer, conn, len))
                return;
        }

        // Fallback to unencrypted processing (for handshake packets)
        PacketType packetType = (PacketType)buffer.Read<byte>();

        if (packetType == PacketType.Fragment)
            HandleFragment(buffer, len, address);
        else
            HandlePacket(packetType, buffer, len, address);
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

    public static unsafe bool Send(ref FlatBuffer buffer, int length, UDPSocket socket, bool flush = true)
    {
        if (length > 0 && socket != null && GlobalSendChannel != null)
        {
            if (length > Mtu)
            {
                const int headerSize = 1 + 2 + 2 + 4;
                int maxPayload = Mtu - headerSize;
                ushort fragmentId = socket.GetNextFragmentId();
                uint totalSize = (uint)length;
                int offset = 0;

                while (offset < length)
                {
                    int chunk = Math.Min(maxPayload, length - offset);
                    var frag = new FlatBuffer(chunk + headerSize);
                    frag.Write(PacketType.Fragment);
                    frag.Write(fragmentId);
                    frag.Write((ushort)offset);
                    frag.Write(totalSize);

                    byte[] chunkData = new byte[chunk];
                    Marshal.Copy((IntPtr)(buffer.Data + offset), chunkData, 0, chunk);
                    frag.WriteBytes(chunkData);

                    int fragLen = frag.Position;
                    var fragData = AddSignature(frag.Data, fragLen, out fragLen);

                    var packet = new SendPacket
                    {
                        Buffer = fragData,
                        Length = fragLen,
                        Address = socket.RemoteAddress,
                        Pooled = true
                    };

                    GlobalSendChannel.Writer.TryWrite(packet);

                    Interlocked.Increment(ref _sendQueueCount);

                    offset += chunk;
                }

                if (flush)
                    buffer.Reset();

                Flush();

                return true;
            }
            else
            {
                var len = length;
                var data = AddSignature(buffer.Data, length, out len);

                var packet = new SendPacket
                {
                    Buffer = data,
                    Length = len,
                    Address = socket.RemoteAddress,
                    Pooled = true
                };

                GlobalSendChannel.Writer.TryWrite(packet);

                Interlocked.Increment(ref _sendQueueCount);

                if (flush)
                    buffer.Reset();

                Flush();

                return true;
            }
        }
        else
        {
            return false;
        }
    }

    public static void Flush()
    {
        if (SendEvent != null)
            SendEvent.Set();
    }

    public static void Update(float delta)
    {
        foreach (var kv in Clients.Values)
            kv.Update(delta);
    }

    public static void RunMainLoop()
    {
        var config = ServerConfig.IsLoaded ? ServerConfig.Instance : ServerConfig.CreateDefaultConfig();
        float deltaTime = 1.0f / config.Performance.TickRate;
        TimeSpan frameTime = TimeSpan.FromSeconds(deltaTime);
        TimeSpan targetTime = frameTime;

        Stopwatch sw = Stopwatch.StartNew();
        Stopwatch memLog = Stopwatch.StartNew();
        Stopwatch trimTimer = Stopwatch.StartNew();
        Stopwatch latence = Stopwatch.StartNew();

        int fps = 0;
        double lastTime = sw.Elapsed.TotalMilliseconds;

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

            TimeSpan elapsed = TimeSpan.FromMilliseconds(sw.Elapsed.TotalMilliseconds - now);
            int sleepTime = (int)(frameTime - elapsed).TotalMilliseconds;
            fps++;

            if (sleepTime > 1)
                Thread.Sleep(sleepTime - 1);
        }
    }

    private static unsafe bool ProcessEncryptedPacket(FlatBuffer data, UDPSocket conn, int totalLen)
    {
        try
        {
            // Simple structure: Just encrypted payload, no complex headers
            // Format: CRC32 + Encrypted(EPacketType + ClientPackets + Data)

            uint signature = data.ReadSign(totalLen);
            uint crc32c = CRC32C.Compute(data.Data, totalLen - 4);

            if (signature != crc32c)
            {
                ServerMonitor.Log($"CRC32 validation failed for encrypted packet from {conn.RemoteAddress}");
                return false;
            }

            // Prepare for decryption - payload is everything except CRC32
            int payloadLen = totalLen - 4;
            var payload = new ReadOnlySpan<byte>(data.Data, payloadLen);

            // Simple decryption using sequence as nonce
            Span<byte> plaintext = stackalloc byte[payloadLen];

            if (conn.Session.ConnectionId == 0 || !conn.Session.DecryptPayload(payload, new ReadOnlySpan<byte>(), 0, plaintext, out int plaintextLen))
            {
                FileLogger.Log($"[CRYPTO] Decrypt failed for packet from {conn.RemoteAddress}");
                return false;
            }

            // Create buffer with decrypted content
            var decryptedBuffer = new FlatBuffer(plaintextLen);
            decryptedBuffer.WriteBytes(plaintext.Slice(0, plaintextLen).ToArray());
            decryptedBuffer.RestorePosition(0);

            // Now process as normal packet: EPacketType + ClientPackets + Data
            PacketType packetType = (PacketType)decryptedBuffer.Read<byte>();

            if (packetType == PacketType.Unreliable)
            {
                // Route to unreliable queue for processing
                if (!conn.UnreliableEventQueue.Writer.TryWrite(decryptedBuffer))
                {
                    decryptedBuffer.Free();
                }

                // Update connection timeout
                conn.TimeoutLeft = 30f;
                return true;
            }
            else if (packetType == PacketType.Reliable)
            {
                // Route to reliable queue for processing
                if (conn.Session.ConnectionId != 0)
                    conn.ProcessReliablePacket(conn.Session.SeqRx, decryptedBuffer);
                return true;
            }
            else
            {
                FileLogger.Log($"[SERVER] Unknown packet type: {packetType}");
                decryptedBuffer.Free();
                return false;
            }
        }
        catch (Exception ex)
        {
            FileLogger.Log($"[SERVER] Error processing encrypted packet: {ex.Message}");
            return false;
        }
    }

    private static unsafe bool ProcessEncryptedPacket_Legacy(FlatBuffer data, UDPSocket conn, int totalLen)
    {
        try
        {
            FileLogger.Log($"[SERVER] 🔓 LEGACY: Starting header-based decryption for {totalLen} bytes");
            var header = PacketHeader.Deserialize(data.Data);
            FileLogger.Log($"[SERVER] 🔓 LEGACY: Header parsed - ConnectionId: {header.ConnectionId}, Sequence: {header.Sequence}");

            if (conn.Session.ConnectionId == 0 || header.ConnectionId != conn.Session.ConnectionId)
                return false;

            if (!header.Flags.HasFlag(PacketHeaderFlags.Encrypted) ||
                !header.Flags.HasFlag(PacketHeaderFlags.AEAD_ChaCha20Poly1305))
                return false;

            // Subtract both header and CRC32 signature (4 bytes) from total length
            int payloadLen = totalLen - PacketHeader.Size - 4;

            if (payloadLen <= 16)
                return false;

            var payload = new ReadOnlySpan<byte>(data.Data + PacketHeader.Size, payloadLen);
            var aad = header.GetAAD();
            bool isCompressed = header.Flags.HasFlag(PacketHeaderFlags.Compressed);
            bool isAcknowledgment = header.Flags.HasFlag(PacketHeaderFlags.Acknowledgment);

            Span<byte> plaintext = stackalloc byte[payloadLen * 4]; // Extra space for decompression

            if (conn.Session.ConnectionId == 0 || !conn.Session.DecryptPayloadWithDecompression(payload, aad, header.Sequence, isCompressed, plaintext, out int plaintextLen))
            {
                ServerMonitor.Log($"Failed to decrypt packet from {conn.RemoteAddress}");
                return false;
            }

            // Log packet processing details for ALL unreliable packets (first 10)
            if (header.Channel == PacketChannel.Unreliable && header.Sequence > 0 && header.Sequence <= 10)
            {
                string packetTypeName = "Unknown";
                if (plaintextLen > 0)
                {
                    byte firstByte = plaintext[0];
                    packetTypeName = ((PacketType)firstByte) switch
                    {
                        PacketType.Connect => "Connect",
                        PacketType.Ping => "Ping",
                        PacketType.Pong => "Pong",
                        PacketType.BenckmarkTest => "Benchmark Test",
                        PacketType.ReliableHandshake => "Reliable Handshake",
                        PacketType.Disconnect => "Disconnect",
                        _ => $"Unknown Type {firstByte}"
                    };
                }

                FileLogger.Log($"=== RECEIVED {packetTypeName} (seq={header.Sequence}) ===");
                FileLogger.Log($"[SERVER] Channel: {header.Channel}");
                FileLogger.Log($"[SERVER] Compressed: {isCompressed}");
                FileLogger.Log($"[SERVER] Decrypted Size: {plaintextLen} bytes");
                FileLogger.LogHex("[SERVER] RAW Decrypted Payload", plaintext.Slice(0, plaintextLen));

                                                // Log COMPLETE byte-by-byte analysis
                FileLogger.Log($"[SERVER] === DETAILED PAYLOAD ANALYSIS ===");
                for (int i = 0; i < Math.Min(plaintextLen, 24); i++)
                {
                    string byteDescription = i switch
                    {
                        0 => " (PacketType byte)",
                        1 => " (ClientPacket low byte)",
                        2 => " (ClientPacket high byte)",
                        >= 3 and <= 14 => $" (Position data byte {i - 2})",
                        >= 15 and <= 26 => $" (Rotation data byte {i - 14})",
                        _ => ""
                    };

                    FileLogger.Log($"[SERVER] Payload[{i:D2}] = {plaintext[i]:D3} (0x{plaintext[i]:X2}){byteDescription}");
                }

                // Interpret packet structure correctly
                if (plaintextLen >= 3)
                {
                    byte packetTypeValue = plaintext[0];
                    ushort clientPacketValue = (ushort)(plaintext[1] | (plaintext[2] << 8));
                    FileLogger.Log($"[SERVER] PacketType: {(PacketType)packetTypeValue} (raw: {packetTypeValue})");
                    FileLogger.Log($"[SERVER] ClientPacket: {(ClientPackets)clientPacketValue} (raw: {clientPacketValue})");
                    FileLogger.Log($"[SERVER] Expected: PacketType=Unreliable(4), ClientPacket=SyncEntity(0)");

                    if (packetTypeValue == 4 && clientPacketValue == 0)
                    {
                        FileLogger.Log($"[SERVER] *** CORRECT SYNCENTITY PACKET STRUCTURE! ***");
                    }
                }
            }

            // Handle acknowledgment packets
            if (isAcknowledgment)
            {
                FileLogger.Log($"[SERVER] 🔓 Processing ACK packet (sequence: {header.Sequence})");
                conn.AcknowledgeReliablePacket(header.Sequence);
                return true;
            }

            var decryptedBuffer = new FlatBuffer(plaintextLen + 1);
            decryptedBuffer.WriteBytes(plaintext.Slice(0, plaintextLen).ToArray());

            FileLogger.Log($"[SERVER] 🔓 Decrypted packet: {plaintextLen} bytes, Channel: {header.Channel}");

            // Route to appropriate queue based on channel
            if (header.Channel == PacketChannel.ReliableOrdered)
            {
                ServerMonitor.Log($"[RELIABLE] Processing reliable packet sequence {header.Sequence} from client {conn.Id}");
                conn.ProcessReliablePacket(header.Sequence, decryptedBuffer);
                return true;
            }
            else
            {
                // Log unreliable packet processing
                if (header.Sequence <= 10) // First 10 unreliable packets
                {
                    FileLogger.Log($"[SERVER] Processing unreliable packet seq={header.Sequence} immediately");
                }

                // Process unreliable packet immediately - reset position to read from start
                decryptedBuffer.RestorePosition(0);

                                // Debug: Log DETAILED buffer analysis
                if (header.Sequence <= 10)
                {
                    FileLogger.Log($"[SERVER] === BUFFER PROCESSING ANALYSIS ===");
                    FileLogger.Log($"[SERVER] Buffer position after RestorePosition(0): {decryptedBuffer.Position}");
                    FileLogger.Log($"[SERVER] Buffer capacity: {decryptedBuffer.Capacity}");

                    // Read and log first 10 bytes without advancing position
                    if (decryptedBuffer.Capacity >= 10)
                    {
                        int savedPos = decryptedBuffer.Position;

                        FileLogger.Log($"[SERVER] === FLATBUFFER BYTE ANALYSIS ===");
                        for (int i = 0; i < Math.Min(decryptedBuffer.Capacity, 10); i++)
                        {
                            byte byteVal = decryptedBuffer.Read<byte>();
                            string interpretation = i switch
                            {
                                0 => $" -> PacketType: {(PacketType)byteVal}",
                                1 => " -> ClientPacket low byte",
                                2 => " -> ClientPacket high byte",
                                _ => " -> Data byte"
                            };
                            FileLogger.Log($"[SERVER] FlatBuffer[{i}] = {byteVal:D3} (0x{byteVal:X2}){interpretation}");
                        }

                        // Reset position
                        decryptedBuffer.RestorePosition(savedPos);
                    }
                }

                // Just peek at packet structure for logging without consuming
                if (header.Sequence <= 10)
                {
                    int savedPos = decryptedBuffer.Position;
                    PacketType packetType = (PacketType)decryptedBuffer.Read<byte>();
                    ClientPackets clientPacket = (ClientPackets)decryptedBuffer.Read<ushort>();

                    FileLogger.Log($"[SERVER] Reading PacketType: {packetType} (raw value: {(byte)packetType})");
                    FileLogger.Log($"[SERVER] Reading ClientPacket: {clientPacket} (raw value: {(ushort)clientPacket})");
                    FileLogger.Log($"[SERVER] Adding ClientPacket={clientPacket} seq={header.Sequence} to UnreliableEventQueue");

                    // Restore position for handler processing
                    decryptedBuffer.RestorePosition(savedPos);
                }

                // Send complete buffer to processing queue (starting from position 0)
                if (!conn.UnreliableEventQueue.Writer.TryWrite(decryptedBuffer))
                {
                    decryptedBuffer.Free();
                }

                // Update connection timeout to prevent disconnection
                conn.TimeoutLeft = 30f;
                return true;
            }
        }
        catch (Exception ex)
        {
            ServerMonitor.Log($"Error processing encrypted packet: {ex.Message}");
            return false;
        }
    }



    private static unsafe void PrintBuffer(byte* buffer, int len)
    {
        var sb = new System.Text.StringBuilder(len * 3);

        for (int i = 0; i < len; i++)
            sb.AppendFormat("{0:X2} ", buffer[i]);

        ServerMonitor.Log(sb.ToString());
    }
}

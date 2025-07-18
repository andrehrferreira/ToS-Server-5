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
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Channels;

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
    public int MTU = 1500;
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

    public static TimeSpan ReliableTimeout = TimeSpan.FromMilliseconds(250f);

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

    public static int Mtu
    {
        get
        {
            return _options?.MTU ?? 1500;
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

                    if (pingTimer.Elapsed >= TimeSpan.FromSeconds(5))
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
                                //PrintBuffer(packet.Buffer, packet.Length);
                                int sent = UDP.Unsafe.Send(ServerSocket, &address, packet.Buffer, packet.Length);
                                
                                if (sent > 0)
                                {
                                    //Marshal.FreeHGlobal((IntPtr)packet.Buffer);
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
                PacketType packetType = (PacketType)buffer.Read<byte>();
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

    private static unsafe void HandlePacket(PacketType type, FlatBuffer data, int len, Address address)
    {
        UDPSocket conn;

        try
        {
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
                                ClientPackets clientPacket = (ClientPackets)data.Read<short>();
                                
                                if(PlayerController.TryGet(conn.Id, out var controller))                                
                                    PacketHandler.HandlePacket(controller, ref data, clientPacket);                                
                            }
                        }

                        data.Free();
                    }
                    break;
                case PacketType.Ack:
                    {
                        if (Clients.TryGetValue(address, out conn))
                        {
                            short seq = data.Read<short>();
                            conn.Acknowledge(seq);
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
                /*case PacketType.BenckmarkTest:
                    {
                        if (Clients.TryGetValue(address, out conn))
                        {
                            data.Reset();
                            conn.EventQueue.Writer.TryWrite(data);
                        }
                    }
                    break;*/
            }
        }
        catch (Exception ex)
        {
            ServerMonitor.Log($"Error handling packet: {ex.Message}");
        }
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
        const float deltaTime = 1.0f / 32.0f; 
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

    private static unsafe void PrintBuffer(byte* buffer, int len)
    {
        var sb = new System.Text.StringBuilder(len * 3);

        for (int i = 0; i < len; i++)        
            sb.AppendFormat("{0:X2} ", buffer[i]);
        
        Console.WriteLine(sb.ToString());
    }
}

/*
* Server
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using NanoSockets;
using Org.BouncyCastle.Tls;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using Wormhole.Packets;

namespace Wormhole
{
    public enum ConnectionState : byte
    {
        Diconnected,
        Connecting,
        Connected,
        Disconnecting
    }

    public enum DisconnectReason : byte
    {
        Timeout,
        NegativeSequence,
        InvalidIntegrity,
        InvalidToken,
        LocalDisconnect,
        Other
    }

    public enum PacketType : byte
    {
        Connect,
        Ping,
        Pong,
        Reliable,
        Unreliable,
        Ack,
        Disconnect,
        Error,
        ConnectionDenied,
        ConnectionAccepted,
        Cookie,
        Handshake,
        HandshakeAck,
        ReliableHandshake,
        Heartbeat,
        RetryToken,
        None = 255
    }

    [Flags]
    public enum PacketFlags : byte
    {
        None = 0,
        Encrypted = 1 << 0,
        Rekey = 1 << 2,
        Fragment = 1 << 3,
        Compressed = 1 << 4
    }

    public struct ReceivedPacket
    {
        public unsafe byte* Buffer;
        public int Length;
        public Address Address;
    }

    public sealed class Server
    {
        private static Socket ServerSocket;

        private static bool Running = false;

        private static Thread SendThread;

        private static Thread ReceiveThread;

        private static Channel<Messenger> GlobalSendChannel;
        private static Channel<ReceivedPacket> GlobalReceiveChannel;

        private static unsafe byte* ReceivedBuffer;
        private static Thread ProcessThread;

        public static ConcurrentDictionary<Address, Connection> PendingConnections =
            new ConcurrentDictionary<Address, Connection>();

        public static ConcurrentDictionary<Address, Connection> Clients =
            new ConcurrentDictionary<Address, Connection>();

        private readonly static PriorityQueue<DelayedEvent, long> PriorityQueue =
            new PriorityQueue<DelayedEvent, long>();

        public static uint GetRandomId() =>
            (uint)BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & 0x7FFFFFFF;

        public static bool Start(ushort port)
        {
            try
            {
                if (UDP.Initialize() != Status.OK)
                {
                    Debug.Log("[SERVER] Failed to initialize NanoSockets");

                    return false;
                }

                ServerSocket = UDP.Create(256 * 1024, 256 * 1024);

                if (!ServerSocket.IsCreated)
                {
                    Debug.Log("[SERVER] Failed to create NanoSockets socket");

                    return false;
                }

                var bindAddress = Address.CreateFromIpPort("0.0.0.0", port);

                if (UDP.Bind(ServerSocket, ref bindAddress) != 0)
                {
                    Debug.Log($"[SERVER] Failed to bind to port {port}");

                    return false;
                }

                UDP.SetDontFragment(ServerSocket);

                UDP.SetNonBlocking(ServerSocket, false);

                Running = true;

                CreateSendThread();

                CreateReceiveThread();

                RunMainLoop();

                Debug.Log($"[SERVER] NanoSockets UDP server started on port {port}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.Log($"[SERVER] Error initializing UDP server: {ex.Message}");

                Running = false;

                return false;
            }
        }

        public static void Stop()
        {
            Running = false;

            if (ServerSocket.IsCreated)
                UDP.Destroy(ref ServerSocket);

            unsafe
            {
                if (ReceivedBuffer != null)
                    Marshal.FreeHGlobal((IntPtr)ReceivedBuffer);
            }

            UDP.Deinitialize();
        }

        public static void CreateSendThread()
        {
            if (SendThread != null) return;

            GlobalSendChannel = Channel.CreateUnbounded<Messenger>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            SendThread = new Thread(() =>
            {
                while (Running)
                {
                    if (GlobalSendChannel.Reader.TryRead(out var packet))
                    {
                        unsafe
                        {
                            byte* buffer = packet.Pack();
                            UDP.Unsafe.Send(ServerSocket, &packet.Address, buffer, packet.Length);
                            Marshal.FreeHGlobal((IntPtr)buffer);
                            packet.Free();
                        }
                    }
                }
            });

            SendThread.IsBackground = true;

            SendThread.Start();
        }

        public static unsafe void CreateReceiveThread()
        {
            if (ReceiveThread != null) return;

            GlobalReceiveChannel = Channel.CreateUnbounded<ReceivedPacket>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            ReceiveThread = new Thread(() =>
            {
                ReceiveThread = Thread.CurrentThread;

                StringBuilder ip = new StringBuilder(UDP.hostNameSize);

                Address address = new Address();

                ReceivedBuffer = (byte*)Marshal.AllocHGlobal(1024);

                while (Running)
                {
                    if (UDP.Poll(ServerSocket, 15) > 0)
                    {
                        int dataLength = 0;

                        while ((dataLength = UDP.Unsafe.Receive(ServerSocket, &address, ReceivedBuffer, 1024)) > 0)
                        {                            
                            if (!AddressBlacklist.IsBanned(address))
                            {
                                byte* packetBuffer = (byte*)Marshal.AllocHGlobal(dataLength);
                                Buffer.MemoryCopy(ReceivedBuffer, packetBuffer, dataLength, dataLength);

                                GlobalReceiveChannel.Writer.TryWrite(new ReceivedPacket
                                {
                                    Buffer = packetBuffer,
                                    Length = dataLength,
                                    Address = address
                                });
                            }
                        }
                    }
                }
            });

            ReceiveThread.IsBackground = true;

            ReceiveThread.Start();

            CreateProcessThread();
        }

        public static void CreateProcessThread()
        {
            if (ProcessThread != null) return;

            ProcessThread = new Thread(() =>
            {
                ProcessThread = Thread.CurrentThread;

                while (Running)
                {
                    if (GlobalReceiveChannel.Reader.TryRead(out var receivedPacket))
                    {
                        unsafe
                        {
                            ProcessPacket(receivedPacket.Buffer, receivedPacket.Length, receivedPacket.Address);
                            Marshal.FreeHGlobal((IntPtr)receivedPacket.Buffer);
                        }
                    }
                }
            });

            ProcessThread.IsBackground = true;

            ProcessThread.Start();
        }

        public static unsafe void ProcessPacket(byte* buffer, int len, Address address)
        {
            if (AddressBlacklist.IsBanned(address))
                return;

            Connection conn;

            var msg = Messenger.Unpack(buffer, len, address);

            switch (msg.Type)
            {
                case PacketType.Connect:
                    HandleConnect(new Connection { RemoteAddress = address }, msg);
                    break;
                case PacketType.RetryToken:
                    if(PendingConnections.TryGetValue(address, out conn))
                        HandleRetryToken(conn, msg);
                    break;
                case PacketType.Disconnect:
                    if (Clients.TryGetValue(address, out conn))
                        conn.Disconnect(DisconnectReason.LocalDisconnect);
                    break;
                case PacketType.Unreliable:
                case PacketType.Reliable:
                    if (Clients.TryGetValue(address, out conn))
                        Task.Run(() => PacketHandler.HandlePacket(msg, conn));
                    break;
            }

            msg.Free();
        }

        public static bool Send(PacketType type, Connection socket, PacketFlags flags = PacketFlags.None)
        {
            return GlobalSendChannel.Writer.TryWrite(new Messenger
            {
                Address = socket.RemoteAddress,
                Payload = new ByteBuffer(),
                Type = type,
                Sequence = socket.NextSequence,
                Flags = flags
            });
        }

        public static unsafe bool Send(PacketType type, ref ByteBuffer buffer, Connection socket, PacketFlags flags = PacketFlags.None)
        {
            return GlobalSendChannel.Writer.TryWrite(new Messenger
            {
                Address = socket.RemoteAddress,
                Payload = buffer,
                Type = type,
                Sequence = socket.NextSequence,
                Flags = flags
            });
        }

        public static void RunMainLoop()
        {
            float deltaTime = 1.0f / ServerConfig.Instance.Network.TickRate;
            TimeSpan frameTime = TimeSpan.FromSeconds(deltaTime);
            TimeSpan targetTime = frameTime;

            Stopwatch sw = Stopwatch.StartNew();
            Stopwatch memLog = Stopwatch.StartNew();
            Stopwatch trimTimer = Stopwatch.StartNew();
            Stopwatch latence = Stopwatch.StartNew();
            Stopwatch pingTimer = Stopwatch.StartNew();

            int fps = 0;
            double lastTime = sw.Elapsed.TotalMilliseconds;

            while (Running)
            {
                double now = sw.Elapsed.TotalMilliseconds;
                double delta = now - lastTime;
                lastTime = now;

                if (pingTimer.ElapsedMilliseconds >= 5000)
                {
                    PingTimer();
                    pingTimer.Restart();
                }

                if (latence.Elapsed >= TimeSpan.FromSeconds(1))
                {
                    fps = 0;
                    latence.Restart();
                }

                if (PriorityQueue.Count > 0 && PriorityQueue.TryPeek(out var _, out var due) && due <= now)
                    PriorityQueue.Dequeue().Invoke();

                TimeSpan elapsed = TimeSpan.FromMilliseconds(sw.Elapsed.TotalMilliseconds - now);
                int sleepTime = (int)(frameTime - elapsed).TotalMilliseconds;
                fps++;

                if (sleepTime > 1)
                    Thread.Sleep(sleepTime - 1);
            }
        }

        public static void PingTimer()
        {
            if (Clients.Count > 0)
            {
                var sentTimestamp = Stopwatch.GetTimestamp();
                ushort timestampMs = (ushort)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000) % 65536);
                var buffer = (new PingPacket { SentTimestamp = timestampMs }).ToBuffer();

                foreach (Connection conn in PendingConnections.Values)
                {
                    if(conn.State != ConnectionState.Connected)
                        continue;

                    conn.PingSentAt = DateTime.UtcNow;

                    conn.Send(PacketType.Ping, buffer);
                }

                buffer.Free();
            }
        }

        public static void Heartbeat()
        {
            foreach(Connection conn in PendingConnections.Values)
            {
                if (conn.ExpiredConnecting)
                {
                    conn.Disconnect(DisconnectReason.Timeout);
                    PendingConnections.TryRemove(conn.RemoteAddress, out _);
                }                                 
            }

            PriorityQueue.Enqueue(new HeartBeatEvent(), (long)Stopwatch.StartNew().Elapsed.TotalMilliseconds + 1000);
        }

        // Handlers

        public static unsafe void HandleConnect(Connection conn, Messenger msg)
        {
            if (PendingConnections.ContainsKey(conn.RemoteAddress) || Clients.ContainsKey(conn.RemoteAddress))
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long exp = now + (long)TimeSpan.FromSeconds(3).TotalSeconds;

            conn.State = ConnectionState.Connecting;
            conn.ConnectingTime = exp;

            if (!PendingConnections.TryAdd(conn.RemoteAddress, conn))            
                return;
            
            var token = AddressValidationToken.Generate(conn.RemoteAddress);
            var buffer = (new RetryTokenPacket { Token = token }).ToBuffer();
            Send(PacketType.RetryToken, ref buffer, conn);
            buffer.Free();
        }

        public static unsafe void HandleRetryToken(Connection conn, Messenger msg)
        {
            if(conn.State != ConnectionState.Connecting)
                return;

            byte[] clientPub = msg.Payload.ReadBytes(32);

            byte[] token = msg.Payload.ReadBytes(48);

            if (!conn.ValidateToken(token, conn.RemoteAddress))
            {
                AddressBlacklist.Ban(conn.RemoteAddress);

                conn.Send(PacketType.ConnectionDenied);

                conn.Disconnect(DisconnectReason.InvalidToken);

                return;
            }            
                
            conn.Id = GetRandomId();

            var (serverPub, salt, session) = SecureSession.CreateAsServer(clientPub, conn.Id);

            conn.Session = session;

            PendingConnections.TryRemove(conn.RemoteAddress, out _);
            
            if(Clients.TryAdd(conn.RemoteAddress, conn))
            {
                conn.Send(PacketType.ConnectionAccepted, (new ConnectionAcceptedPacket
                {
                    Id = conn.Id,
                    ServerPublicKey = serverPub,
                    Salt = salt
                }).ToBuffer());

                conn.State = ConnectionState.Connected;
            }

            msg.Free();
        }
    }
}











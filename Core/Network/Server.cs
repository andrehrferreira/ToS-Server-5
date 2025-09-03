/*
* Server
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

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

    public static ConcurrentDictionary<Address, Connection> Clients =
        new ConcurrentDictionary<Address, Connection>();

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

        // Free the allocated buffer
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
        Connection conn;

        var packet = Messenger.Unpack(buffer, len, address);

        if (packet.Type == PacketType.Connect)
            ValidateConnection(packet.Payload);
        else if (Clients.TryGetValue(address, out conn))
            Task.Run(() => PacketHandler.HandlePacket(packet, conn));

        packet.Free();
    }

    public static unsafe void ValidateConnection(ByteBuffer packet)
    {

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

            foreach (var kv in Clients)
            {
                kv.Value.PingSentAt = DateTime.UtcNow;

                kv.Value.Send(PacketType.Ping, buffer);
            }

            buffer.Free();
        }
    }
}

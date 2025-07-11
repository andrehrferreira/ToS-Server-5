/*
* UDPSocket
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
using System.Threading.Channels;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting
}

public enum DisconnectReason
{
    Timeout,
    NegativeSequence,
    RemoteBufferTooBig,
    InvalidIntegrity,
    Other
}

public class UDPSocket
{
    public uint Id;

    public uint EntityId;

    public Address RemoteAddress;

    private Socket ServerSocket;

    public uint Ping = 0;

    public DateTime PingSentAt;

    public DisconnectReason Reason;

    public ConnectionState State;

    public PacketFlags Flags;

    public float TimeoutLeft = 30f;

    public bool EnableIntegrityCheck = true;

    public ushort IntegrityCheck;

    public DateTime IntegrityCheckSentAt;

    public float TimeoutIntegrityCheck = 120f;

    internal ConcurrentDictionary<short, FlatBuffer> ReliablePackets =
        new ConcurrentDictionary<short, FlatBuffer>();

    public Channel<FlatBuffer> EventQueue;

    private short Sequence = 1;

    internal FlatBuffer ReliableBuffer;
    internal FlatBuffer UnreliableBuffer;
    internal FlatBuffer AckBuffer;

    public UDPSocket(Socket serverSocket)
    {
        State = ConnectionState.Disconnected;
        RemoteAddress = default;
        ServerSocket = serverSocket;
        ReliableBuffer = new FlatBuffer(UDPServer.Mtu);
        UnreliableBuffer = new FlatBuffer(UDPServer.Mtu);
        AckBuffer = new FlatBuffer(UDPServer.Mtu);

        EventQueue = Channel.CreateUnbounded<FlatBuffer>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool IsConnected
    {
        get
        {
            return State == ConnectionState.Connected;
        }
    }

    public void Send(INetworkPacket networkPacket, bool reliable = false)
    {
        ref FlatBuffer buffer = ref (reliable ? ref ReliableBuffer : ref UnreliableBuffer);

        if(buffer.Position + networkPacket.Size > buffer.Capacity)
            Send(ref buffer);

        networkPacket.Serialize(ref buffer);

        if (buffer.Position > UDPServer.Mtu / 2)
            Send(ref buffer);
    }

    public void Send(ref FlatBuffer buffer, bool flush = true)
    {
        if(buffer.Position > 0)
            UDPServer.Send(ref buffer, buffer.Position, this, flush);
    }

    public bool Update(float delta)
    {
        if (State == ConnectionState.Disconnected)
            return true;

        TimeoutLeft -= delta;

        if (TimeoutLeft <= 0)
        {
            Disconnect(DisconnectReason.Timeout);

            return false;
        }

        if (EnableIntegrityCheck)
        {
            TimeoutIntegrityCheck -= delta;

            if (TimeoutIntegrityCheck <= 0)
            {
                Disconnect(DisconnectReason.InvalidIntegrity);

                return false;
            }
        }

        if (ReliableBuffer.Position > 0)
        {
            //ReliablePackets[ReliableBuffer.Sequence] = ReliableBuffer;

            Send(ref ReliableBuffer, true);
        }

        if (UnreliableBuffer.Position > 0)
            Send(ref UnreliableBuffer);

        if (AckBuffer.Position > 0)
            Send(ref AckBuffer);

        ProcessPacket();

        return true;
    }

    public void Disconnect(DisconnectReason reason = DisconnectReason.Other)
    {
        if (State != ConnectionState.Disconnected)
        {
            Reason = reason;

            Send(new DisconnectPacket());
        }
    }

    internal void OnDisconnect()
    {
        if (State != ConnectionState.Disconnected)
            State = ConnectionState.Disconnected;
    }

    public bool AddReliablePacket(short packetId, FlatBuffer buffer)
    {
        return ReliablePackets.TryAdd(packetId, buffer);
    }

    public void ProcessPacket()
    {
        while (EventQueue.Reader.TryRead(out var buffer))
        {
            PacketType packetType = (PacketType)buffer.Read<byte>();

            switch (packetType)
            {
                case PacketType.BenckmarkTest:
                {
                    try
                    {
                        var newLocation = buffer.ReadFVector();
                        var newRotation = buffer.ReadFRotator();
                        var count = UDPServer.Clients.Count;

                        if (count == 0)
                            break;

                        var rnd = Random.Shared;
                        int limit = Math.Min(100, count);
                        var packet = new BenchmarkPacket
                        {
                            Id = Id,
                            Positon = newLocation,
                            Rotator = newRotation
                        };

                        for (int i = 0; i < limit; i++)
                        {
                            var randomValue = UDPServer.Clients.Values.ElementAt(rnd.Next(count));
                            packet.Serialize(ref randomValue.UnreliableBuffer);

                            if (randomValue.UnreliableBuffer.Position > UDPServer.Mtu / 2)
                                randomValue.Send(ref randomValue.UnreliableBuffer);

                            Interlocked.Increment(ref UDPServer._packetsSent);
                            Interlocked.Add(ref UDPServer._bytesSent, packet.Size);
                        }
                    }
                    catch
                    {  }
                }
                break;
            }
        }
    }
}

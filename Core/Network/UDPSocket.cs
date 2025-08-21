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

    public SecureSession Session;

    public bool ClientCryptoConfirmed = false;
    public bool ServerCryptoConfirmed = false;
    public uint ClientTestValue;
    public uint ServerTestValue;
    public bool CryptoHandshakeComplete => ClientCryptoConfirmed && ServerCryptoConfirmed;

    internal class ReliablePacketInfo
    {
        public FlatBuffer Buffer;
        public DateTime SentAt;
    }

    internal ConcurrentDictionary<short, ReliablePacketInfo> ReliablePackets =
        new ConcurrentDictionary<short, ReliablePacketInfo>();

    public Channel<FlatBuffer> EventQueue;

    private short Sequence = 1;

    internal FlatBuffer ReliableBuffer;
    internal FlatBuffer UnreliableBuffer;
    internal FlatBuffer AckBuffer;

    private ushort NextFragmentId = 1;

    internal class FragmentInfo
    {
        public FlatBuffer Buffer;
        public uint TotalSize;
        public int Received;
        public DateTime LastUpdate;
    }

    internal ConcurrentDictionary<ushort, FragmentInfo> Fragments =
        new ConcurrentDictionary<ushort, FragmentInfo>();

    internal ushort GetNextFragmentId()
    {
        ushort id = NextFragmentId++;
        if (NextFragmentId == 0)
            NextFragmentId = 1;
        return id;
    }

    internal void CleanupFragments(TimeSpan timeout)
    {
        foreach (var kv in Fragments)
        {
            if (DateTime.UtcNow - kv.Value.LastUpdate > timeout)
            {
                kv.Value.Buffer.Free();
                Fragments.TryRemove(kv.Key, out _);
            }
        }
    }

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
        bool useEncryption = (State == ConnectionState.Connected);

        if (useEncryption && !CryptoHandshakeComplete)
            return;

        if (useEncryption)
        {
            SendEncrypted(networkPacket, reliable);
        }
        else
        {
            SendLegacy(networkPacket, reliable);
        }
    }

    private void SendEncrypted(INetworkPacket networkPacket, bool reliable)
    {
        var payload = new FlatBuffer(networkPacket.Size);
        networkPacket.Serialize(ref payload);

        var header = new PacketHeader
        {
            ConnectionId = Session.ConnectionId,
            Channel = reliable ? PacketChannel.ReliableOrdered : PacketChannel.Unreliable,
            Flags = PacketHeaderFlags.Encrypted | PacketHeaderFlags.AEAD_ChaCha20Poly1305,
            Sequence = Session.SeqTx
        };

        int totalSize = PacketHeader.Size + payload.Position + 16;
        var packet = new FlatBuffer(totalSize);

        unsafe
        {
            byte[] headerBytes = new byte[PacketHeader.Size];
            fixed (byte* headerPtr = headerBytes)
            {
                header.Serialize(headerPtr);
            }
            packet.WriteBytes(headerBytes);
        }

        var aad = header.GetAAD();
        Span<byte> ciphertext = stackalloc byte[payload.Position + 16];

        unsafe
        {
            if (Session.EncryptPayload(new ReadOnlySpan<byte>(payload.Data, payload.Position), aad, ciphertext, out int ciphertextLen))
            {
                packet.WriteBytes(ciphertext.Slice(0, ciphertextLen).ToArray());

                if (reliable)
                {
                    short seq = Sequence++;
                    if (Sequence == short.MaxValue)
                        Sequence = 1;

                    AddReliablePacket(seq, packet);
                }

                UDPServer.Send(ref packet, packet.Position, this, !reliable);
            }
        }

        payload.Free();
        if (!reliable) packet.Free();
    }

    private void SendLegacy(INetworkPacket networkPacket, bool reliable)
    {
        if (reliable)
        {
            var tmp = new FlatBuffer(networkPacket.Size);
            networkPacket.Serialize(ref tmp);

            short seq = Sequence++;

            if (Sequence == short.MaxValue)
                Sequence = 1;

            var packet = new FlatBuffer(tmp.Position + 2);
            packet.Write(PacketType.Reliable);
            packet.Write(seq);

            if (tmp.Position > 1)
            {
                unsafe
                {
                    byte[] payloadData = new byte[tmp.Position - 1];
                    fixed (byte* payloadPtr = payloadData)
                    {
                        Buffer.MemoryCopy(tmp.Data + 1, payloadPtr, payloadData.Length, tmp.Position - 1);
                    }
                    packet.WriteBytes(payloadData);
                }
            }

            AddReliablePacket(seq, packet);
            UDPServer.Send(ref packet, packet.Position, this, false);

            tmp.Free();
        }
        else
        {
            ref FlatBuffer buffer = ref UnreliableBuffer;

            if (buffer.Position + networkPacket.Size > buffer.Capacity)
                Send(ref buffer);

            networkPacket.Serialize(ref buffer);

            if (buffer.Position > UDPServer.Mtu * 0.8)
                Send(ref buffer);
        }
    }

    public bool Send(ref FlatBuffer buffer, bool flush = true)
    {
        if(buffer.Position > 0)
            return UDPServer.Send(ref buffer, buffer.Position, this, flush);
        else
            return false;
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

        float resendMs = Math.Max(Ping, (uint)UDPServer.ReliableTimeout.TotalMilliseconds);

        foreach (var kv in ReliablePackets)
        {
            if ((DateTime.UtcNow - kv.Value.SentAt).TotalMilliseconds >= resendMs)
            {
                UDPServer.Send(ref kv.Value.Buffer, kv.Value.Buffer.Position, this, false);
                kv.Value.SentAt = DateTime.UtcNow;
            }
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

        EntityManager.CleanupSocket(this);
    }

    public bool AddReliablePacket(short packetId, FlatBuffer buffer)
    {
        var info = new ReliablePacketInfo
        {
            Buffer = buffer,
            SentAt = DateTime.UtcNow
        };

        return ReliablePackets.TryAdd(packetId, info);
    }

    internal void Acknowledge(short sequence)
    {
        if (ReliablePackets.TryRemove(sequence, out var info))
        {
            info.Buffer.Free();
        }
    }

    public void ProcessPacket()
    {
        while (EventQueue.Reader.TryRead(out var buffer))
        {
            try
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
                                    FlatBuffer bufferSent = new FlatBuffer(20);
                                    packet.Serialize(ref bufferSent);

                                    if(randomValue.Send(ref bufferSent))
                                    {
                                        Interlocked.Increment(ref UDPServer._packetsSent);
                                        Interlocked.Add(ref UDPServer._bytesSent, packet.Size);
                                    }

                                    bufferSent.Free();
                                }
                            }
                            catch(Exception ex)
                            {
                                ServerMonitor.Log($"Error processing BenchmarkTest packet: {ex.Message}");
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing packet: {ex.Message}");
            }
        }
    }
}

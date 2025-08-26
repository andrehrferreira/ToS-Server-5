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
        public int RetryCount = 0;
        public ulong Sequence;
    }

    internal ConcurrentDictionary<ulong, ReliablePacketInfo> ReliablePackets =
        new ConcurrentDictionary<ulong, ReliablePacketInfo>();

    // Legacy event queue for backward compatibility
    public Channel<FlatBuffer> EventQueue;

    // Separate processing queues
    public Channel<FlatBuffer> ReliableEventQueue;
    public Channel<FlatBuffer> UnreliableEventQueue;

    // Sequence management
    private ulong ReliableSequenceSend = 1;
    private ulong ReliableSequenceReceive = 0;
    private ulong UnreliableSequenceSend = 1;

    // Received packet ordering buffer for reliable packets
    private ConcurrentDictionary<ulong, FlatBuffer> ReliablePacketBuffer =
        new ConcurrentDictionary<ulong, FlatBuffer>();

    // Acknowledgment management
    private ConcurrentDictionary<ulong, DateTime> PendingAcks =
        new ConcurrentDictionary<ulong, DateTime>();

    private short Sequence = 1;  // Legacy sequence for old reliable system

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

                // Create legacy event queue for backward compatibility
        EventQueue = Channel.CreateUnbounded<FlatBuffer>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Create separate queues for reliable and unreliable packets
        ReliableEventQueue = Channel.CreateUnbounded<FlatBuffer>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        UnreliableEventQueue = Channel.CreateUnbounded<FlatBuffer>(new UnboundedChannelOptions
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
            Sequence = Session.SeqTx // This will be the sequence used for encryption
        };

        var aad = header.GetAAD();
        Span<byte> result = stackalloc byte[payload.Position + 1024]; // Extra space for compression overhead

        unsafe
        {
            if (Session.EncryptPayloadWithCompression(new ReadOnlySpan<byte>(payload.Data, payload.Position), aad, result, out int resultLen, out bool wasCompressed))
            {
                if (wasCompressed)
                    header.Flags |= PacketHeaderFlags.Compressed;

                int totalSize = PacketHeader.Size + resultLen;
                var packet = new FlatBuffer(totalSize);

                // Serialize updated header with compression flag
                byte[] headerBytes = new byte[PacketHeader.Size];
                fixed (byte* headerPtr = headerBytes)
                {
                    header.Serialize(headerPtr);
                }
                packet.WriteBytes(headerBytes);
                packet.WriteBytes(result.Slice(0, resultLen).ToArray());

                if (reliable)
                {
                    short seq = Sequence++;
                    if (Sequence == short.MaxValue)
                        Sequence = 1;

                    AddReliablePacket(seq, packet);
                }

                UDPServer.Send(ref packet, packet.Position, this, !reliable);
                if (!reliable) packet.Free();
            }
        }

        payload.Free();
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

        // Handle retransmission for new reliable system
        var reliablePacketsToRetry = new List<KeyValuePair<ulong, ReliablePacketInfo>>();
        foreach (var kv in ReliablePackets)
        {
            if ((DateTime.UtcNow - kv.Value.SentAt).TotalMilliseconds >= resendMs)
            {
                reliablePacketsToRetry.Add(kv);
            }
        }

        foreach (var kv in reliablePacketsToRetry)
        {
            kv.Value.RetryCount++;

            // Max retry attempts before giving up
            if (kv.Value.RetryCount >= 10)
            {
                ReliablePackets.TryRemove(kv.Key, out var removedInfo);
                removedInfo?.Buffer.Free();
                ServerMonitor.Log($"Reliable packet {kv.Key} dropped after 10 retries");
                continue;
            }

            // Resend the packet
            UDPServer.Send(ref kv.Value.Buffer, kv.Value.Buffer.Position, this, false);
            kv.Value.SentAt = DateTime.UtcNow;

            if (kv.Value.RetryCount > 3)
            {
                ServerMonitor.Log($"Reliable packet {kv.Key} retry #{kv.Value.RetryCount}");
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
            SentAt = DateTime.UtcNow,
            Sequence = (ulong)packetId,
            RetryCount = 0
        };

        return ReliablePackets.TryAdd((ulong)packetId, info);
    }

    internal void Acknowledge(short sequence)
    {
        if (ReliablePackets.TryRemove((ulong)sequence, out var info))
        {
            info.Buffer.Free();
        }
    }

    // New reliability system methods
    public void SendReliablePacket(INetworkPacket networkPacket)
    {
        SendEncrypted(networkPacket, true); // Use original SendEncrypted with reliable=true
    }

    public void SendUnreliablePacket(INetworkPacket networkPacket)
    {
        SendEncrypted(networkPacket, false); // Use original SendEncrypted with reliable=false
    }

    private bool AddReliablePacketNew(ulong sequenceId, FlatBuffer buffer)
    {
        var info = new ReliablePacketInfo
        {
            Buffer = buffer,
            SentAt = DateTime.UtcNow,
            Sequence = sequenceId,
            RetryCount = 0
        };

        return ReliablePackets.TryAdd(sequenceId, info);
    }

    public void AcknowledgeReliablePacket(ulong sequence)
    {
        if (ReliablePackets.TryRemove(sequence, out var info))
        {
            info.Buffer.Free();
        }
    }

        public void SendAcknowledgment(ulong sequence)
    {
        // Use original legacy ACK system that worked - break ulong into two uint
        var buffer = new FlatBuffer(9);
        buffer.Write(PacketType.Ack);
        buffer.Write((uint)(sequence & 0xFFFFFFFF));        // Low 32 bits
        buffer.Write((uint)((sequence >> 32) & 0xFFFFFFFF)); // High 32 bits

        UDPServer.Send(ref buffer, buffer.Position, this, false);
    }

    public void ProcessReliablePacket(ulong sequence, FlatBuffer buffer)
    {
        // Send acknowledgment immediately
        SendAcknowledgment(sequence);

        // Check if this is the next expected sequence
        if (sequence == ReliableSequenceReceive + 1)
        {
            // Process this packet and any buffered consecutive ones
            ReliableSequenceReceive = sequence;
            if (!ReliableEventQueue.Writer.TryWrite(buffer))
            {
                buffer.Free();
            }

            // Check for buffered packets
            while (ReliablePacketBuffer.TryRemove(ReliableSequenceReceive + 1, out var nextBuffer))
            {
                ReliableSequenceReceive++;
                if (!ReliableEventQueue.Writer.TryWrite(nextBuffer))
                {
                    nextBuffer.Free();
                }
            }
        }
        else if (sequence > ReliableSequenceReceive + 1)
        {
            // Buffer out-of-order packet
            ReliablePacketBuffer.TryAdd(sequence, buffer);
        }
        else
        {
            // Duplicate or old packet, discard
            buffer.Free();
        }
    }

    public void ProcessPacket()
    {
        // Process reliable packets with ordering guarantee
        while (ReliableEventQueue.Reader.TryRead(out var reliableBuffer))
        {
            try
            {
                ProcessGamePacket(reliableBuffer, true);
            }
            catch (Exception ex)
            {
                ServerMonitor.Log($"Error processing reliable packet: {ex.Message}");
            }
        }

        // Process unreliable packets (no ordering required)
        while (UnreliableEventQueue.Reader.TryRead(out var unreliableBuffer))
        {
            try
            {
                ProcessGamePacket(unreliableBuffer, false);
            }
            catch (Exception ex)
            {
                ServerMonitor.Log($"Error processing unreliable packet: {ex.Message}");
            }
        }
    }

    private void ProcessGamePacket(FlatBuffer buffer, bool isReliable)
    {
        PacketType packetType = (PacketType)buffer.Read<byte>();

        // Log game packet processing for first few packets
        if (ReliableSequenceReceive <= 5 || (!isReliable && ReliableSequenceReceive <= 10))
        {
            FileLogger.Log($"[SERVER] ProcessGamePacket: PacketType={packetType}, IsReliable={isReliable}");
        }

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

                            // Send via the new reliability system
                            if (isReliable)
                            {
                                randomValue.SendReliablePacket(packet);
                            }
                            else
                            {
                                randomValue.SendUnreliablePacket(packet);
                            }

                            Interlocked.Increment(ref UDPServer._packetsSent);
                            Interlocked.Add(ref UDPServer._bytesSent, packet.Size);
                        }
                    }
                    catch(Exception ex)
                    {
                        ServerMonitor.Log($"Error processing BenchmarkTest packet: {ex.Message}");
                    }
                }
                break;

            case PacketType.ReliableHandshake:
                {
                    // Handle reliable handshake packet
                    ServerMonitor.Log($"[QUEUE] Processing ReliableHandshake packet from client {Id}");

                    FileLogger.Log($"=== RELIABLE HANDSHAKE RECEIVED ===");
                    FileLogger.Log($"[SERVER] 🤝 Received ReliableHandshake from client: {Id}");
                    FileLogger.Log($"[SERVER] 🤝 Buffer size: {buffer.Capacity} bytes");
                    FileLogger.Log($"[SERVER] 🤝 Crypto ready: {(Session.ConnectionId != 0 ? "YES" : "NO")}");

                    // Send reliable handshake response
                    var response = new ReliableHandshakePacket { Message = "Handshake Complete" };

                    FileLogger.Log($"[SERVER] 🤝 Sending ReliableHandshake response");
                    FileLogger.Log($"[SERVER] 🤝 Response message: {response.Message}");

                    SendReliablePacket(response);

                    ServerMonitor.Log($"[QUEUE] Sent reliable handshake response to client {Id}");
                    FileLogger.Log($"[SERVER] 🤝 Reliable handshake complete for client: {Id}");
                }
                break;
        }
    }
}

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

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Buffers;

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

    public EndPoint RemoteEndPoint;

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

    internal ConcurrentDictionary<short, ByteBuffer> ReliablePackets =
        new ConcurrentDictionary<short, ByteBuffer>();

    private short Sequence = 1;

    internal ByteBuffer ReliableBuffer;
    internal ByteBuffer UnreliableBuffer;
    internal ByteBuffer AckBuffer;

    public UDPSocket(Socket serverSocket)
    {
        State = ConnectionState.Disconnected;
        RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        ServerSocket = serverSocket;
    }

    public bool IsConnected
    {
        get
        {
            return State == ConnectionState.Connected;
        }
    }

    public void Send(INetworkPacket networkPacket)
    {
        var buffer = new FlatBuffer(networkPacket.Size);
        networkPacket.Serialize(ref buffer);
        UDPServer.Send(buffer, buffer.Position, this);
    }

    public void Send(FlatBuffer buffer)
    {
        UDPServer.Send(buffer, buffer.Position, this);
    }

    public void Send(byte[] buffer)
    {
        if (ServerSocket == null || RemoteEndPoint == null)
            throw new InvalidOperationException("Socket is not initialized or remote endpoint is not set.");

        UDPServer.Send(buffer, this);
    }

    public unsafe void Send(NativeBuffer buffer)
    {
        if (ServerSocket == null || RemoteEndPoint == null)
            throw new InvalidOperationException("Socket is not initialized or remote endpoint is not set.");

        byte[] managed = ArrayPool<byte>.Shared.Rent(buffer.Length);
        Marshal.Copy((IntPtr)buffer.Data, managed, 0, buffer.Length);
        UDPServer.Send(managed, this, pooled: true);
        NativeMemory.Free(buffer.Data);
    }

    public void Send(ByteBuffer buffer)
    {
        if (ServerSocket == null || RemoteEndPoint == null)
            throw new InvalidOperationException("Socket is not initialized or remote endpoint is not set.");

        buffer.Connection = this;      

        UDPServer.Send(buffer);
    }

    public void Send(ServerPacket packetType, ByteBuffer buffer)
    {
        if (ServerSocket == null || RemoteEndPoint == null)
            throw new InvalidOperationException("Socket is not initialized or remote endpoint is not set.");

        buffer.Connection = this;

        UDPServer.Send(packetType, buffer);
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

        if (ReliableBuffer != null)
        {
            ReliablePackets[ReliableBuffer.Sequence] = ReliableBuffer;

            Send(ReliableBuffer);

            ReliableBuffer = null;
        }

        if (UnreliableBuffer != null)
        {
            Send(UnreliableBuffer);

            UnreliableBuffer = null;
        }

        if (AckBuffer != null)
        {
            Send(AckBuffer);

            AckBuffer = null;
        }

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

    public bool AddReliablePacket(short packetId, ByteBuffer buffer)
    {
        return ReliablePackets.TryAdd(packetId, buffer);
    }

    public void ProcessPacket(PacketType type, ByteBuffer buffer)
    {

    }

    public ByteBuffer BeginReliable()
    {
        if (ReliableBuffer == null)
        {
            ReliableBuffer = ByteBufferPool.Acquire();
            ReliableBuffer.Connection = this;

            Sequence = (short)((Sequence + 1) % 16384);

            ReliableBuffer.Write(PacketType.Reliable);
            ReliableBuffer.Write(Sequence);
            //ReliableBuffer.Write(Manager.TickNumber);

            ReliableBuffer.Sequence = Sequence;
            ReliableBuffer.Reliable = true;
        }

        return ReliableBuffer;
    }

    public void EndReliable()
    {
        if (ReliableBuffer.Offset >= 1200)
        {
            ReliablePackets[ReliableBuffer.Sequence] = ReliableBuffer;

            Send(ReliableBuffer);

            ReliableBuffer = null;
        }
    }

    public ByteBuffer BeginUnreliable()
    {
        if (UnreliableBuffer == null)
        {
            UnreliableBuffer = ByteBufferPool.Acquire();
            UnreliableBuffer.Connection = this;

            UnreliableBuffer.Write((byte)PacketType.Unreliable);
            //UnreliableBuffer.Write(Manager.TickNumber);
            UnreliableBuffer.Reliable = false;
        }

        return UnreliableBuffer;
    }

    public void EndUnreliable()
    {
        if (UnreliableBuffer.Length >= 1200)
        {
            Send(UnreliableBuffer);

            UnreliableBuffer = null;
        }
    }

    public void PushAck(short sequence)
    {
        if (AckBuffer == null)
        {
            AckBuffer = ByteBufferPool.Acquire();
            AckBuffer.Connection = this;
            AckBuffer.Reliable = false;
            AckBuffer.Write(PacketType.Ack);
        }

        AckBuffer.Write(sequence);

        if (AckBuffer.Offset > 1198)
        {
            Send(AckBuffer);

            AckBuffer = null;
        }
    }
}

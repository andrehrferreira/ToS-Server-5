using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

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
    public int Id;

    public int EntityId;

    public EndPoint RemoteEndPoint;

    public Socket ServerSocket;

    public int Ping = 0;

    public DateTime PingSentAt;

    public DisconnectReason Reason;

    public ConnectionState State;

    public PacketFlags Flags;

    public float TimeoutLeft = 30f;

    public bool EnableIntegrityCheck = true;

    public ushort IntegrityCheck;

    public DateTime IntegrityCheckSentAt;

    public float TimeoutIntegrityCheck = 120f;

    internal ConcurrentDictionary<uint, ByteBuffer> ReliablePackets =
        new ConcurrentDictionary<uint, ByteBuffer>();

    public bool IsConnected
    {
        get
        {
            return State == ConnectionState.Connected;
        }
    }

    public void Send(byte[] buffer)
    {
        if (ServerSocket == null || RemoteEndPoint == null)
            throw new InvalidOperationException("Socket is not initialized or remote endpoint is not set.");

        UDPServer.Send(buffer, this);
    }

    public void Send(ByteBuffer buffer)
    {
        if (ServerSocket == null || RemoteEndPoint == null)
            throw new InvalidOperationException("Socket is not initialized or remote endpoint is not set.");

        buffer.Connection = this;      

        UDPServer.Send(buffer);
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
        
        return true;
    }

    public void Disconnect(DisconnectReason reason = DisconnectReason.Other)
    {
        if (State != ConnectionState.Disconnected)
        {
            Reason = reason;

            ByteBuffer response = ByteBufferPool.Acquire();

            response.Connection = this;

            response.Reliable = true;

            response.Write(PacketType.Disconnect);

            QueueBuffer.RemoveSocket(Id);

            Send(response);
        }
    }

    internal void OnDisconnect()
    {
        if (State != ConnectionState.Disconnected)
        {
            State = ConnectionState.Disconnected;
        }
    }

    public bool AddReliablePacket(uint packetId, ByteBuffer buffer)
    {
        return ReliablePackets.TryAdd(packetId, buffer);
    }

    public void ProcessPacket(PacketType type, ByteBuffer buffer)
    {

    }
}

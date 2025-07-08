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

    internal ConcurrentDictionary<uint, ByteBuffer> ReliablePackets =
        new ConcurrentDictionary<uint, ByteBuffer>();

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

        UDPServer.Send(managed, this);

        ArrayPool<byte>.Shared.Return(managed);
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
        
        return true;
    }

    public void Disconnect(DisconnectReason reason = DisconnectReason.Other)
    {
        if (State != ConnectionState.Disconnected)
        {
            Reason = reason;

            QueueBuffer.RemoveSocket(Id);

            if (PacketManager.TryGet(PacketType.Disconnect, out var disconnectPacket))            
                Send(disconnectPacket.Serialize());                      
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

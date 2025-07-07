using System.Diagnostics;
using System.Runtime.CompilerServices;

public enum PacketLayerType
{
    Server,
    Client
}

public enum PacketType
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
    CheckIntegrity,
}

[Flags]
public enum PacketFlags : byte
{
    None = 0,
    Encrypted = 1 << 0,
    Queue = 1 << 1,
    XOR = 1 << 2
}

public enum AuthType
{
    Test,
    Default,
    Steam,
    EpicGames,
}

public static class PacketFlagsUtils
{
    public static bool HasFlag(PacketFlags flags, PacketFlags flagToCheck)
    {
        return (flags & flagToCheck) != 0;
    }

    public static PacketFlags AddFlag(PacketFlags flags, PacketFlags flagToAdd)
    {
        return flags | flagToAdd;
    }

    public static PacketFlags RemoveFlag(PacketFlags flags, PacketFlags flagToRemove)
    {
        return flags & ~flagToRemove;
    }
}

public static class PacketPing
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteBuffer Serialize(long timestamp)
    {
        ByteBuffer bufferPing = ByteBufferPool.Acquire();
        bufferPing.Reliable = false;
        bufferPing.Write(PacketType.Ping);
        bufferPing.Write(timestamp);
        return bufferPing;
    }
}

public static class PacketPong
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteBuffer Serialize()
    {
        ByteBuffer bufferPing = ByteBufferPool.Acquire();
        bufferPing.Reliable = false;
        bufferPing.Write(PacketType.Pong);
        return bufferPing;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deserialize(ByteBuffer buffer, UDPSocket conn)
    {
        long sentTimestamp = buffer.ReadLong();
        long nowTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = nowTimestamp - sentTimestamp;

        double rttMs = (elapsedTicks * 1000.0) / Stopwatch.Frequency;

        conn.Ping = (int)rttMs;
        conn.TimeoutLeft = 30f;

        //if (conn.State == ConnectionState.Connected)
        //    Console.WriteLine($"Client: {conn.RemoteEndPoint} Ping: {conn.Ping} ms");
    }
}

public static class PacketConnectionAccepted
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteBuffer Serialize(int Id)
    {
        ByteBuffer bufferPing = ByteBufferPool.Acquire();
        bufferPing.Reliable = true;
        bufferPing.Write(PacketType.ConnectionAccepted);
        bufferPing.Write(Id);
        return bufferPing;
    }
}

public static class PacketConnectionDenied
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteBuffer Serialize()
    {
        ByteBuffer bufferPing = ByteBufferPool.Acquire();
        bufferPing.Reliable = true;
        bufferPing.Write(PacketType.ConnectionDenied);
        return bufferPing;
    }
}

public static class PacketDisconnect
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteBuffer Serialize()
    {
        ByteBuffer buffer = ByteBufferPool.Acquire();
        buffer.Reliable = true;
        buffer.Write(PacketType.Disconnect);
        return buffer;
    }
}

public static class PacketCheckIntegrity
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteBuffer Serialize(ushort index)
    {
        ByteBuffer buffer = ByteBufferPool.Acquire();
        buffer.Reliable = true;
        buffer.Write(PacketType.CheckIntegrity);
        buffer.Write(index);
        return buffer;
    }
}

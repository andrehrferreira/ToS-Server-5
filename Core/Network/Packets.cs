using System.Diagnostics;
using System.Runtime.CompilerServices;

public static partial class PacketRegistration { }

public enum PacketLayerType
{
    Server,
    Client
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
    CheckIntegrity,
    None = 255
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

        conn.Ping = (uint)rttMs;
        conn.TimeoutLeft = 30f;

        //if (conn.State == ConnectionState.Connected)
        //    Console.WriteLine($"Client: {conn.RemoteEndPoint} Ping: {conn.Ping} ms");
    }
}

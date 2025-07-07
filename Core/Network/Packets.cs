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

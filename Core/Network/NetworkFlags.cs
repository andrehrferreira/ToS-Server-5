public enum ConnectionState : byte
{
    Diconnected,
    Connecting,
    Connected,
    Disconnecting
}

public enum DisconnectReason : byte
{
    Timeout,
    NegativeSequence,
    InvalidIntegrity,
    Other
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
    Cookie,
    CryptoTest,
    CryptoTestAck,
    ReliableHandshake,
    None = 255
}

[Flags]
public enum PacketFlags : byte
{
    None = 0,
    Encrypted = 1 << 0,
    Rekey = 1 << 2,
    Fragment = 1 << 3,
    Compressed = 1 << 4
}
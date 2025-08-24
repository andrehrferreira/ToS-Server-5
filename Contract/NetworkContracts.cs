[Contract("Ping", PacketLayerType.Server, ContractPacketFlags.Queue_ToEntity, PacketType.Ping)]
public partial struct PingPacket
{
    [ContractField("ushort")]
    public ushort SentTimestamp;
}

[Contract("Pong", PacketLayerType.Client, ContractPacketFlags.None, PacketType.Pong)]
public partial struct PongPacket
{
    [ContractField("ushort")]
    public ushort SentTimestamp;
}

[Contract("ConnectionAccepted", PacketLayerType.Server, ContractPacketFlags.None, PacketType.ConnectionAccepted)]
public partial struct ConnectionAcceptedPacket
{
    [ContractField("uint")]
    public uint Id;

    [ContractField("byte[]", 32)]
    public byte[] ServerPublicKey;

    [ContractField("byte[]", 16)]
    public byte[] Salt;
}

[Contract("ConnectionDenied", PacketLayerType.Server, ContractPacketFlags.ToEntity, PacketType.ConnectionDenied)]
public partial struct ConnectionDeniedPacket { }

[Contract("Cookie", PacketLayerType.Server, ContractPacketFlags.ToEntity, PacketType.Cookie)]
public partial struct CookiePacket
{
    [ContractField("byte[]", 48)]
    public byte[] Cookie;
}

[Contract("Disconnect",PacketLayerType.Server, ContractPacketFlags.ToEntity, PacketType.Disconnect)]
public partial struct DisconnectPacket { }

[Contract("CheckIntegrity", PacketLayerType.Server, ContractPacketFlags.Reliable_ToEntity, PacketType.CheckIntegrity)]
public partial struct CheckIntegrityPacket
{
    [ContractField("ushort")]
    public ushort Index;

    [ContractField("uint")]
    public uint Version;
}

[Contract("Ack", PacketLayerType.Server, ContractPacketFlags.ToEntity, PacketType.Ack)]
public partial struct AckPacket
{
    [ContractField("short")]
    public short Sequence;
}

[Contract("ReliableHandshake", PacketLayerType.Server, ContractPacketFlags.Reliable_ToEntity, PacketType.ReliableHandshake)]
public partial struct ReliableHandshakePacket
{
    [ContractField("string")]
    public string Message;
}

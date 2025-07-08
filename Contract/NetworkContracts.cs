[Contract(
    "Ping",
    PacketLayerType.Server,
    ContractPacketFlags.Queue_ToEntity,
    PacketType.Ping
)]
public struct Ping
{
    [ContractField("long")]
    public long SentTimestamp;
}

[Contract(
    "Pong",
    PacketLayerType.Client,
    ContractPacketFlags.None,
    PacketType.Pong
)]
public struct Pong
{
    [ContractField("long")]
    public long SentTimestamp;
}

[Contract(
    "ConnectionAccepted",
    PacketLayerType.Server,
    ContractPacketFlags.ToEntity,
    PacketType.ConnectionAccepted
)]
public struct ConnectionAccepted
{
    [ContractField("uint")]
    public uint Id;
}

[Contract(
    "ConnectionDenied",
    PacketLayerType.Server,
    ContractPacketFlags.ToEntity,
    PacketType.ConnectionDenied
)]
public struct ConnectionDenied{}

[Contract(
    "Disconnect",
    PacketLayerType.Server,
    ContractPacketFlags.ToEntity,
    PacketType.Disconnect
)]
public struct Disconnect { }

[Contract(
    "CheckIntegrity",
    PacketLayerType.Server,
    ContractPacketFlags.Reliable_ToEntity,
    PacketType.CheckIntegrity
)]
public struct CheckIntegrity
{
    [ContractField("ushort")]
    public ushort Index;

    [ContractField("uint")]
    public uint Version;
}

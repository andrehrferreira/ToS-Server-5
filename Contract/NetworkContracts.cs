[Contract("Ping", PacketLayerType.Server, ContractPacketFlags.Queue_ToEntity)]
public struct Ping
{
    [ContractField("long")]
    public long SentTimestamp;
}

[Contract("Pong", PacketLayerType.Client)]
public struct Pong
{
    [ContractField("long")]
    public long SentTimestamp;
}

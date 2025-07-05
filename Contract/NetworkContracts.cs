using Networking;

[Contract("Ping")]
public struct PingContract 
{
    [ContractField("long")]
    public long SentTimestamp;
}

[Contract("Pong", PacketLayerType.Client)]
public struct PongContract
{
    [ContractField("long")]
    public long SentTimestamp;
}

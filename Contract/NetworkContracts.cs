using Networking;

[Contract("Ping")]
public struct PingContract 
{
    [ContractField("int")]
    public Int64 SentTimestamp;
}

[Contract("Pong", PacketLayerType.Client)]
public struct PongContract
{
    [ContractField("int")]
    public Int64 SentTimestamp;
}

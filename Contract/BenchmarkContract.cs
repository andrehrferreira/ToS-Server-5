[Contract("Benchmark", PacketLayerType.Server, ContractPacketFlags.None)]
public partial struct BenchmarkPacket
{
    [ContractField("uint")]
    public uint Id;

    [ContractField("FVector")]
    public FVector Positon;

    [ContractField("FRotator")]
    public FRotator Rotator;
}

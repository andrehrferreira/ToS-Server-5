[Contract("DeltaSync", PacketLayerType.Server, ContractPacketFlags.None)]
public partial struct DeltaSyncPacket
{
    [ContractField("uint")]
    public uint Index;

    [ContractField("byte")]
    public byte EntitiesMask;
}

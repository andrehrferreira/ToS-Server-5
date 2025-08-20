[Contract("RekeyRequest", PacketLayerType.Server, ContractPacketFlags.Reliable)]
public partial struct RekeyRequestPacket
{
    [ContractField("ulong")]
    public ulong CurrentSequence;

    [ContractField("byte[]", 16)]
    public byte[] NewSalt;
}

[Contract("RekeyResponse", PacketLayerType.Client, ContractPacketFlags.Reliable)]
public partial struct RekeyResponsePacket
{
    [ContractField("bool")]
    public bool Accepted;

    [ContractField("ulong")]
    public ulong AcknowledgedSequence;
}

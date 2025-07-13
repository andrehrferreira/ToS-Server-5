[Contract("DeltaSync", PacketLayerType.Server, ContractPacketFlags.ToEntity)]
public partial struct DeltaSyncPacket
{
    [ContractField("uint")]
    public uint Index;

    [ContractField("byte")]
    public byte EntitiesMask;

    [ContractField("IntPtr")]
    public IntPtr EntitiesData;
}

[Contract("SyncStateInt", PacketLayerType.Server, ContractPacketFlags.Reliable_ToEntity)]
public partial struct SyncStateIntPacket
{ 
    //uint 
}

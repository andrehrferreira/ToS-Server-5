[Contract("CreateEntity", PacketLayerType.Server, ContractPacketFlags.FromEntity)]
public partial struct CreateEntityPacket
{

}

[Contract("SyncEntity", PacketLayerType.Client)]
public partial struct SyncEntityPacket
{
    [ContractField("FVector")]
    public FVector Positon;

    [ContractField("FRotator")]
    public FRotator Rotator;

    [ContractField("ushort")]
    public ushort AnimationState;
}

[Contract("UpdateEntity", PacketLayerType.Server, ContractPacketFlags.FromEntity)]
public partial struct UpdateEntityPacket
{
    [ContractField("uint")]
    public uint EntityId;

    [ContractField("FVector")]
    public FVector Positon;

    [ContractField("FRotator")]
    public FRotator Rotator;

    [ContractField("ushort")]
    public ushort AnimationState;

    [ContractField("uint")]
    public uint Flags;
}

[Contract("RemoveEntity", PacketLayerType.Server, ContractPacketFlags.Reliable_ToEntity)]
public partial struct RemoveEntityPacket
{

}

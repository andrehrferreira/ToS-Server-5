[Contract("CreateEntity", PacketLayerType.Server, ContractPacketFlags.Reliable_ToEntity)]
public struct CreateEntity
{

}

[Contract("SyncEntity", PacketLayerType.Client)]
public struct SyncEntity
{
    [ContractField("FVector")]
    public FVector Positon;

    [ContractField("FRotator")]
    public FRotator Rotator;

    [ContractField("ushort")]
    public ushort AnimationState;
}

[Contract("UpdateEntity", PacketLayerType.Server, ContractPacketFlags.ToEntity)]
public struct UpdateEntity
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
public struct RemoveEntity
{

}

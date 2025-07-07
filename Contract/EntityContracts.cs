[Contract("CreateEntity", PacketLayerType.Server, ContractPacketFlags.Reliable_ToEntity)]
public struct CreateEntity
{

}

[Contract("SyncEntity", PacketLayerType.Client)]
public struct SyncEntity
{

}

[Contract("UpdateEntity", PacketLayerType.Server, ContractPacketFlags.ToEntity)]
public struct UpdateEntity
{

}

[Contract("RemoveEntity", PacketLayerType.Server, ContractPacketFlags.Reliable_ToEntity)]
public struct RemoveEntity
{

}

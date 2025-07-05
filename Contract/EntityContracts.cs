using Networking;

[Contract("CreateEntity", PacketLayerType.Server, true)]
public struct CreateEntityContract 
{
    
}

[Contract("SyncEntity", PacketLayerType.Client, false)]
public struct SyncEntityContract
{

}

[Contract("UpdateEntity", PacketLayerType.Server, false)]
public struct UpdateEntityContract
{

}

[Contract("RemoveEntity", PacketLayerType.Server, true)]
public struct RemoveEntityContract
{

}
[Contract("CreateEntity", PacketLayerType.Client)]
public partial struct EnterToWorldPacket
{
    [ContractField("uint")]
    public uint CharacterId;    
}

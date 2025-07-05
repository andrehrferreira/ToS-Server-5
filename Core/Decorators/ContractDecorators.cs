using Networking;

[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public class ContractAttribute : Attribute
{
    public string Name { get; }
    public PacketLayerType PacketLayerType { get; set; } = PacketLayerType.Server;
    public bool IsReliable { get; set; } = true;

    public ContractAttribute(string name, PacketLayerType packetLayerType = PacketLayerType.Server, bool isReliable = false)
    {
        Name = name;
        PacketLayerType = packetLayerType;
        IsReliable = isReliable;
    }
}

[AttributeUsage(AttributeTargets.Field, Inherited = false)]
public class ContractFieldAttribute : Attribute
{
    public string Type { get; }

    public ContractFieldAttribute(string type)
    {
        Type = type;
    }
}
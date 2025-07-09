[Flags]
public enum ContractPacketFlags : byte
{
    None = 0,
    Reliable = 1 << 0,
    Queue = 1 << 1,
    Self = 1 << 2,
    AreaOfInterest = 1 << 3,
    ToEntity = 1 << 4,
    NoContent = 1 << 5,
    FromEntity = 1 << 6,

    Queue_Self = Queue | Self,
    Queue_Self_NoContent = Queue | Self | NoContent,
    Queue_AreaOfInterest = Queue | AreaOfInterest,
    Queue_ToEntity = Queue | ToEntity,
    Queue_ToEntity_NoContent = Queue | ToEntity | NoContent,
    Reliable_Self = Reliable | Self,
    Reliable_Self_NoContent = Reliable | Self | NoContent,
    Reliable_ToEntity = Reliable | ToEntity,
    Reliable_ToEntity_NoContent = Reliable | ToEntity | NoContent,
}

public static class ContractPacketFlagsUtils
{
    public static bool IsReliable(this ContractPacketFlags flags)
    {
        return (flags & ContractPacketFlags.Reliable) != 0;
    }

    public static bool IsQueued(this ContractPacketFlags flags)
    {
        return (flags & ContractPacketFlags.Queue) != 0;
    }

    public static bool IsForSelf(this ContractPacketFlags flags)
    {
        return (flags & ContractPacketFlags.Self) != 0;
    }

    public static bool IsForAreaOfInterest(this ContractPacketFlags flags)
    {
        return (flags & ContractPacketFlags.AreaOfInterest) != 0;
    }

    public static bool IsForEntity(this ContractPacketFlags flags)
    {
        return (flags & ContractPacketFlags.ToEntity) != 0;
    }
}

[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public class ContractAttribute : Attribute
{
    public string Name { get; }
    public PacketLayerType LayerType { get; set; } = PacketLayerType.Server;
    public ContractPacketFlags Flags { get; set; } = ContractPacketFlags.None;
    public PacketType PacketType { get; set; } = PacketType.None;

    public ContractAttribute(
        string name,
        PacketLayerType packetLayerType = PacketLayerType.Server,
        ContractPacketFlags flags = ContractPacketFlags.None,
        PacketType packetType = PacketType.None
    )
    {
        Name = name;
        LayerType = packetLayerType;
        Flags = flags;
        PacketType = packetType;
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

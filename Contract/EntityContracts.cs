[Contract("CreateEntity", PacketLayerType.Server, ContractPacketFlags.FromEntity)]
public partial struct CreateEntityPacket
{
    [ContractField("uint")]
    public uint EntityId;

    [ContractField("FVector")]
    public FVector Positon;

    [ContractField("FRotator")]
    public FRotator Rotator;

    [ContractField("uint")]
    public uint Flags;
}

[Contract("SyncEntity", PacketLayerType.Client)]
public partial struct SyncEntityPacket
{
    [ContractField("FVector")]
    public FVector Positon;

    [ContractField("FRotator")]
    public FRotator Rotator;

    [ContractField("FVector")]
    public FVector Velocity;

    [ContractField("ushort")]
    public ushort AnimationState;

    [ContractField("bool")]
    public bool IsFalling;
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

    [ContractField("FVector")]
    public FVector Velocity;

    [ContractField("ushort")]
    public ushort AnimationState;

    [ContractField("uint")]
    public uint Flags;
}

[Contract("RemoveEntity", PacketLayerType.Server, ContractPacketFlags.FromEntity)]
public partial struct RemoveEntityPacket
{
    [ContractField("uint")]
    public uint EntityId;
}

[Contract("SyncEntityQuantized", PacketLayerType.Client)]
public partial struct SyncEntityQuantizedPacket
{
    [ContractField("short")]
    public short QuantizedX;

    [ContractField("short")]
    public short QuantizedY;

    [ContractField("short")]
    public short QuantizedZ;

    [ContractField("short")]
    public short QuadrantX;

    [ContractField("short")]
    public short QuadrantY;

    [ContractField("float")]
    public float Yaw;  // Only Yaw rotation for optimization

    [ContractField("FVector")]
    public FVector Velocity;

    [ContractField("ushort")]
    public ushort AnimationState;

    [ContractField("bool")]
    public bool IsFalling;
}

[Contract("UpdateEntityQuantized", PacketLayerType.Server, ContractPacketFlags.FromEntity)]
public partial struct UpdateEntityQuantizedPacket
{
    [ContractField("uint")]
    public uint EntityId;

    [ContractField("short")]
    public short QuantizedX;

    [ContractField("short")]
    public short QuantizedY;

    [ContractField("short")]
    public short QuantizedZ;

    [ContractField("short")]
    public short QuadrantX;

    [ContractField("short")]
    public short QuadrantY;

    [ContractField("float")]
    public float Yaw;  // Only Yaw rotation for optimization

    [ContractField("FVector")]
    public FVector Velocity;

    [ContractField("ushort")]
    public ushort AnimationState;

    [ContractField("uint")]
    public uint Flags;
}

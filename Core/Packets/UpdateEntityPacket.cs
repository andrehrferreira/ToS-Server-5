// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct UpdateEntityPacket: INetworkPacket
{
    public int Size => 13;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPackets.UpdateEntity);
        buffer.Write(EntityId);
        // Unsupported type: FVector
        // Unsupported type: FRotator
        // Unsupported type: FVector
        buffer.Write(AnimationState);
        buffer.Write(Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        EntityId = buffer.Read<uint>();
        // Unsupported type: FVector
        // Unsupported type: FRotator
        // Unsupported type: FVector
        AnimationState = buffer.Read<ushort>();
        Flags = buffer.Read<uint>();
    }
}

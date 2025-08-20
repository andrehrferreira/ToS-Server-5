// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct CreateEntityPacket: INetworkPacket
{
    public int Size => 11;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPackets.CreateEntity);
        buffer.Write(EntityId);
        // Unsupported type: FVector
        // Unsupported type: FRotator
        buffer.Write(Flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        EntityId = buffer.Read<uint>();
        // Unsupported type: FVector
        // Unsupported type: FRotator
        Flags = buffer.Read<uint>();
    }
}

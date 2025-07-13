// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct DeltaSyncPacket: INetworkPacket
{
    public int Size => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPacket.DeltaSync);
        buffer.Write(Index);
        buffer.Write(EntitiesMask);
    // Unsupported type: IntPtr
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Index = buffer.Read<uint>();
        EntitiesMask = buffer.Read<byte>();
    // Unsupported type: IntPtr
    }
}

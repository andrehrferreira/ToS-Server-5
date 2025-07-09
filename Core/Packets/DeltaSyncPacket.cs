// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct DeltaSyncPacket: INetworkPacket
{
    public int Size => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Reset();
        buffer.Write(PacketType.Reliable);
        buffer.Write((ushort)ServerPacket.DeltaSync);
        buffer.Write(Index);
        buffer.Write(EntitiesMask);
    // Tipo não suportado: IntPtr
    }
}

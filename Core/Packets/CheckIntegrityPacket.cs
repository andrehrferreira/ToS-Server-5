// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct CheckIntegrityPacket: INetworkPacket
{
    public int Size => 7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.CheckIntegrity);
        buffer.Write(Index);
        buffer.Write(Version);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Index = buffer.ReadUShort();
        Version = buffer.ReadUInt();
    }
}

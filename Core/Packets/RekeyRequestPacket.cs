// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct RekeyRequestPacket: INetworkPacket
{
    public int Size => 27;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Reliable);
        buffer.Write((ushort)ServerPackets.RekeyRequest);
        buffer.Write(CurrentSequence);
        buffer.WriteBytes(NewSalt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        CurrentSequence = buffer.Read<ulong>();
        NewSalt = buffer.ReadBytes(16);
    }
}

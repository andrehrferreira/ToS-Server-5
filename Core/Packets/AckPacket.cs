// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct AckPacket : INetworkPacket
{
    public int Size => 3;

    public short Sequence;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Ack);
        buffer.Write(Sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Sequence = buffer.ReadShort();
    }
}

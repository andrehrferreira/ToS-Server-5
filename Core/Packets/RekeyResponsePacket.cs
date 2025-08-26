// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct RekeyResponsePacket: INetworkPacketRecive
{
    public int Size => 12;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Accepted = buffer.Read<bool>();
        AcknowledgedSequence = buffer.Read<ulong>();
    }
}

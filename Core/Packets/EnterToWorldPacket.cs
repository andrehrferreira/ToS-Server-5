// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct EnterToWorldPacket: INetworkPacketRecive
{
    public int Size => 7;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        CharacterId = buffer.Read<uint>();
    }
}

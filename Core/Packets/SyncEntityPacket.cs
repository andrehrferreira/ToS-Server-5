// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct SyncEntityPacket: INetworkPacketRecive
{
    public int Size => 21;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Positon = buffer.ReadFVector(0.1f);
        Rotator = buffer.ReadFRotator(0.1f);
        Speed = buffer.Read<uint>();
        AnimationState = buffer.Read<ushort>();
    }
}

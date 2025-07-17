// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct SyncEntityPacket: INetworkPacketRecive
{
    public int Size => 24;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Positon = buffer.ReadFVector(0.1f);
        Rotator = buffer.ReadFRotator(0.1f);
        Velocity = buffer.ReadFVector(0.1f);
        AnimationState = buffer.Read<ushort>();
        IsFalling = buffer.Read<bool>();
    }
}

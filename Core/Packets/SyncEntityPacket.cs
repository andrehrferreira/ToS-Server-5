// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct SyncEntityPacket: INetworkPacketRecive
{
    public int Size => 6;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        // Unsupported type: FVector
        // Unsupported type: FRotator
        // Unsupported type: FVector
        AnimationState = buffer.Read<ushort>();
        IsFalling = buffer.Read<bool>();
    }
}

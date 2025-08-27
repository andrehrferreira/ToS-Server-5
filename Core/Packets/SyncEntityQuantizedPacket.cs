// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct SyncEntityQuantizedPacket: INetworkPacketRecive
{
    public int Size => 26;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        QuantizedX = buffer.Read<short>();
        QuantizedY = buffer.Read<short>();
        QuantizedZ = buffer.Read<short>();
        QuadrantX = buffer.Read<short>();
        QuadrantY = buffer.Read<short>();
        Yaw = buffer.Read<float>();
        Velocity = buffer.ReadFVector(0.1f);
        AnimationState = buffer.Read<ushort>();
        IsFalling = buffer.Read<bool>();
    }
}

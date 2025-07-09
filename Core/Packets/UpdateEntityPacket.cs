// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct UpdateEntityPacket: INetworkPacket
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Reset();
        buffer.Write((ushort)ServerPacket.UpdateEntity);
        buffer.Write(EntityId);
        buffer.Write(Positon);
        buffer.Write(Rotator);
        buffer.Write(AnimationState);
        buffer.Write(Flags);
    }
}

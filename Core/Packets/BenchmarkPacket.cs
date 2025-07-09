// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct BenchmarkPacket: INetworkPacket
{
    public int Size => 31;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Reset();
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPacket.Benchmark);
        buffer.Write(Id);
        buffer.Write(Positon);
        buffer.Write(Rotator);
    }
}

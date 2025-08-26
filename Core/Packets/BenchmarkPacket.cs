// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct BenchmarkPacket: INetworkPacket
{
    public int Size => 19;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPackets.Benchmark);
        buffer.Write(Id);
        buffer.Write(Positon, 0.1f);
        buffer.Write(Rotator, 0.1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Id = buffer.Read<uint>();
        Positon = buffer.ReadFVector(0.1f);
        Rotator = buffer.ReadFRotator(0.1f);
    }
}

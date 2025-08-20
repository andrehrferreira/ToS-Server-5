// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct BenchmarkPacket: INetworkPacket
{
    public int Size => 7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Unreliable);
        buffer.Write((ushort)ServerPackets.Benchmark);
        buffer.Write(Id);
        // Unsupported type: FVector
        // Unsupported type: FRotator
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Id = buffer.Read<uint>();
        // Unsupported type: FVector
        // Unsupported type: FRotator
    }
}

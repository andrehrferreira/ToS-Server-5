// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct ConnectionAcceptedPacket: INetworkPacket
{
    public int Size => 53;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.ConnectionAccepted);
        buffer.Write(Id);
        buffer.WriteBytes(ServerPublicKey);
        buffer.WriteBytes(Salt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Id = buffer.Read<uint>();
        ServerPublicKey = buffer.ReadBytes(32);
        Salt = buffer.ReadBytes(16);
    }
}

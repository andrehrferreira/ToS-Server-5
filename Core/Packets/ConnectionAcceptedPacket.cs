// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct ConnectionAcceptedPacket: INetworkPacket
{
    public byte[] ServerPublicKey;
    public byte[] Salt;

    public int Size => 53;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.ConnectionAccepted);
        buffer.Write(Id);
        if (ServerPublicKey != null && ServerPublicKey.Length == 32)
        {
            unsafe
            {
                fixed (byte* pk = ServerPublicKey)
                    buffer.WriteBytes(pk, 32);
            }
        }
        if (Salt != null && Salt.Length == 16)
        {
            unsafe
            {
                fixed (byte* s = Salt)
                    buffer.WriteBytes(s, 16);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Id = buffer.Read<uint>();
        ServerPublicKey = new byte[32];
        for (int i = 0; i < 32; i++)
            ServerPublicKey[i] = buffer.Read<byte>();
        Salt = new byte[16];
        for (int i = 0; i < 16; i++)
            Salt[i] = buffer.Read<byte>();
    }
}

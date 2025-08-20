// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public partial struct CookiePacket: INetworkPacket
{
    public int Size => 49;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(ref FlatBuffer buffer)
    {
        buffer.Write(PacketType.Cookie);
        buffer.WriteBytes(Cookie);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(ref FlatBuffer buffer)
    {
        Cookie = buffer.ReadBytes(48);
    }
}

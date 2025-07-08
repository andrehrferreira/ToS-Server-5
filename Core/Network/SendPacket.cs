using System.Net;

public struct SendPacket
{
    public byte[] Buffer;
    public int Length;
    public EndPoint Address;
    public bool Pooled;
}

using NanoSockets;

public class UDPSocket
{
    public uint Id;

    public Address RemoteAddress;

    public uint Ping = 0;

    public DateTime PingSentAt;

    public float TimeoutLeft = 30f;

    public uint Sequence = 0;

    public uint NextSequence
    {
        get
        {
            Sequence++;
            return Sequence;
        }
    }

    public TinyBuffer ParseSecurePacket(PacketHeader header, TinyBuffer payload)
    {
        //if ((Header & PacketHeader.Encrypted) != 0)
        //    throw new NotImplementedException("Descript payload not implemented");

        //if ((Header & PacketHeader.Compressed) != 0)
        //    throw new NotImplementedException("Compress payload not implemented");

        return payload;
    }

    public void Send(PacketType type, TinyBuffer buffer, PacketHeader header = PacketHeader.None)
    {
        UDPServer.Send(type, ref buffer, this, header);
    }
}
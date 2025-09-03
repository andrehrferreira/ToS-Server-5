/*
* UDPSocket
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using NanoSockets;

public sealed class Connection
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

    public ByteBuffer ParseSecurePacket(PacketHeader header, ByteBuffer payload)
    {
        //if ((Header & PacketHeader.Encrypted) != 0)
        //    throw new NotImplementedException("Descript payload not implemented");

        //if ((Header & PacketHeader.Compressed) != 0)
        //    throw new NotImplementedException("Compress payload not implemented");

        return payload;
    }

    public void Send(PacketType type, ByteBuffer buffer, PacketHeader header = PacketHeader.None)
    {
        UDPServer.Send(type, ref buffer, this, header);
    }
}

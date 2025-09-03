/*
* PacketHandler
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

using System.Runtime.CompilerServices;

public interface IPacketServer
{
    void Serialize(ref ByteBuffer buffer);
}

public interface IPacketClient
{
    void Deserialize(ref ByteBuffer buffer);
}

public abstract class PacketHandler
{
    static readonly PacketHandler[] Handlers = new PacketHandler[1024];
    public abstract PacketType Type { get; }
    public abstract void Consume(Messenger msg, Connection connection);

    static PacketHandler()
    {
        var handlerTypes = Namespaces.GetTypesInNamespace("Packets.Handler").ToList();

        foreach (Type t in handlerTypes)
        {
            if (Activator.CreateInstance(t) is PacketHandler packetHandler)            
                Handlers[(int)packetHandler.Type] = packetHandler;            
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HandlePacket(Messenger msg, Connection connection)
    {
        try
        {
            var handler = Handlers[(int)msg.Type];

            if (handler == null) return;
            
            handler.Consume(msg, connection);
        }
        catch (Exception ex){}
    }
}
/*
* PacketHandler
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using System.Runtime.CompilerServices;

namespace Wormhole
{
    public interface IPacketServer
    {
        void Serialize(ref ByteBuffer buffer);
        ByteBuffer ToBuffer();
    }

    public interface IPacketClient
    {
        void Deserialize(ref ByteBuffer buffer);
    }

    public abstract class PacketHandler
    {
        static readonly PacketHandler[] Handlers = new PacketHandler[1024];
        public abstract PacketType Type { get; }
        public abstract void Consume(Messenger msg, Connection conn);

        static PacketHandler()
        {
            var handlerTypes = Namespaces.GetTypesInNamespace("Wormhole.Handlers").ToList();

            foreach (Type t in handlerTypes)
            {
                if (Activator.CreateInstance(t) is PacketHandler packetHandler)
                    Handlers[(int)packetHandler.Type] = packetHandler;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HandlePacket(Messenger msg, Connection conn)
        {
            try
            {
                var handler = Handlers[(int)msg.Type];

                if (handler == null) return;

                msg.UnpackSecurityCompress(conn);

                handler.Consume(msg, conn);
            }
            catch (Exception ex) { }
        }
    }
}

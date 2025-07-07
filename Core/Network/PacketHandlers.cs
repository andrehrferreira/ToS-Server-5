using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public abstract class PacketHandler
{
    static readonly PacketHandler[] Handlers = new PacketHandler[255];
    public abstract ClientPacket Type { get; }
    public abstract void Consume(PlayerController ctrl, ByteBuffer buffer);

    static PacketHandler()
    {
        foreach (Type t in AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(t => t.GetTypes())
                   .Where(t => t.IsClass && t.Namespace == "GameServer.Packets.Receive"))
        {
            if (Activator.CreateInstance(t) is PacketHandler packetHandler)
            {
                Handlers[(int)packetHandler.Type] = packetHandler;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HandlePacket(PlayerController ctrl, ByteBuffer buffer, ClientPacket type)
    {
        Handlers[(int)type]?.Consume(ctrl, buffer);
    }
}

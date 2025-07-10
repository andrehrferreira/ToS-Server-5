using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public abstract class PacketHandler
{
    static readonly PacketHandler[] Handlers = new PacketHandler[1024];
    public abstract ClientPacket Type { get; }
    public abstract void Consume(PlayerController ctrl, ByteBuffer buffer);

    static PacketHandler()
    {
        foreach (Type t in AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(t => t.GetTypes())
                   .Where(t => t.IsClass && t.Namespace == "Packets.Handler"))
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

public interface IPacket
{
    FlatBuffer Buffer { get; }
    void Serialize(object? data = null);
}

public unsafe class Packet : IPacket
{
    public FlatBuffer _buffer = new FlatBuffer(1);

    static readonly Packet[] Handlers = new Packet[1024];

    public FlatBuffer Buffer
    {
        get
        {
            return _buffer;
        }
    }

    public virtual void Serialize(object? data = null)
    {
        throw new NotImplementedException("Serialization method not implemented for this packet type.");
    }

    public virtual void Send(Entity owner, object data, Entity entity)
    {
        throw new NotImplementedException("Send method not implemented for this packet type.");
    }
}

public static class PacketManager
{
    private static readonly ConcurrentDictionary<ServerPacket, IPacket> HighLevelPackets =
        new ConcurrentDictionary<ServerPacket, IPacket>();

    private static readonly ConcurrentDictionary<PacketType, IPacket> LowLevelPackets =
        new ConcurrentDictionary<PacketType, IPacket>();

    public static void Register(ServerPacket packetType, IPacket packet)
    {
        if (!HighLevelPackets.TryAdd(packetType, packet))
            throw new InvalidOperationException($"High-level packet for {packetType} already registered.");
    }

    public static void Register(PacketType packetType, IPacket packet)
    {
        if (!LowLevelPackets.TryAdd(packetType, packet))
            throw new InvalidOperationException($"Low-level packet for {packetType} already registered.");
    }

    public static bool TryGet(ServerPacket packetType, out IPacket packet)
        => HighLevelPackets.TryGetValue(packetType, out packet);

    public static bool TryGet(PacketType packetType, out IPacket packet)
        => LowLevelPackets.TryGetValue(packetType, out packet);

    public static IPacket Get(ServerPacket packetType)
        => HighLevelPackets.TryGetValue(packetType, out var packet)
            ? packet
            : throw new KeyNotFoundException($"Packet {packetType} not registered.");

    public static IPacket Get(PacketType packetType)
        => LowLevelPackets.TryGetValue(packetType, out var packet)
            ? packet
            : throw new KeyNotFoundException($"Packet {packetType} not registered.");

    public static void Clear()
    {
        HighLevelPackets.Clear();
        LowLevelPackets.Clear();
    }
}

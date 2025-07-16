
using NanoSockets;
using Spectre.Console;
using System.Collections.Concurrent;

public partial class PlayerController
{
    public static ConcurrentDictionary<uint, PlayerController> Controllers =
        new ConcurrentDictionary<uint, PlayerController>();

    public UDPSocket Socket { get; private set; }
    public string Token { get; private set; }
    public uint AccountId { get; set; }
    public uint EntityId { get; set; }

    public static bool TryGet(uint id, out PlayerController controller)
    {
        return Controllers.TryGetValue(id, out controller);
    }

    public static PlayerController? Get(uint id)
    {
        if (Controllers.TryGetValue(id, out var controller))
            return controller;

        return null;
    }

    public static bool Add(uint id, PlayerController controller)
    {
        return Controllers.TryAdd(id, controller);
    }

    public PlayerController(UDPSocket socket, string token)
    {
        Socket = socket;
        Token = token;

        Entity entity = new Entity(socket.Id, EntityType.Player);
        var name = new FixedString32();
        name.Set($"Player_{socket.Id}");
        entity.Id = socket.Id;
        entity.Type = EntityType.Player;
        entity.Flags = EntityState.None;
        entity.Name = name;
        entity.Position = new FVector(0, 0, 0);
        entity.Rotation = new FRotator(0, 0, 0);
        entity.AnimState = 0;

        // AccountId vem no token
        EntitySocketMap.Bind(socket.Id, socket);

        EntityId = entity.Id;
        EntityManager.Add(entity);
    }

    public ref Entity Entity
    {
        get
        {
            return ref EntityManager.TryGetRef(EntityId, out _);
        }
    }

    public void Update()
    {
        if (!EntityManager.TryGet(EntityId, out var entity))
            return;

        var neighbours = EntityManager.GetNearestEntities(EntityId);

        if (neighbours.Count == 0)
            return;

        var packet = new UpdateEntityPacket
        {
            EntityId = entity.Id,
            Positon = entity.Position,
            Rotator = entity.Rotation,
            AnimationState = (ushort)entity.AnimState,
            Flags = (uint)entity.Flags
        };

        foreach (var other in neighbours)
        {
            if (EntitySocketMap.TryGet(other.Id, out var socket))
            {
                FlatBuffer buffer = new FlatBuffer(packet.Size);
                packet.Serialize(ref buffer);
                socket.Send(ref buffer);
            }
        }
    }
}

using System.Collections.Concurrent;

[Flags]
public enum EntityState : uint
{
    None = 0,
    IsAlive = 1 << 0,
    IsInCombat = 1 << 1,
    IsMoving = 1 << 2,
    IsCasting = 1 << 3,
    IsInvisible = 1 << 4,
    IsStunned = 1 << 5
}

public enum EntityType
{
    Player,
    NPC,
    Monster,
    Projectile,
    Environment
}

public partial struct Entity
{
    public uint Id;
    public EntityType Type;
    public EntityState Flags;
    public FixedString32 Name;
    public FVector Position;
    public FRotator Rotation;
    public uint AnimState;

    //AIO Control
    public uint WorldId;
    public (int, int, int) CurrentCell;
    public FVector LastPosition;

    public Entity(uint id, EntityType type)
    {
        Id = id;
        Type = type;
    }

    public World GetWorld {
        get
        {
            if (World.Get(WorldId, out var world))
                return world;

            return null;
        }
    }

    public UDPSocket Socket {
        get
        {
            if (EntitySocketMap.TryGet(Id, out var socket))            
                return socket;
            
            return null;
        }
        set
        {
            if (value != null)
            {
                EntitySocketMap.Bind(Id, value);
            }
            else
            {
                EntitySocketMap.Unbind(Id);
            }
        }
    }
}

public static class EntitySocketMap
{
    private static readonly ConcurrentDictionary<uint, UDPSocket> _map = new();

    public static void Bind(uint entityId, UDPSocket socket)
    {
        _map[entityId] = socket;
    }

    public static bool TryGet(uint entityId, out UDPSocket socket)
    {
        return _map.TryGetValue(entityId, out socket);
    }

    public static void Unbind(uint entityId)
    {
        _map.TryRemove(entityId, out _);
    }
}

public static class EntityManager
{
    private static readonly ConcurrentDictionary<uint, Entity> _entities = new();

    public static void Add(Entity entity)
    {
        _entities[entity.Id] = entity;
    }

    public static bool TryGet(uint id, out Entity entity)
    {
        return _entities.TryGetValue(id, out entity);
    }

    public static void Remove(uint id)
    {
        _entities.TryRemove(id, out _);
    }
}

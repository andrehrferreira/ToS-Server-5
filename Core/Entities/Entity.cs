using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

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

[Flags]
public enum EntityDelta
{
    None = 0,
    Position = 1 << 0,
    Rotation = 1 << 1,
    AnimState = 1 << 2,
    Flags = 1 << 3,
    All = Position | Rotation | AnimState | Flags
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

    public World World {
        get
        {
            if (World.Get(WorldId, out var world))
                return world;

            return null;
        }
        set
        {
            if (value != null)
            {
                WorldId = value.Id;
            }
            else
            {
                WorldId = 0;
            }
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

    public bool IsAlive
    {
        get => (Flags & EntityState.IsAlive) != 0;
        set
        {
            if (value)
                Flags |= EntityState.IsAlive;
            else
                Flags &= ~EntityState.IsAlive;
        }
    }

    public void Snapshot()
    {
        EntityManager.Snapshot(Id, this);
    }

    public void Move(FVector position)
    {
        if(position == Position)
            return;

        Snapshot();
        Position = position;
    }

    public EntityDelta Delta()
    {
        Entity last;
        EntityManager.TryGetSnapshot(Id, out last);

        EntityDelta delta = EntityDelta.None;

        if(Position != last.Position) delta |= EntityDelta.Position;
        if(Rotation != last.Rotation) delta |= EntityDelta.Rotation;
        if(AnimState != last.AnimState) delta |= EntityDelta.AnimState;
        if(Flags != last.Flags) delta |= EntityDelta.Flags;

        return delta;
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
    private static readonly ConcurrentDictionary<uint, Entity> _snapshot = new();

    public static void Add(Entity entity)
    {
        _entities[entity.Id] = entity;
    }

    public static void Snapshot(uint id, Entity entity)
    {
        _snapshot[id] = entity;
    }

    public static bool TryGet(uint id, out Entity entity)
    {
        return _entities.TryGetValue(id, out entity);
    }

    public static bool TryGetSnapshot(uint id, out Entity entity)
    {
        return _snapshot.TryGetValue(id, out entity);
    }

    public static void Remove(uint id)
    {
        _entities.TryRemove(id, out _);
    }
}

public partial struct DeltaSyncPacket
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Delta(Entity entity, ref FlatBuffer buffer)
    {
        var delta = entity.Delta();
        buffer.Write((byte)delta);

        if(delta.HasFlag(EntityDelta.Position))
            buffer.Write(entity.Position);
        if (delta.HasFlag(EntityDelta.Rotation))
            buffer.Write(entity.Rotation);
        if (delta.HasFlag(EntityDelta.AnimState))
            buffer.Write(entity.AnimState);
        if (delta.HasFlag(EntityDelta.Flags))
            buffer.Write(entity.Flags);
    }
}

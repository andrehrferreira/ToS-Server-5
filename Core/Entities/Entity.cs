using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[Flags]
[FlagReply]
public enum EntityState : uint
{
    None = 0,
    IsAlive = 1 << 0,
    IsInCombat = 1 << 1,
    IsMoving = 1 << 2,
    IsCasting = 1 << 3,
    IsInvisible = 1 << 4,
    IsStunned = 1 << 5,
    IsFalling = 1 << 6
}

[Flags]
[FlagReply]
public enum EntityDelta
{
    None = 0,
    Position = 1 << 0,
    Rotation = 1 << 1,
    AnimState = 1 << 2,
    Flags = 1 << 3,
    Velocity = 1 << 4,
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
    public FVector Velocity;
    public uint AnimState;

    public DateTime LastUpdate;
    
    //AIO Control
    public uint WorldId;
    public (int, int, int) CurrentCell;
    public FVector LastPosition;

    public Entity(uint id, EntityType type)
    {
        Id = id;
        Type = type;
        LastUpdate = DateTime.UtcNow;
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
        LastUpdate = DateTime.UtcNow;
    }

    public void Rotate(FRotator rotation)
    {
        if(rotation == Rotation)
            return;

        Snapshot();
        Rotation = rotation;
        LastUpdate = DateTime.UtcNow;
    }

    public void SetAnimState(uint animState)
    {
        if(animState == AnimState)
            return;

        Snapshot();
        AnimState = animState;
        LastUpdate = DateTime.UtcNow;
    }

    public void SetVelocity(FVector velocity)
    {
        Velocity = velocity;
        LastUpdate = DateTime.UtcNow;
    }

    public void SetFlags(EntityState flags)
    {
        if (flags == Flags)
            return;

        Snapshot();
        Flags = flags;
        LastUpdate = DateTime.UtcNow;
    }

    public void SetFlag(EntityState flag, bool value)
    {
        if (value)
            Flags |= flag;
        else
            Flags &= ~flag;

        LastUpdate = DateTime.UtcNow;
    }

    public EntityDelta Delta()
    {
        Entity last;
        EntityManager.TryGetSnapshot(Id, out last);

        EntityDelta delta = EntityDelta.Flags | EntityDelta.Velocity;

        if(Position != last.Position) delta |= EntityDelta.Position;
        if(Rotation != last.Rotation) delta |= EntityDelta.Rotation;
        if(AnimState != last.AnimState) delta |= EntityDelta.AnimState;
        //if(Velocity != last.Velocity) delta |= EntityDelta.Velocity;
        //if(Flags != last.Flags) delta |= EntityDelta.Flags;

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
    private static readonly Dictionary<uint, Entity> _entities = new();
    private static readonly Dictionary<uint, Entity> _snapshot = new();
    private static readonly object _lock = new();
    private static Entity NullEntity;

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
        lock (_lock)
        {
            _entities.Remove(id);
        }
    }

    public static List<Entity> GetNearestEntities(uint id, float maxDistance = 5000f, int maxResults = 100)
    {
        if (!TryGet(id, out var originEntity))
            return new List<Entity>();

        var originPos = originEntity.Position;

        var nearbyEntities = new List<(Entity entity, float distance)>(maxResults);

        foreach (var pair in _entities)
        {
            if (pair.Key == id)
                continue;

            var target = pair.Value;
            var distanceSquared = (target.Position - originPos).SizeSquared();

            if (distanceSquared <= maxDistance * maxDistance)
                nearbyEntities.Add((target, distanceSquared));
        }

        return nearbyEntities
            .OrderBy(e => e.distance)
            .Take(maxResults)
            .Select(e => e.entity)
            .ToList();
    }

    public static ref Entity TryGetRef(uint id, out bool found)
    {
        ref var entityRef = ref CollectionsMarshal.GetValueRefOrNullRef(_entities, id);

        if (Unsafe.IsNullRef(ref entityRef))
        {
            found = false;
            return ref NullEntity;
        }

        found = true;
        return ref entityRef;
    }

    public static ref Entity GetRef(uint id)
    {
        ref var entityRef = ref CollectionsMarshal.GetValueRefOrNullRef(_entities, id);

        if (Unsafe.IsNullRef(ref entityRef))
            throw new KeyNotFoundException($"Entity with ID {id} not found");

        return ref entityRef;
    }

    public static void CleanupSocket(UDPSocket socket)
    {
        if (socket == null)
            return;

        List<uint> removeIds = new();

        foreach (var pair in _entities)
        {
            if (pair.Value.Socket == socket)
                removeIds.Add(pair.Key);
        }

        foreach (var id in removeIds)
        {
            Remove(id);
            EntitySocketMap.Unbind(id);
        }
    }
}

public partial struct DeltaSyncPacket
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Delta(Entity entity, ref FlatBuffer buffer)
    {
        var delta = entity.Delta();

        if(delta != EntityDelta.None)
        {
            buffer.Write(PacketType.Unreliable);
            buffer.Write((ushort)ServerPackets.DeltaSync);
            buffer.Write(entity.Id);
            buffer.Write((byte)delta);

            if (delta.HasFlag(EntityDelta.Position))
                buffer.Write(entity.Position);
            if (delta.HasFlag(EntityDelta.Rotation))
                buffer.Write(entity.Rotation);
            if (delta.HasFlag(EntityDelta.AnimState))
                buffer.Write(entity.AnimState);
            if (delta.HasFlag(EntityDelta.Velocity))
                buffer.Write(entity.Velocity);
            if (delta.HasFlag(EntityDelta.Flags))
                buffer.Write(entity.Flags);
        }        
    }
}

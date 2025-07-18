using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public class AOIGridConfig
{
    public FVector CellSize = new FVector(500, 500, 500);
    public int GridWidth = 10;
    public int GridHeight = 10;
    public int GridDepth = 4;
    public int InterestRadius = 2;
}

public class World : IDisposable
{
    public uint Id;
    private StructPool<Entity> _entityPool;
    private int _maxEntities;
    private Thread _worldThread;
    private bool _running = true;
    private float _tickDelta = 1f / 20f;

    private AOIGridConfig _aoiConfig = new AOIGridConfig();
    private Dictionary<(int, int, int), List<uint>> _aoiGrid = new();
    private ConcurrentDictionary<uint, PlayerController> _players = new();

    public static ConcurrentDictionary<uint, World> Worlds = new ConcurrentDictionary<uint, World>();

    public World(int maxEntities = 10000, AOIGridConfig aoiConfig = null)
    {
        _maxEntities = maxEntities;
        _entityPool = new StructPool<Entity>(_maxEntities);

        if (aoiConfig != null)
            _aoiConfig = aoiConfig;

        for (int x = 0; x < _aoiConfig.GridWidth; x++)
        for (int y = 0; y < _aoiConfig.GridHeight; y++)
        for (int z = 0; z < _aoiConfig.GridDepth; z++)
            _aoiGrid[(x, y, z)] = new List<uint>();

        _worldThread = new Thread(WorldLoop) { IsBackground = true };

        _worldThread.Start();
    }

    public static World GetOrCreate(string map)
    {
        if(Worlds.TryGetValue((uint)map.GetHashCode(), out var world))
            return world;

        world = new World();
        world.Id = (uint)map.GetHashCode();
        Worlds.TryAdd(world.Id, world);
        return world;
    }

    public static bool Get(uint Id, out World world)
    {
        return Worlds.TryGetValue(Id, out world);
    }

    private void WorldLoop()
    {
        while (_running)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Tick(_tickDelta);
            sw.Stop();
            var sleep = (int)(_tickDelta * 1000 - sw.Elapsed.TotalMilliseconds);

            if (sleep > 0)
                Thread.Sleep(sleep);
        }
    }

    private (int, int, int) GetCell(FVector pos)
    {
        int x = (int)(pos.X / _aoiConfig.CellSize.X);
        int y = (int)(pos.Y / _aoiConfig.CellSize.Y);
        int z = (int)(pos.Z / _aoiConfig.CellSize.Z);
        return (x, y, z);
    }

    public uint SpawnEntity(string name, FVector position, FRotator rotation, uint animationState = 0)
    {
        uint id = _entityPool.Create();
        ref Entity entity = ref _entityPool.Get(id);
        entity.Id = id;
        entity.Name.Set(name);
        entity.Position = position;
        entity.Rotation = rotation;
        entity.AnimState = animationState;
        entity.Flags = EntityState.None;
        entity.CurrentCell = GetCell(position);
        entity.LastPosition = position;
        var cell = entity.CurrentCell;

        lock (_aoiGrid)
        {
            _aoiGrid[cell].Add(id);
        }

        return id;
    }

    public void AddEntity(Entity entity)
    {
        if (_entityPool.IsActive(entity.Id))
            return;

        //_entityPool.Create(entity.Id, );
    }

    public void AddPlayer(PlayerController player)
    {
        if (player != null)
            _players[player.EntityId] = player;
    }

    public void RemovePlayer(uint entityId)
    {
        _players.TryRemove(entityId, out _);
    }

    public void RemoveEntity(uint id)
    {
        if (!_entityPool.IsActive(id)) 
            return;

        ref Entity entity = ref _entityPool.Get(id);
        var cell = entity.CurrentCell;

        lock (_aoiGrid)
        {
            _aoiGrid[cell].Remove(id);
        }

        _entityPool.Destroy((int)id);
    }

    public void Tick(float deltaTime)
    {
        lock (_aoiGrid)
        {
            for (uint i = 0; i < _entityPool.Capacity; i++)
            {
                if (!_entityPool.IsActive(i))
                    continue;

                ref Entity entity = ref _entityPool.Get(i);
                bool moved = entity.Position != entity.LastPosition;
                var prevCell = entity.CurrentCell;
                var newCell = (moved) ? GetCell(entity.Position) : entity.CurrentCell;

                if (moved)
                {
                    if (prevCell != newCell)
                    {
                        _aoiGrid[prevCell].Remove(entity.Id);
                        _aoiGrid[newCell].Add(entity.Id);
                        entity.CurrentCell = newCell;

                        var prevAOI = GetEntitiesInAOI(entity.Id, prevCell);
                        var currAOI = GetEntitiesInAOI(entity.Id, newCell);

                        // Entidades que saíram da AOI
                        foreach (var otherId in prevAOI)
                        {
                            if (!currAOI.Contains(otherId))
                            {
                                // TODO: Enviar pacote para remover entidade otherId do client de entity.Id
                            }
                        }
                        // Entidades que entraram na AOI
                        foreach (var otherId in currAOI)
                        {
                            if (!prevAOI.Contains(otherId))
                            {
                                // TODO: Enviar pacote para criar entidade para otherId
                                // TODO: Enviar pacote para criar otherId para entity.Id
                            }
                        }
                    }
                    else
                    {
                        var aoiEntities = GetEntitiesInAOI(entity.Id);
                        foreach (var otherId in aoiEntities)
                        {
                            // TODO: Enviar pacote de atualização de posição para otherId
                        }
                    }
                    entity.LastPosition = entity.Position;
                }
            }
        }

        foreach (var player in _players.Values)
        {
            player.Update();
        }
    }

    public List<uint> GetEntitiesInAOI(uint entityId)
    {
        if (!_entityPool.IsActive(entityId))
            return new List<uint>();

        ref Entity entity = ref _entityPool.Get(entityId);
        return GetEntitiesInAOI(entityId, entity.CurrentCell);
    }

    public List<uint> GetEntitiesInAOI(uint entityId, (int, int, int) centerCell)
    {
        if (!_entityPool.IsActive(entityId))
            return new List<uint>();

        var result = new List<uint>();

        lock (_aoiGrid)
        {
            for (int dx = -_aoiConfig.InterestRadius; dx <= _aoiConfig.InterestRadius; dx++)
            for (int dy = -_aoiConfig.InterestRadius; dy <= _aoiConfig.InterestRadius; dy++)
            for (int dz = -_aoiConfig.InterestRadius; dz <= _aoiConfig.InterestRadius; dz++)
            {
                var cell = (centerCell.Item1 + dx, centerCell.Item2 + dy, centerCell.Item3 + dz);
                if (_aoiGrid.TryGetValue(cell, out var list))
                {
                    foreach (var id in list)
                    {
                        if (id != entityId)
                            result.Add(id);
                    }
                }
            }
        }

        return result;
    }

    public ref Entity GetEntity(uint id)
    {
        return ref _entityPool.Get(id);
    }

    public bool TryGetEntity(uint id, out Entity entity)
    {
        if (_entityPool.IsActive(id))
        {
            entity = _entityPool.Get(id);
            return true;
        }

        entity = default;
        return false;
    }

    public bool HasEntity(uint id)
    {
        return _entityPool.IsActive(id);
    }

    public void Dispose()
    {
        _running = false;
        _worldThread?.Join();
        _entityPool.Dispose();
    }
}

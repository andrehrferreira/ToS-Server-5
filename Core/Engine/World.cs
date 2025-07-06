using System;
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
    private StructPool<Entity> _entityPool;
    private int _maxEntities;
    private Thread _worldThread;
    private bool _running = true;
    private float _tickDelta = 1f / 20f;

    private AOIGridConfig _aoiConfig = new AOIGridConfig();
    private Dictionary<(int, int, int), List<int>> _aoiGrid = new();

    public World(int maxEntities = 100000, AOIGridConfig aoiConfig = null)
    {
        _maxEntities = maxEntities;
        _entityPool = new StructPool<Entity>(_maxEntities);

        if (aoiConfig != null)
            _aoiConfig = aoiConfig;

        Console.WriteLine($"Create World supported {maxEntities} entities");

        for (int x = 0; x < _aoiConfig.GridWidth; x++)
        for (int y = 0; y < _aoiConfig.GridHeight; y++)
        for (int z = 0; z < _aoiConfig.GridDepth; z++)
            _aoiGrid[(x, y, z)] = new List<int>();

        _worldThread = new Thread(WorldLoop) { IsBackground = true };
        _worldThread.Start();
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

    public int SpawnEntity(string name, FVector position, FRotator rotation, int animationState = 0)
    {
        int id = _entityPool.Create();
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

    public void RemoveEntity(int id)
    {
        if (!_entityPool.IsActive(id)) return;
        ref Entity entity = ref _entityPool.Get(id);
        var cell = entity.CurrentCell;

        lock (_aoiGrid)
        {
            _aoiGrid[cell].Remove(id);
        }

        _entityPool.Destroy(id);
    }

    public void Tick(float deltaTime)
    {
        lock (_aoiGrid)
        {
            for (int i = 0; i < _entityPool.Capacity; i++)
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
    }

    public List<int> GetEntitiesInAOI(int entityId)
    {
        if (!_entityPool.IsActive(entityId))
            return new List<int>();

        ref Entity entity = ref _entityPool.Get(entityId);
        return GetEntitiesInAOI(entityId, entity.CurrentCell);
    }

    public List<int> GetEntitiesInAOI(int entityId, (int, int, int) centerCell)
    {
        if (!_entityPool.IsActive(entityId)) return new List<int>();
        var result = new List<int>();
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

    public ref Entity GetEntity(int id)
    {
        return ref _entityPool.Get(id);
    }

    public void Dispose()
    {
        _running = false;
        _worldThread?.Join();
        _entityPool.Dispose();
    }
}

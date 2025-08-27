using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Config;
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
    private float _tickDelta = 1f / 60f;  

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

        UpdateTickRate();

        _worldThread = new Thread(WorldLoop) { IsBackground = true };
        _worldThread.Start();
    }

    private void UpdateTickRate()
    {
        if (ServerConfig.IsLoaded)
        {
            var adaptiveConfig = ServerConfig.Instance.AreaOfInterest.AdaptiveSync;
            _tickDelta = 1f / adaptiveConfig.ServerTickRate;
            FileLogger.Log($"[WORLD] 🕒 Configurado com taxa de atualização de {adaptiveConfig.ServerTickRate} FPS (intervalo: {_tickDelta:F4}s)");
        }
        else
        {
            FileLogger.Log($"[WORLD] ⚠️ Configuração não carregada, usando taxa de atualização padrão de 60 FPS");
        }
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
    }

    public void AddPlayer(PlayerController player)
    {
        if (player == null)
            return;

        _players[player.EntityId] = player;

        SendInitialEntities(player);
    }

    private void SendInitialEntities(PlayerController newPlayer)
    {
        if (!EntityManager.TryGet(newPlayer.EntityId, out var playerEntity))
            return;

        var aoiConfig = ServerConfig.Instance.AreaOfInterest;
        if (!aoiConfig.Enabled)
            return;

        float aoiDistance = aoiConfig.GetDistanceForEntityType("Player");
        int syncCount = 0;
        List<PlayerController> playersInRange = new List<PlayerController>();

        FileLogger.Log($"[INITIAL SYNC] ⚡ Sending initial entities to player {newPlayer.EntityId}");

        foreach (var otherPlayer in _players.Values)
        {
            if (otherPlayer.EntityId == newPlayer.EntityId)
                continue;

            if (!EntityManager.TryGet(otherPlayer.EntityId, out var otherEntity))
                continue;

            float distance = FVector.Distance(playerEntity.Position, otherEntity.Position);
            if (distance <= aoiDistance)
            {
                var createPacket = new CreateEntityPacket
                {
                    EntityId = otherEntity.Id,
                    Positon = otherEntity.Position,
                    Rotator = otherEntity.Rotation,
                    Flags = (uint)otherEntity.Flags
                };

                var createBuffer = new FlatBuffer(createPacket.Size);
                createPacket.Serialize(ref createBuffer);
                newPlayer.Socket.Send(ref createBuffer, true);

                syncCount++;

                if (newPlayer._visibleEntities == null)
                    newPlayer._visibleEntities = new HashSet<uint>();

                newPlayer._visibleEntities.Add(otherEntity.Id);

                playersInRange.Add(otherPlayer);
            }
        }

        FileLogger.Log($"[INITIAL SYNC] ✅ Sent {syncCount} initial entities to player {newPlayer.EntityId}");

        NotifyExistingPlayersAboutNewPlayer(newPlayer, playersInRange);
    }

    private void NotifyExistingPlayersAboutNewPlayer(PlayerController newPlayer, List<PlayerController> playersInRange)
    {
        if (!EntityManager.TryGet(newPlayer.EntityId, out var playerEntity))
            return;

        var createPacket = new CreateEntityPacket
        {
            EntityId = playerEntity.Id,
            Positon = playerEntity.Position,
            Rotator = playerEntity.Rotation,
            Flags = (uint)playerEntity.Flags
        };

        int notifiedCount = 0;

        foreach (var existingPlayer in playersInRange)
        {
            if (existingPlayer._visibleEntities == null)
                existingPlayer._visibleEntities = new HashSet<uint>();

            existingPlayer._visibleEntities.Add(playerEntity.Id);

            var createBuffer = new FlatBuffer(createPacket.Size);
            createPacket.Serialize(ref createBuffer);
            existingPlayer.Socket.Send(ref createBuffer, true);

            notifiedCount++;
        }

        FileLogger.Log($"[INITIAL SYNC] 📢 Notified {notifiedCount} existing players about new player {newPlayer.EntityId}");
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
                bool needsPeriodicSync = false;

                if (!moved && entity.Type == EntityType.Player)
                {
                    var adaptiveConfig = ServerConfig.Instance.AreaOfInterest.AdaptiveSync;
                    TimeSpan timeSinceLastSync = DateTime.UtcNow - entity.LastSyncTime;
                    if (timeSinceLastSync.TotalSeconds >= adaptiveConfig.PeriodicSyncInterval)
                    {
                        needsPeriodicSync = true;
                        entity.LastSyncTime = DateTime.UtcNow;
                        FileLogger.Log($"[PERIODIC SYNC] ⏱️ Sending periodic update for player {entity.Id} after {timeSinceLastSync.TotalSeconds:F1}s");
                    }
                }

                var prevCell = entity.CurrentCell;
                var newCell = (moved) ? GetCell(entity.Position) : entity.CurrentCell;

                if (moved || needsPeriodicSync)
                {
                    if (prevCell != newCell)
                    {
                        _aoiGrid[prevCell].Remove(entity.Id);
                        _aoiGrid[newCell].Add(entity.Id);
                        entity.CurrentCell = newCell;

                        var prevAOI = GetEntitiesInAOI(entity.Id, prevCell);
                        var currAOI = GetEntitiesInAOI(entity.Id, newCell);

                        foreach (var otherId in prevAOI)
                        {
                            if (!currAOI.Contains(otherId))
                            {
                                if (entity.Type == EntityType.Player && PlayerController.TryGet(entity.Id, out var controller))
                                {
                                    if (controller._visibleEntities != null)
                                    {
                                        controller._visibleEntities.Remove(otherId);
                                    }

                                    var removePacket = new RemoveEntityPacket { EntityId = otherId };
                                    var removeBuffer = new FlatBuffer(removePacket.Size);
                                    removePacket.Serialize(ref removeBuffer);
                                    controller.Socket.Send(ref removeBuffer, true);
                                }
                            }
                        }

                        foreach (var otherId in currAOI)
                        {
                            if (!prevAOI.Contains(otherId))
                            {
                                if (entity.Type == EntityType.Player && PlayerController.TryGet(entity.Id, out var controller))
                                {
                                    if (!EntityManager.TryGet(otherId, out var otherEntity))
                                        continue;

                                    if (controller._visibleEntities == null)
                                        controller._visibleEntities = new HashSet<uint>();

                                    controller._visibleEntities.Add(otherId);

                                    var createPacket = new CreateEntityPacket
                                    {
                                        EntityId = otherId,
                                        Positon = otherEntity.Position,
                                        Rotator = otherEntity.Rotation,
                                        Flags = (uint)otherEntity.Flags
                                    };

                                    var createBuffer = new FlatBuffer(createPacket.Size);
                                    createPacket.Serialize(ref createBuffer);
                                    controller.Socket.Send(ref createBuffer, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        var aoiEntities = GetEntitiesInAOI(entity.Id);

                        if (entity.Type == EntityType.Player && PlayerController.TryGet(entity.Id, out var controller))
                        {
                            if (needsPeriodicSync)
                            {
                                SendPeriodicUpdateToNearbyPlayers(entity.Id);
                            }
                        }
                    }

                    if (moved)
                    {
                        entity.LastPosition = entity.Position;
                    }
                }
            }
        }

        foreach (var player in _players.Values)
        {
            player.Update();
        }
    }

    private void SendPeriodicUpdateToNearbyPlayers(uint entityId)
    {
        if (!EntityManager.TryGet(entityId, out var entity))
            return;

        if (!PlayerController.TryGet(entityId, out var sourceController))
            return;

        var aoiConfig = ServerConfig.Instance.AreaOfInterest;
        if (!aoiConfig.Enabled)
            return;

        float aoiDistance = aoiConfig.GetDistanceForEntityType("Player");
        int updateCount = 0;

        foreach (var otherPlayer in _players.Values)
        {
            if (otherPlayer.EntityId == entityId)
                continue;

            if (!EntityManager.TryGet(otherPlayer.EntityId, out var otherEntity))
                continue;

            float distance = FVector.Distance(entity.Position, otherEntity.Position);
            if (distance <= aoiDistance)
            {
                var adaptiveConfig = ServerConfig.Instance.AreaOfInterest.AdaptiveSync;
                bool shouldSync = true;

                bool isStationary = entity.Velocity.Length() < adaptiveConfig.MovementThreshold;
                bool isDistant = distance > adaptiveConfig.DistanceThreshold;

                if (isStationary || isDistant)
                {
                    float syncRate = isStationary ? adaptiveConfig.StationarySyncRate : adaptiveConfig.DistantSyncRate;

                    Random random = new Random();
                    shouldSync = random.NextDouble() < syncRate;

                    if (!shouldSync)
                        continue;
                }

                const int estimatedPacketSize = 40;
                const int safetyMargin = 100;
                int safetyLimit = UDPServer.Mtu - estimatedPacketSize - safetyMargin;

                if (otherPlayer.Socket.UnreliableBuffer.Position > safetyLimit)
                {
                    otherPlayer.Socket.Send(ref otherPlayer.Socket.UnreliableBuffer);
                }

                try
                {
                    var updatePacket = new UpdateEntityQuantizedPacket();
                    var worldConfig = ServerConfig.Instance.WorldOriginRebasing;
                    var defaultMapName = ServerConfig.Instance.Server.DefaultMapName;

                    if (!worldConfig.Maps.TryGetValue(defaultMapName, out var mapConfig))
                    {
                        mapConfig = worldConfig.Maps.Values.FirstOrDefault() ?? new MapSectionConfig();
                    }

                    var quadrantInfo = mapConfig.GetQuadrantFromPosition(entity.Position);
                    var quadrantX = (short)quadrantInfo.X;
                    var quadrantY = (short)quadrantInfo.Y;

                    var quantizedPos = mapConfig.QuantizePosition(entity.Position, quadrantX, quadrantY);

                    updatePacket.EntityId = entityId;
                    updatePacket.QuadrantX = quadrantX;
                    updatePacket.QuadrantY = quadrantY;
                    updatePacket.QuantizedX = quantizedPos.X;
                    updatePacket.QuantizedY = quantizedPos.Y;
                    updatePacket.QuantizedZ = quantizedPos.Z;
                    updatePacket.Yaw = entity.Rotation.Yaw;
                    updatePacket.Velocity = entity.Velocity;
                    updatePacket.AnimationState = (ushort)entity.AnimState;
                    updatePacket.Flags = (uint)entity.Flags;
                    updatePacket.Serialize(ref otherPlayer.Socket.UnreliableBuffer);

                    string syncType = isStationary ? "STATIONARY" : (isDistant ? "DISTANT" : "NORMAL");
                    
                    if (isStationary || isDistant)
                    {
                        FileLogger.Log($"[ADAPTIVE SYNC] {syncType} sync for player {entityId} to {otherPlayer.EntityId} at distance {distance:F1}");
                    }
                }
                catch (IndexOutOfRangeException ex)
                {
                    FileLogger.Log($"[PERIODIC SYNC] ⚠️ Buffer overflow ao enviar update para player {otherPlayer.EntityId}: {ex.Message}");
                    otherPlayer.Socket.Send(ref otherPlayer.Socket.UnreliableBuffer);
                }

                updateCount++;
            }
        }

        if (updateCount > 0)
        {
            FileLogger.Log($"[PERIODIC SYNC] ✅ Sent periodic update for player {entityId} to {updateCount} nearby players");
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

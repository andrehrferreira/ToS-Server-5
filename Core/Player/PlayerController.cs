
using NanoSockets;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Linq;
using Core.Config;

public partial class PlayerController
{
    public static ConcurrentDictionary<uint, PlayerController> Controllers =
        new ConcurrentDictionary<uint, PlayerController>();

    public UDPSocket Socket { get; private set; }
    public string Token { get; private set; }
    public uint AccountId { get; set; }
    public uint EntityId { get; set; }

    // Simple AOI tracking - entities currently visible to this player
    internal HashSet<uint> _visibleEntities = new HashSet<uint>();

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

        if ((DateTime.UtcNow - entity.LastUpdate).TotalMilliseconds > 300)
        {
            entity.Snapshot();
            entity.Velocity = FVector.Zero;
        }

        var neighbours = EntityManager.GetNearestEntities(EntityId);

        if (neighbours.Count == 0)
            return;

        var packet = new DeltaSyncPacket();

        foreach (var other in neighbours)
        {
            if (EntitySocketMap.TryGet(other.Id, out var socket))
                packet.Delta(entity, ref socket.UnreliableBuffer);
        }
    }

    /// <summary>
    /// Execute AOI-based replication with automatic CreateEntity/RemoveEntity handling
    /// </summary>
    public void ExecuteAOIReplication(Action<List<PlayerController>> callback)
    {
        if (!ServerConfig.Instance.AreaOfInterest.Enabled)
        {
            // AOI disabled - send to all players
            var allPlayers = Controllers.Values.Where(p => p.EntityId != this.EntityId).ToList();
            callback(allPlayers);
            return;
        }

        var playersInRange = GetPlayersInAOIRange();

        // Handle AOI enter/exit tracking
        HandleAOIChanges(playersInRange);

        if (playersInRange.Count > 0)
        {
            callback(playersInRange);
        }
    }

    /// <summary>
    /// Handle AOI enter/exit events with CreateEntity/RemoveEntity packets
    /// </summary>
    private void HandleAOIChanges(List<PlayerController> currentPlayersInRange)
    {
        if (!EntityManager.TryGet(EntityId, out var sourceEntity))
            return;

        var currentEntityIds = new HashSet<uint>(currentPlayersInRange.Select(p => p.EntityId));

        // Find players who entered AOI (new entities this player should see)
        var enteredEntities = currentEntityIds.Except(_visibleEntities);
        foreach (var entityId in enteredEntities)
        {
            if (PlayerController.TryGet(entityId, out var enteredPlayer) &&
                EntityManager.TryGet(entityId, out var enteredEntity))
            {
                // Send CreateEntity packet to THIS player for the ENTERED entity
                var createPacket = new CreateEntityPacket
                {
                    EntityId = entityId,
                    Positon = enteredEntity.Position,
                    Rotator = enteredEntity.Rotation,
                    Flags = (uint)enteredEntity.Flags
                };

                var createBuffer = new FlatBuffer(createPacket.Size);
                createPacket.Serialize(ref createBuffer);
                Socket.Send(ref createBuffer, true);

                FileLogger.Log($"[AOI ENTER] ✅ Player {EntityId} can now see Entity {entityId}");
            }
        }

        // Find players who left AOI (entities this player should no longer see)
        var leftEntities = _visibleEntities.Except(currentEntityIds);
        foreach (var entityId in leftEntities)
        {
            // Send RemoveEntity packet to THIS player for the LEFT entity
            var removePacket = new RemoveEntityPacket
            {
                EntityId = entityId
            };

            var removeBuffer = new FlatBuffer(removePacket.Size);
            removePacket.Serialize(ref removeBuffer);
            Socket.Send(ref removeBuffer, true);

            FileLogger.Log($"[AOI EXIT] ❌ Player {EntityId} can no longer see Entity {entityId}");
        }

        // Update visible entities list
        _visibleEntities = currentEntityIds;
    }

    /// <summary>
    /// Get players within AOI range using simple distance calculation
    /// </summary>
    private List<PlayerController> GetPlayersInAOIRange()
    {
        var result = new List<PlayerController>();
        var aoiConfig = ServerConfig.Instance.AreaOfInterest;

        if (!EntityManager.TryGet(EntityId, out var sourceEntity))
            return result;

        string sourceEntityType = "Player"; // For now, assume all entities are players
        float aoiDistance = aoiConfig.GetDistanceForEntityType(sourceEntityType);

        foreach (var otherPlayer in Controllers.Values)
        {
            if (otherPlayer.EntityId == EntityId)
                continue;

            if (!EntityManager.TryGet(otherPlayer.EntityId, out var targetEntity))
                continue;

            float distance = FVector.Distance(sourceEntity.Position, targetEntity.Position);
            if (distance <= aoiDistance)
            {
                result.Add(otherPlayer);
            }
        }

        return result;
    }
}


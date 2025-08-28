namespace Packets.Handler
{
    public class SyncEntityQuantized : PacketHandler
    {
        private static int syncCounter = 0; // Static counter at class level

        public override ClientPackets Type => ClientPackets.SyncEntityQuantized;

        public override void Consume(PlayerController ctrl, ref FlatBuffer buffer)
        {
            // Debug: Log values AFTER deserialization to check data integrity
            if (syncCounter < 5)
            {
                FileLogger.Log($"[QUANTIZED HANDLER] === DESERIALIZATION ANALYSIS #{syncCounter + 1} ===");
                FileLogger.Log($"[QUANTIZED HANDLER] Buffer Position: {buffer.Position}, Capacity: {buffer.Capacity}");
            }

            // Skip packet header: PacketType (1 byte) + ClientPacket (2 bytes) = 3 bytes
            buffer.Read<byte>();    // Skip PacketType
            buffer.Read<ushort>();  // Skip ClientPacket

            if (syncCounter < 5)
            {
                FileLogger.Log($"[QUANTIZED HANDLER] Buffer Position after header skip: {buffer.Position}");
            }

            SyncEntityQuantizedPacket syncPacket = new SyncEntityQuantizedPacket();
            syncPacket.Deserialize(ref buffer);

            // Debug: Log sync packets (log first few to verify data)
            syncCounter++;

            // Debug: Log deserialized values to check data integrity
            if (syncCounter <= 5)
            {
                FileLogger.Log($"[QUANTIZED HANDLER] === DESERIALIZED VALUES #{syncCounter} ===");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw QuantizedX: {syncPacket.QuantizedX}");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw QuantizedY: {syncPacket.QuantizedY}");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw QuantizedZ: {syncPacket.QuantizedZ}");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw QuadrantX: {syncPacket.QuadrantX}");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw QuadrantY: {syncPacket.QuadrantY}");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw Yaw: {syncPacket.Yaw}");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw Velocity: X={syncPacket.Velocity.X} Y={syncPacket.Velocity.Y} Z={syncPacket.Velocity.Z}");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw AnimationState: {syncPacket.AnimationState}");
                FileLogger.Log($"[QUANTIZED HANDLER] Raw IsFalling: {syncPacket.IsFalling}");
            }

            var config = Core.Config.ServerConfig.Instance;
            if (!config.WorldOriginRebasing.Enabled)
            {
                FileLogger.Log($"[QUANTIZED HANDLER] ❌ World Origin Rebasing is disabled, ignoring quantized packet");
                return;
            }

            // Get current map configuration
            var mapName = config.Server.DefaultMapName;
            if (!config.WorldOriginRebasing.Maps.TryGetValue(mapName, out var mapConfig))
            {
                FileLogger.Log($"[QUANTIZED HANDLER] ❌ Map '{mapName}' not found in configuration");
                return;
            }

            // Log simples para debug
            if (syncCounter <= 3)
            {
                Console.WriteLine($"[DEBUG] Processando SyncEntityQuantized #{syncCounter} para EntityId: {ctrl.EntityId}");
                Console.WriteLine($"[DEBUG] Quadrante: ({syncPacket.QuadrantX}, {syncPacket.QuadrantY}), Quantizado: ({syncPacket.QuantizedX}, {syncPacket.QuantizedY}, {syncPacket.QuantizedZ})");
            }

            // Convert quantized position back to world position
            var worldPosition = mapConfig.DequantizePosition(
                syncPacket.QuantizedX,
                syncPacket.QuantizedY,
                syncPacket.QuantizedZ,
                syncPacket.QuadrantX,
                syncPacket.QuadrantY
            );

            // Create rotation from Yaw only if enabled, otherwise use default rotation
            FRotator rotation;
            if (config.WorldOriginRebasing.EnableYawOnlyRotation)
            {
                rotation = new FRotator { Pitch = 0.0f, Yaw = syncPacket.Yaw, Roll = 0.0f };
            }
            else
            {
                // If not using Yaw-only, we need to get the rotation from somewhere else
                // For now, just use Yaw with zero pitch and roll
                rotation = new FRotator { Pitch = 0.0f, Yaw = syncPacket.Yaw, Roll = 0.0f };
            }

            if (syncCounter <= 20) // Log first 20 packets with full details (temporary)
            {
                FileLogger.Log($"[QUANTIZED HANDLER] ✅ SyncEntityQuantized #{syncCounter} for EntityId: {ctrl.EntityId}");
                FileLogger.Log($"[QUANTIZED HANDLER] 🎯 Quadrant: X={syncPacket.QuadrantX} Y={syncPacket.QuadrantY}");
                FileLogger.Log($"[QUANTIZED HANDLER] 📦 Quantized: X={syncPacket.QuantizedX} Y={syncPacket.QuantizedY} Z={syncPacket.QuantizedZ}");
                FileLogger.Log($"[QUANTIZED HANDLER] 📍 World Position: X={worldPosition.X:F3} Y={worldPosition.Y:F3} Z={worldPosition.Z:F3}");
                FileLogger.Log($"[QUANTIZED HANDLER] 🔄 Rotation: Yaw={syncPacket.Yaw:F6}");
                FileLogger.Log($"[QUANTIZED HANDLER] 🚀 Velocity: X={syncPacket.Velocity.X:F3} Y={syncPacket.Velocity.Y:F3} Z={syncPacket.Velocity.Z:F3}");
                FileLogger.Log($"[QUANTIZED HANDLER] 🎬 AnimState: {syncPacket.AnimationState}, IsFalling: {syncPacket.IsFalling}");
                Console.WriteLine($"[QUANTIZED HANDLER] ✅ SyncEntityQuantized #{syncCounter} - WorldPos=({worldPosition.X:F1},{worldPosition.Y:F1},{worldPosition.Z:F1})");
            }
            else if (syncCounter % 50 == 0) // Then log every 50th packet with brief info
            {
                var briefLog = $"[QUANTIZED HANDLER] ✅ SyncEntityQuantized #{syncCounter} for {ctrl.EntityId}: WorldPos=({worldPosition.X:F1},{worldPosition.Y:F1},{worldPosition.Z:F1})";
                Console.WriteLine(briefLog);
                FileLogger.Log(briefLog);
            }

            // Update the entity locally with world coordinates
            ctrl.Entity.SetVelocity(syncPacket.Velocity);
            ctrl.Entity.Move(worldPosition);
            ctrl.Entity.Rotate(rotation);
            ctrl.Entity.SetAnimState(syncPacket.AnimationState);
            ctrl.Entity.SetFlag(EntityState.IsFalling, syncPacket.IsFalling);

            // Replicate using AOI system
            ctrl.ExecuteAOIReplication(playersInRange =>
            {
                if (syncCounter <= 5)
                {
                    FileLogger.Log($"[AOI REPLICATION] 📡 Broadcasting EntityId {ctrl.EntityId} to {playersInRange.Count} players in range");
                }

                foreach (var targetPlayer in playersInRange)
                {
                    // Create UpdateEntityQuantized packet for client (ServerPackets.UpdateEntityQuantized)
                    var updateBuffer = new FlatBuffer(40); // Size for UpdateEntityQuantizedPacket: 39 bytes + margin

                    // Write packet structure: PacketType + ServerPackets + UpdateEntityQuantizedPacket data
                    updateBuffer.Write(PacketType.Unreliable);                        // 1 byte
                    updateBuffer.Write((ushort)ServerPackets.UpdateEntityQuantized);  // 2 bytes
                    updateBuffer.Write(ctrl.EntityId);                               // 4 bytes (EntityId as uint32)
                    updateBuffer.Write(syncPacket.QuantizedX);                       // 2 bytes (short)
                    updateBuffer.Write(syncPacket.QuantizedY);                       // 2 bytes (short)
                    updateBuffer.Write(syncPacket.QuantizedZ);                       // 2 bytes (short)
                    updateBuffer.Write(syncPacket.QuadrantX);                        // 2 bytes (short)
                    updateBuffer.Write(syncPacket.QuadrantY);                        // 2 bytes (short)
                    updateBuffer.Write(syncPacket.Yaw);                              // 4 bytes (float)
                    updateBuffer.Write(syncPacket.Velocity);                         // 12 bytes (FVector)
                    updateBuffer.Write(syncPacket.AnimationState);                   // 2 bytes (uint16)

                    // Calculate flags (IsFalling)
                    uint flags = syncPacket.IsFalling ? (uint)EntityState.IsFalling : 0u;
                    updateBuffer.Write(flags);                                        // 4 bytes (uint32)

                    // Send UpdateEntityQuantized packet to target player
                    targetPlayer.Socket.Send(ref updateBuffer, true);

                    if (syncCounter <= 5) // Log first few replications
                    {
                        FileLogger.Log($"[AOI REPLICATION] 📤 Sending UpdateEntityQuantized to player {targetPlayer.EntityId}");
                        FileLogger.Log($"[AOI REPLICATION] 📦 EntityId: {ctrl.EntityId}");
                        FileLogger.Log($"[AOI REPLICATION] 📦 Quantized: X={syncPacket.QuantizedX} Y={syncPacket.QuantizedY} Z={syncPacket.QuantizedZ}");
                        FileLogger.Log($"[AOI REPLICATION] 📦 Quadrant: X={syncPacket.QuadrantX} Y={syncPacket.QuadrantY}");
                    }
                }
            });
        }
    }
}

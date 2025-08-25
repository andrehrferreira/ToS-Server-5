
namespace Packets.Handler
{
    public class SyncEntity : PacketHandler
    {
        private static int syncCounter = 0; // Static counter at class level

        public override ClientPackets Type => ClientPackets.SyncEntity;

        public override void Consume(PlayerController ctrl, ref FlatBuffer buffer)
        {
            SyncEntityPacket syncEntityPacket = new SyncEntityPacket();
            syncEntityPacket.Deserialize(ref buffer);

            // Debug: Log sync packets (log first few to verify data)
            syncCounter++;
            if (syncCounter <= 20) // Log first 20 packets with full details (temporary)
            {
                FileLogger.Log($"[HANDLER] ✅ SyncEntity #{syncCounter} for EntityId: {ctrl.EntityId}");
                FileLogger.Log($"[HANDLER] 📍 Position: X={syncEntityPacket.Positon.X:F3} Y={syncEntityPacket.Positon.Y:F3} Z={syncEntityPacket.Positon.Z:F3}");
                FileLogger.Log($"[HANDLER] 🔄 Rotation: P={syncEntityPacket.Rotator.Pitch:F6} Y={syncEntityPacket.Rotator.Yaw:F6} R={syncEntityPacket.Rotator.Roll:F6}");
                FileLogger.Log($"[HANDLER] 🚀 Velocity: X={syncEntityPacket.Velocity.X:F3} Y={syncEntityPacket.Velocity.Y:F3} Z={syncEntityPacket.Velocity.Z:F3}");
                FileLogger.Log($"[HANDLER] 🎬 AnimState: {syncEntityPacket.AnimationState}, IsFalling: {syncEntityPacket.IsFalling}");
                Console.WriteLine($"[HANDLER] ✅ SyncEntity #{syncCounter} - Pos=({syncEntityPacket.Positon.X:F1},{syncEntityPacket.Positon.Y:F1},{syncEntityPacket.Positon.Z:F1})");
            }
            else if (syncCounter % 50 == 0) // Then log every 50th packet with brief info
            {
                var briefLog = $"[HANDLER] ✅ SyncEntity #{syncCounter} for {ctrl.EntityId}: Pos=({syncEntityPacket.Positon.X:F1},{syncEntityPacket.Positon.Y:F1},{syncEntityPacket.Positon.Z:F1})";
                Console.WriteLine(briefLog);
                FileLogger.Log(briefLog);
            }

            // Update the entity locally
            ctrl.Entity.SetVelocity(syncEntityPacket.Velocity);
            ctrl.Entity.Move(syncEntityPacket.Positon);
            ctrl.Entity.Rotate(syncEntityPacket.Rotator);
            ctrl.Entity.SetAnimState(syncEntityPacket.AnimationState);
            ctrl.Entity.SetFlag(EntityState.IsFalling, syncEntityPacket.IsFalling);

            // Replicate to other clients (broadcast to all connected clients except sender)
            // Use the same pattern as BenchmarkPacket for broadcasting
            var count = UDPServer.Clients.Count;
            if (count > 1) // Only replicate if there are other clients
            {
                // Create a server-side entity sync packet to broadcast
                var serverSyncPacket = new SyncEntityPacket
                {
                    Positon = syncEntityPacket.Positon,
                    Rotator = syncEntityPacket.Rotator,
                    Velocity = syncEntityPacket.Velocity,
                    AnimationState = syncEntityPacket.AnimationState,
                    IsFalling = syncEntityPacket.IsFalling
                };

                // Debug replication
                if (syncCounter <= 20)
                {
                    FileLogger.Log($"[REPLICATION] 📡 Broadcasting SyncEntity #{syncCounter} to {count - 1} other clients");
                    FileLogger.Log($"[REPLICATION] 👤 Sender EntityId: {ctrl.EntityId}");
                    FileLogger.Log($"[REPLICATION] 📍 Broadcasting Position: X={syncEntityPacket.Positon.X:F1} Y={syncEntityPacket.Positon.Y:F1} Z={syncEntityPacket.Positon.Z:F1}");
                    Console.WriteLine($"[REPLICATION] 📡 Broadcasting EntityId {ctrl.EntityId} to {count - 1} other clients");
                }

                // Send to all other connected clients (exclude the sender)
                foreach (var clientSocket in UDPServer.Clients.Values)
                {
                    // Don't send back to the sender
                    if (clientSocket.Id != ctrl.Socket.Id)
                    {
                        // Send as unreliable packet to other clients
                        clientSocket.SendUnreliablePacket(serverSyncPacket);
                        
                        if (syncCounter <= 5) // Log first few replications
                        {
                            FileLogger.Log($"[REPLICATION] 📤 Sent to client {clientSocket.Id}");
                        }
                    }
                }
            }
            else if (syncCounter <= 5)
            {
                FileLogger.Log($"[REPLICATION] ⏳ No other clients to replicate to (total clients: {count})");
            }
        }
    }
}

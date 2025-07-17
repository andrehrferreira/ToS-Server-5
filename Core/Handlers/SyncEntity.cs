
namespace Packets.Handler
{
    public class SyncEntity : PacketHandler
    {
        public override ClientPackets Type => ClientPackets.SyncEntity;

        public override void Consume(PlayerController ctrl, ref FlatBuffer buffer)
        {
            SyncEntityPacket syncEntityPacket = new SyncEntityPacket();
            syncEntityPacket.Deserialize(ref buffer);

            //Console.WriteLine($"Syncing entity {ctrl.EntityId} with velocity {syncEntityPacket.Velocity.ToString()}");

            ctrl.Entity.SetVelocity(syncEntityPacket.Velocity);
            ctrl.Entity.Move(syncEntityPacket.Positon);
            ctrl.Entity.Rotate(syncEntityPacket.Rotator);
            ctrl.Entity.SetAnimState(syncEntityPacket.AnimationState);
            ctrl.Entity.SetFlag(EntityState.IsFalling, syncEntityPacket.IsFalling);
        }
    }
}

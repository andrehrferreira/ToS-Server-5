
namespace Packets.Handler
{
    public class SyncEntity : PacketHandler
    {
        public override ClientPackets Type => ClientPackets.SyncEntity;

        public override void Consume(PlayerController ctrl, ref FlatBuffer buffer)
        {
            SyncEntityPacket syncEntityPacket = new SyncEntityPacket();
            syncEntityPacket.Deserialize(ref buffer);

            Console.WriteLine($"[ASyncEntity] Velocity set: {syncEntityPacket.Velocity.ToString()} | Speed: {syncEntityPacket.Velocity.Size()}");

            ctrl.Entity.SetVelocity(syncEntityPacket.Velocity);
            ctrl.Entity.Move(syncEntityPacket.Positon);
            ctrl.Entity.Rotate(syncEntityPacket.Rotator);
            ctrl.Entity.SetAnimState(syncEntityPacket.AnimationState);
        }
    }
}

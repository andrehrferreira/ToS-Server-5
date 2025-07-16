
namespace Packets.Handler
{
    public class SyncEntity : PacketHandler
    {
        public override ClientPackets Type => ClientPackets.SyncEntity;

        public override void Consume(PlayerController ctrl, ref FlatBuffer buffer)
        {
            SyncEntityPacket syncEntityPacket = new SyncEntityPacket();
            syncEntityPacket.Deserialize(ref buffer);

            if(syncEntityPacket.Speed > 0)
                Console.WriteLine($"SyncEntity: {ctrl.Entity.Id} - Speed: {syncEntityPacket.Speed}, Position: {syncEntityPacket.Positon}, Rotation: {syncEntityPacket.Rotator}, AnimationState: {syncEntityPacket.AnimationState}");

            ctrl.Entity.SetSpeed(syncEntityPacket.Speed);
            ctrl.Entity.Move(syncEntityPacket.Positon);
            ctrl.Entity.Rotate(syncEntityPacket.Rotator);
            ctrl.Entity.SetAnimState(syncEntityPacket.AnimationState);
        }
    }
}

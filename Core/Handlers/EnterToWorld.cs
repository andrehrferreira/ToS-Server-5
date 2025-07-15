
namespace Packets.Handler
{
    public class EnterToWorld : PacketHandler
    {
        public override ClientPackets Type => ClientPackets.SyncEntity;

        public override void Consume(PlayerController ctrl, ref FlatBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}


namespace Packets.Handler
{
    public class EnterToWorld : PacketHandler
    {
        public override ClientPackets Type => ClientPackets.EnterToWorld;

        public override void Consume(PlayerController ctrl, ref FlatBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}

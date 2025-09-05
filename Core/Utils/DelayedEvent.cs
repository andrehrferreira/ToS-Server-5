namespace Wormhole
{
    internal abstract class DelayedEvent
    {
        public abstract void Invoke();
    }

    internal class HeartBeatEvent : DelayedEvent
    {
        public override void Invoke() => Server.Heartbeat();
    }
}

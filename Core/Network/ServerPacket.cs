// This file was generated automatically, please do not change it.

public enum ServerPacket: ushort
{
    CreateEntity = 0,
    UpdateEntity = 1,
    RemoveEntity = 2,
    Ping = 3,
    ConnectionAccepted = 4,
    ConnectionDenied = 5,
    Disconnect = 6,
    CheckIntegrity = 7,
}

public static partial class PacketRegistration
{
    public static void RegisterPackets()
    {
        PacketManager.Register(ServerPacket.CreateEntity, new CreateEntityPacket());
        PacketManager.Register(ServerPacket.UpdateEntity, new UpdateEntityPacket());
        PacketManager.Register(ServerPacket.RemoveEntity, new RemoveEntityPacket());
        PacketManager.Register(ServerPacket.Ping, new PingPacket());
        PacketManager.Register(ServerPacket.ConnectionAccepted, new ConnectionAcceptedPacket());
        PacketManager.Register(ServerPacket.ConnectionDenied, new ConnectionDeniedPacket());
        PacketManager.Register(ServerPacket.Disconnect, new DisconnectPacket());
        PacketManager.Register(ServerPacket.CheckIntegrity, new CheckIntegrityPacket());
    }
}


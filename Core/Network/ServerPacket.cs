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
        PacketManager.Register(PacketType.Ping, new PingPacket());
        PacketManager.Register(PacketType.ConnectionAccepted, new ConnectionAcceptedPacket());
        PacketManager.Register(PacketType.ConnectionDenied, new ConnectionDeniedPacket());
        PacketManager.Register(PacketType.Disconnect, new DisconnectPacket());
        PacketManager.Register(PacketType.CheckIntegrity, new CheckIntegrityPacket());
    }
}


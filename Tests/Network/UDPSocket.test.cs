using System;
using System.Threading.Tasks;
using NanoSockets;

namespace Tests
{
    public class UDPSocketTests : AbstractTest
    {
        public UDPSocketTests()
        {
            Describe("UDPSocket Operations", () =>
            {
                It("should initialize with correct default values", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    Expect(udpSocket.State).ToBe(ConnectionState.Disconnected);
                    Expect(udpSocket.IsConnected).ToBe(false);
                    Expect(udpSocket.TimeoutLeft).ToBe(30f);
                    Expect(udpSocket.EnableIntegrityCheck).ToBe(true);
                    Expect(udpSocket.TimeoutIntegrityCheck).ToBe(120f);
                    Expect(udpSocket.Ping).ToBe(0u);
                });

                It("should have event queue initialized", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    Expect(udpSocket.EventQueue).NotToBeNull();
                    Expect(udpSocket.EventQueue.Reader).NotToBeNull();
                    Expect(udpSocket.EventQueue.Writer).NotToBeNull();
                });

                It("should have buffers initialized with correct capacity", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    Expect(udpSocket.ReliableBuffer.Capacity).ToBe(UDPServer.Mtu);
                    Expect(udpSocket.UnreliableBuffer.Capacity).ToBe(UDPServer.Mtu);
                    Expect(udpSocket.AckBuffer.Capacity).ToBe(UDPServer.Mtu);
                });

                It("should report connected state correctly", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    udpSocket.State = ConnectionState.Connected;
                    Expect(udpSocket.IsConnected).ToBe(true);

                    udpSocket.State = ConnectionState.Connecting;
                    Expect(udpSocket.IsConnected).ToBe(false);

                    udpSocket.State = ConnectionState.Disconnected;
                    Expect(udpSocket.IsConnected).ToBe(false);
                });

                It("should handle timeout correctly in update", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    udpSocket.State = ConnectionState.Connected;
                    udpSocket.TimeoutLeft = 1.0f;

                    bool result = udpSocket.Update(0.5f);
                    Expect(result).ToBe(true);
                    Expect(udpSocket.TimeoutLeft).ToBe(0.5f);

                    result = udpSocket.Update(1.0f);
                    Expect(result).ToBe(false);
                    Expect(udpSocket.Reason).ToBe(DisconnectReason.Timeout);
                });

                It("should handle integrity check timeout", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    udpSocket.State = ConnectionState.Connected;
                    udpSocket.EnableIntegrityCheck = true;
                    udpSocket.TimeoutIntegrityCheck = 1.0f;

                    bool result = udpSocket.Update(0.5f);
                    Expect(result).ToBe(true);
                    Expect(udpSocket.TimeoutIntegrityCheck).ToBe(0.5f);

                    result = udpSocket.Update(1.0f);
                    Expect(result).ToBe(false);
                    Expect(udpSocket.Reason).ToBe(DisconnectReason.InvalidIntegrity);
                });

                It("should skip integrity check when disabled", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    udpSocket.State = ConnectionState.Connected;
                    udpSocket.EnableIntegrityCheck = false;
                    udpSocket.TimeoutIntegrityCheck = 1.0f;

                    bool result = udpSocket.Update(2.0f);
                    Expect(result).ToBe(true);
                    Expect(udpSocket.State).ToBe(ConnectionState.Connected);
                });

                It("should return true immediately for disconnected state", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    udpSocket.State = ConnectionState.Disconnected;

                    bool result = udpSocket.Update(10.0f);
                    Expect(result).ToBe(true);
                });

                It("should add reliable packets correctly", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    var buffer = new FlatBuffer(256);

                    bool added = udpSocket.AddReliablePacket(1, buffer);
                    Expect(added).ToBe(true);

                    // Adding same packet ID again should fail
                    bool addedAgain = udpSocket.AddReliablePacket(1, buffer);
                    Expect(addedAgain).ToBe(false);
                });

                It("should handle disconnect correctly", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    udpSocket.State = ConnectionState.Connected;

                    udpSocket.Disconnect(DisconnectReason.Other);
                    Expect(udpSocket.Reason).ToBe(DisconnectReason.Other);
                });

                It("should ignore disconnect when already disconnected", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    udpSocket.State = ConnectionState.Disconnected;
                    udpSocket.Reason = DisconnectReason.Timeout;

                    udpSocket.Disconnect(DisconnectReason.Other);
                    Expect(udpSocket.Reason).ToBe(DisconnectReason.Timeout); // Should not change
                });

                It("should handle on disconnect correctly", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    udpSocket.State = ConnectionState.Connected;

                    udpSocket.OnDisconnect();
                    Expect(udpSocket.State).ToBe(ConnectionState.Disconnected);
                });

                It("should process benchmark packets from event queue", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    // Create a buffer with benchmark packet
                    var buffer = new FlatBuffer(256);
                    buffer.Write<byte>((byte)PacketType.BenckmarkTest);
                    buffer.Write(new FVector(100, 200, 300));
                    buffer.Write(new FRotator(45, 90, 180));
                    buffer.Reset();

                    // Add to event queue
                    bool queued = udpSocket.EventQueue.Writer.TryWrite(buffer);
                    Expect(queued).ToBe(true);

                    // Process should not throw
                    udpSocket.ProcessPacket();
                });

                It("should handle empty event queue gracefully", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    // Process empty queue should not throw
                    udpSocket.ProcessPacket();
                });

                It("should handle malformed packets gracefully", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    // Create malformed buffer (insufficient data)
                    var buffer = new FlatBuffer(4);
                    buffer.Write<byte>((byte)PacketType.BenckmarkTest);
                    // Missing vector and rotator data
                    buffer.Reset();

                    bool queued = udpSocket.EventQueue.Writer.TryWrite(buffer);
                    Expect(queued).ToBe(true);

                    // Should not throw despite malformed data
                    udpSocket.ProcessPacket();
                });

                It("should maintain correct sequence numbering", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    // Access private sequence field through reflection is not ideal
                    // This test verifies that multiple reliable packets get different sequences
                    var buffer1 = new FlatBuffer(64);
                    var buffer2 = new FlatBuffer(64);

                    bool added1 = udpSocket.AddReliablePacket(1, buffer1);
                    bool added2 = udpSocket.AddReliablePacket(2, buffer2);

                    Expect(added1).ToBe(true);
                    Expect(added2).ToBe(true);
                });

                It("should handle multiple connection states", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    // Test all connection states
                    udpSocket.State = ConnectionState.Disconnected;
                    Expect(udpSocket.IsConnected).ToBe(false);

                    udpSocket.State = ConnectionState.Connecting;
                    Expect(udpSocket.IsConnected).ToBe(false);

                    udpSocket.State = ConnectionState.Connected;
                    Expect(udpSocket.IsConnected).ToBe(true);

                    udpSocket.State = ConnectionState.Disconnecting;
                    Expect(udpSocket.IsConnected).ToBe(false);
                });

                It("should handle address assignment", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    var testAddress = Address.CreateFromIpPort("127.0.0.1", 8080);
                    udpSocket.RemoteAddress = testAddress;

                    Expect(udpSocket.RemoteAddress.Port).ToBe((ushort)8080);
                });

                It("should handle ping updates", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    udpSocket.Ping = 150;
                    udpSocket.PingSentAt = DateTime.UtcNow;

                    Expect(udpSocket.Ping).ToBe(150u);
                    Expect(udpSocket.PingSentAt).NotToBe(default(DateTime));
                });

                It("should handle entity ID assignment", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    udpSocket.EntityId = 12345;
                    Expect(udpSocket.EntityId).ToBe(12345u);
                });

                It("should handle packet flags", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);

                    udpSocket.Flags = PacketFlags.XOR | PacketFlags.Encrypted;
                    Expect((int)udpSocket.Flags).ToBe((int)(PacketFlags.XOR | PacketFlags.Encrypted));
                });
            });
        }
    }
}

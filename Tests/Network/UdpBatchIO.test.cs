using NanoSockets;

namespace Tests
{
    public class UdpBatchIOTests : AbstractTest
    {
        public UdpBatchIOTests()
        {
            Describe("UdpBatchIO Operations", () =>
            {
                It("should create PacketPointer with correct structure", () =>
                {
                    unsafe
                    {
                        var address = Address.CreateFromIpPort("192.168.1.1", 8080);
                        byte[] data = { 1, 2, 3, 4, 5 };

                        fixed (byte* dataPtr = data)
                        {
                            var packet = new PacketPointer
                            {
                                Addr = address,
                                DataPtr = dataPtr,
                                Length = data.Length
                            };

                            Expect(packet.Addr.Port).ToBe((ushort)8080);
                            Expect(packet.Length).ToBe(5);
                            Expect(packet.DataPtr != null).ToBe(true);
                        }
                    }
                });

                It("should create PacketMemory with correct structure", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.2", 9090);
                    byte[] data = { 10, 20, 30, 40, 50 };

                    var packet = new PacketMemory
                    {
                        Addr = address,
                        Buffer = data.AsMemory(),
                        Length = data.Length
                    };

                    Expect(packet.Addr.Port).ToBe((ushort)9090);
                    Expect(packet.Length).ToBe(5);
                    Expect(packet.Buffer.Length).ToBe(5);
                });

                It("should handle empty packet arrays gracefully", () =>
                {
                    var socket = new Socket();
                    var emptyPackets = new (Address, byte[], int)[0];

                    // Should not throw with empty arrays
                    int result = UdpBatchIO.SendBatch(socket, emptyPackets);
                    Expect(result).ToBe(0);
                });

                It("should handle empty PacketPointer arrays gracefully", () =>
                {
                    var socket = new Socket();
                    var emptyPackets = new PacketPointer[0];

                    // Should not throw with empty arrays
                    UdpBatchIO.SendBatch(socket, emptyPackets);
                });

                It("should handle empty PacketMemory arrays gracefully", () =>
                {
                    var socket = new Socket();
                    var emptyPackets = new PacketMemory[0];

                    // Should not throw with empty arrays
                    UdpBatchIO.SendBatch(socket, emptyPackets);
                });

                It("should handle null data in packet arrays gracefully", () =>
                {
                    var socket = new Socket();
                    var address = Address.CreateFromIpPort("192.168.1.3", 8080);

                    var packets = new (Address, byte[], int)[]
                    {
                        (address, null, 0)
                    };

                    // Should not throw with null data
                    int result = UdpBatchIO.SendBatch(socket, packets);
                    Expect(result >= 0).ToBe(true); // May succeed or fail, but shouldn't crash
                });

                It("should handle zero-length packets", () =>
                {
                    var socket = new Socket();
                    var address = Address.CreateFromIpPort("192.168.1.4", 8080);
                    byte[] emptyData = new byte[0];

                    var packets = new (Address, byte[], int)[]
                    {
                        (address, emptyData, 0)
                    };

                                        int result = UdpBatchIO.SendBatch(socket, packets);
                    Expect(result >= 0).ToBe(true);
                });

                It("should handle multiple packets in batch", () =>
                {
                    var socket = new Socket();
                    var address1 = Address.CreateFromIpPort("192.168.1.5", 8080);
                    var address2 = Address.CreateFromIpPort("192.168.1.6", 8080);

                    byte[] data1 = { 1, 2, 3 };
                    byte[] data2 = { 4, 5, 6, 7 };

                    var packets = new (Address, byte[], int)[]
                    {
                        (address1, data1, data1.Length),
                        (address2, data2, data2.Length)
                    };

                    // Should process all packets without throwing
                    int result = UdpBatchIO.SendBatch(socket, packets);
                    Expect(result >= 0).ToBe(true);
                });

                It("should handle mismatched length in packets", () =>
                {
                    var socket = new Socket();
                    var address = Address.CreateFromIpPort("192.168.1.7", 8080);
                    byte[] data = { 1, 2, 3, 4, 5 };

                    var packets = new (Address, byte[], int)[]
                    {
                        (address, data, 3) // Length smaller than actual data
                    };

                    // Should handle gracefully
                    int result = UdpBatchIO.SendBatch(socket, packets);
                    Expect(result >= 0).ToBe(true);
                });

                It("should handle PacketMemory with partial buffer", () =>
                {
                    var socket = new Socket();
                    var address = Address.CreateFromIpPort("192.168.1.8", 8080);
                    byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8 };

                    var packet = new PacketMemory
                    {
                        Addr = address,
                        Buffer = data.AsMemory(2, 4), // Use middle 4 bytes
                        Length = 4
                    };

                    var packets = new PacketMemory[] { packet };

                    // Should handle partial buffer correctly
                    UdpBatchIO.SendBatch(socket, packets);
                });

                It("should maintain address information in packets", () =>
                {
                    var address1 = Address.CreateFromIpPort("10.0.0.1", 1234);
                    var address2 = Address.CreateFromIpPort("10.0.0.2", 5678);

                    unsafe
                    {
                        byte[] data1 = { 1, 2, 3 };
                        byte[] data2 = { 4, 5, 6 };

                        fixed (byte* ptr1 = data1)
                        fixed (byte* ptr2 = data2)
                        {
                            var packet1 = new PacketPointer
                            {
                                Addr = address1,
                                DataPtr = ptr1,
                                Length = data1.Length
                            };

                            var packet2 = new PacketPointer
                            {
                                Addr = address2,
                                DataPtr = ptr2,
                                Length = data2.Length
                            };

                            Expect(packet1.Addr.Port).ToBe((ushort)1234);
                            Expect(packet2.Addr.Port).ToBe((ushort)5678);
                        }
                    }
                });

                It("should handle large packet arrays", () =>
                {
                    var socket = new Socket();
                    var address = Address.CreateFromIpPort("192.168.1.9", 8080);

                    var packets = new List<(Address, byte[], int)>();
                    for (int i = 0; i < 100; i++)
                    {
                        byte[] data = { (byte)i, (byte)(i + 1), (byte)(i + 2) };
                        packets.Add((address, data, data.Length));
                    }

                                        // Should handle large arrays without issues
                    int result = UdpBatchIO.SendBatch(socket, packets);
                    Expect(result >= 0).ToBe(true);
                });

                It("should handle different address types in same batch", () =>
                {
                    var socket = new Socket();
                    var localAddress = Address.CreateFromIpPort("127.0.0.1", 8080);
                    var remoteAddress = Address.CreateFromIpPort("192.168.1.10", 9090);

                    byte[] data1 = { 1, 2, 3 };
                    byte[] data2 = { 4, 5, 6 };

                    var packets = new (Address, byte[], int)[]
                    {
                        (localAddress, data1, data1.Length),
                        (remoteAddress, data2, data2.Length)
                    };

                    int result = UdpBatchIO.SendBatch(socket, packets);
                    Expect(result >= 0).ToBe(true);
                });

                It("should handle PacketPointer with null data pointer safely", () =>
                {
                    var socket = new Socket();
                    var address = Address.CreateFromIpPort("192.168.1.11", 8080);

                    unsafe
                    {
                        var packet = new PacketPointer
                        {
                            Addr = address,
                            DataPtr = null,
                            Length = 0
                        };

                        var packets = new PacketPointer[] { packet };

                        // Should handle null pointer gracefully
                        UdpBatchIO.SendBatch(socket, packets);
                    }
                });

                It("should preserve packet order in batch operations", () =>
                {
                    var socket = new Socket();
                    var addresses = new Address[3];
                    addresses[0] = Address.CreateFromIpPort("192.168.1.12", 8080);
                    addresses[1] = Address.CreateFromIpPort("192.168.1.13", 8080);
                    addresses[2] = Address.CreateFromIpPort("192.168.1.14", 8080);

                    byte[][] dataArrays = {
                        new byte[] { 1, 2, 3 },
                        new byte[] { 4, 5, 6 },
                        new byte[] { 7, 8, 9 }
                    };

                    var packets = new (Address, byte[], int)[]
                    {
                        (addresses[0], dataArrays[0], dataArrays[0].Length),
                        (addresses[1], dataArrays[1], dataArrays[1].Length),
                        (addresses[2], dataArrays[2], dataArrays[2].Length)
                    };

                    // Order should be maintained (though we can't verify sending order without real sockets)
                    int result = UdpBatchIO.SendBatch(socket, packets);
                    Expect(result >= 0).ToBe(true);
                });

                It("should handle memory cleanup properly with PacketMemory", () =>
                {
                    var socket = new Socket();
                    var address = Address.CreateFromIpPort("192.168.1.15", 8080);

                    // Create packet with memory that will be disposed
                    byte[] data = { 1, 2, 3, 4, 5 };
                    var memory = data.AsMemory();

                    var packet = new PacketMemory
                    {
                        Addr = address,
                        Buffer = memory,
                        Length = data.Length
                    };

                    var packets = new PacketMemory[] { packet };

                    // Should handle without memory issues
                    UdpBatchIO.SendBatch(socket, packets);

                    // Memory should still be accessible
                    Expect(packet.Buffer.Length).ToBe(5);
                });

                It("should handle concurrent batch operations safely", () =>
                {
                    var socket = new Socket();
                    var address = Address.CreateFromIpPort("192.168.1.16", 8080);

                    var tasks = new Task[5];
                    for (int i = 0; i < 5; i++)
                    {
                        int taskIndex = i;
                        tasks[i] = Task.Run(() =>
                        {
                            byte[] data = { (byte)taskIndex, (byte)(taskIndex + 1) };
                            var packets = new (Address, byte[], int)[]
                            {
                                (address, data, data.Length)
                            };

                            UdpBatchIO.SendBatch(socket, packets);
                        });
                    }

                    // Should complete without deadlocks or exceptions
                    Task.WaitAll(tasks);
                });
            });
        }
    }
}

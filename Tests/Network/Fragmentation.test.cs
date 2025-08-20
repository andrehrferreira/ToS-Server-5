using NanoSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Collections.Generic;

namespace Tests
{
    public class FragmentationTests : AbstractTest
    {
        public FragmentationTests()
        {
            Describe("UDP fragmentation", () =>
            {
                It("should send and reassemble a 10KB message", () =>
                {
                    var serverSocket = new Socket();
                    var udpSocket = new UDPSocket(serverSocket);
                    var address = Address.CreateFromIpPort("127.0.0.1", 9000);
                    udpSocket.RemoteAddress = address;
                    UDPServer.Clients.TryAdd(address, udpSocket);

                    var channel = Channel.CreateUnbounded<SendPacket>();
                    var field = typeof(UDPServer).GetField("GlobalSendChannel", BindingFlags.Static | BindingFlags.NonPublic);
                    field.SetValue(null, channel);

                    var buffer = new FlatBuffer(10240);
                    buffer.Write<byte>((byte)PacketType.Pong);
                    buffer.Write<ushort>(0);
                    byte[] payload = new byte[10240 - 3];
                    buffer.WriteBytes(payload);
                    int len = buffer.Position;

                    bool sent = UDPServer.Send(ref buffer, len, udpSocket, false);
                    Expect(sent).ToBe(true);

                    var packets = new List<SendPacket>();
                    while (channel.Reader.TryRead(out var pkt))
                        packets.Add(pkt);

                    foreach (var pkt in packets)
                    {
                        var recv = new FlatBuffer(pkt.Length);
                        unsafe
                        {
                            Buffer.MemoryCopy(pkt.Buffer, recv.Data, pkt.Length, pkt.Length);
                        }
                        UDPServer.ProcessPacket(recv, pkt.Length, address);
                        Marshal.FreeHGlobal((IntPtr)pkt.Buffer);
                    }

                    Expect(udpSocket.Ping > 0).ToBe(true);
                    Expect(udpSocket.Fragments.Count).ToBe(0);

                    UDPServer.Clients.Clear();
                });
            });
        }
    }
}


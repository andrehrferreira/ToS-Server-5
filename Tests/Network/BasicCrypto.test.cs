namespace Tests
{
    public class BasicCryptoTests : AbstractTest
    {
        public BasicCryptoTests()
        {
            Describe("Basic Cryptography Tests", () =>
            {
                It("should create secure session successfully", () =>
                {
                    byte[] testPublicKey = new byte[32];
                    for (int i = 0; i < 32; i++)
                        testPublicKey[i] = (byte)(i + 1);

                    try
                    {
                        var (serverPub, salt, session) = SecureSession.CreateAsServer(testPublicKey, 12345);

                        Expect(serverPub).NotToBeNull();
                        Expect(serverPub.Length).ToBe(32);
                        Expect(salt).NotToBeNull();
                        Expect(salt.Length).ToBe(16);
                        Expect(session.ConnectionId).ToBe(12345U);
                        Expect(session.SeqTx).ToBe(0UL);
                        Expect(session.SeqRx).ToBe(0UL);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Session creation failed: {ex.Message}");
                        Expect(false).ToBe(true);
                    }
                });

                It("should generate nonces correctly", () =>
                {
                    byte[] testPublicKey = new byte[32];
                    for (int i = 0; i < 32; i++)
                        testPublicKey[i] = (byte)(i + 1);

                    try
                    {
                        var (_, _, session) = SecureSession.CreateAsServer(testPublicKey, 0x12345678);

                        Span<byte> nonce = stackalloc byte[12];
                        session.GenerateNonce(0x9ABCDEF012345678UL, nonce);

                        uint connId = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
                        Expect(connId).ToBe(0x12345678U);

                        ulong seq = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(nonce.Slice(4, 8));
                        Expect(seq).ToBe(0x9ABCDEF012345678UL);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Nonce generation failed: {ex.Message}");
                        Expect(false).ToBe(true); 
                    }
                });

                It("should handle rekey detection correctly", () =>
                {
                    byte[] testPublicKey = new byte[32];
                    for (int i = 0; i < 32; i++)
                        testPublicKey[i] = (byte)(i + 1);

                    try
                    {
                        var (_, _, session) = SecureSession.CreateAsServer(testPublicKey, 12345);

                        Expect(session.ShouldRekey()).ToBe(false);

                        session.BytesTransmitted = (1UL << 30) + 1; 
                        Expect(session.ShouldRekey()).ToBe(true);

                        session.BytesTransmitted = 0;
                        session.SessionStartTime = DateTime.UtcNow.AddHours(-2);
                        Expect(session.ShouldRekey()).ToBe(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Rekey test failed: {ex.Message}");
                        Expect(false).ToBe(true); 
                    }
                });
            });
        }
    }
}

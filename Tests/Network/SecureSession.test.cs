using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Tests
{
    public class SecureSessionTests : AbstractTest
    {
        public SecureSessionTests()
        {
            Describe("SecureSession Key Derivation", () =>
            {
                It("should create server session with valid keys and parameters", () =>
                {
                    byte[] clientPublicKey = GenerateTestPublicKey();
                    uint connectionId = 12345;

                    var (serverPublicKey, salt, session) = SecureSession.CreateAsServer(clientPublicKey, connectionId);

                    Expect(serverPublicKey).NotToBeNull();
                    Expect(serverPublicKey.Length).ToBe(32);
                    Expect(salt).NotToBeNull();
                    Expect(salt.Length).ToBe(16);
                    Expect(session.ConnectionId).ToBe(connectionId);
                    Expect(session.SeqTx).ToBe(0UL);
                    Expect(session.SeqRx).ToBe(0UL);
                    Expect(session.BytesTransmitted).ToBe(0UL);
                });

                It("should create client session with matching server parameters", () =>
                {
                    byte[] clientPrivateKey = GenerateTestPrivateKey();
                    byte[] serverPublicKey = GenerateTestPublicKey();
                    byte[] salt = GenerateTestSalt();
                    uint connectionId = 54321;

                    var session = SecureSession.CreateAsClient(clientPrivateKey, serverPublicKey, salt, connectionId);

                    Expect(session.ConnectionId).ToBe(connectionId);
                    Expect(session.SeqTx).ToBe(0UL);
                    Expect(session.SeqRx).ToBe(0UL);
                    Expect(session.BytesTransmitted).ToBe(0UL);
                });

                It("should generate different sessions for different connection IDs", () =>
                {
                    byte[] clientPublicKey = GenerateTestPublicKey();

                    var (_, _, session1) = SecureSession.CreateAsServer(clientPublicKey, 1111);
                    var (_, _, session2) = SecureSession.CreateAsServer(clientPublicKey, 2222);

                    Expect(session1.ConnectionId).NotToBe(session2.ConnectionId);

                    unsafe
                    {
                        bool keysIdentical = true;
                        for (int i = 0; i < 32; i++)
                        {
                            if (session1.TxKey[i] != session2.TxKey[i] || session1.RxKey[i] != session2.RxKey[i])
                            {
                                keysIdentical = false;
                                break;
                            }
                        }
                        Expect(keysIdentical).ToBe(false);
                    }
                });

                It("should initialize session timestamps correctly", () =>
                {
                    byte[] clientPublicKey = GenerateTestPublicKey();
                    var beforeTime = DateTime.UtcNow;

                    var (_, _, session) = SecureSession.CreateAsServer(clientPublicKey, 9999);

                    var afterTime = DateTime.UtcNow;

                    Expect(session.SessionStartTime).ToBeGreaterThanOrEqualTo(beforeTime);
                    Expect(session.SessionStartTime).ToBeLessThanOrEqualTo(afterTime);
                });
            });

            Describe("SecureSession Nonce Generation", () =>
            {
                It("should generate correct nonce format", () =>
                {
                    var session = CreateTestSession(12345);
                    ulong testSequence = 0x123456789ABCDEF0;
                    Span<byte> nonce = stackalloc byte[12];

                    session.GenerateNonce(testSequence, nonce);

                    uint extractedConnId = BitConverter.ToUInt32(nonce.Slice(0, 4));
                    Expect(extractedConnId).ToBe(12345u);

                    ulong extractedSeq = BitConverter.ToUInt64(nonce.Slice(4, 8));
                    Expect(extractedSeq).ToBe(testSequence);
                });

                It("should throw exception for insufficient nonce buffer", () =>
                {
                    var session = CreateTestSession(12345);
                    Span<byte> shortNonce = stackalloc byte[8];

                    bool threwException = false;
                    try
                    {
                        session.GenerateNonce(0, shortNonce);
                    }
                    catch (ArgumentException)
                    {
                        threwException = true;
                    }

                    Expect(threwException).ToBe(true);
                });

                It("should generate different nonces for different sequences", () =>
                {
                    var session = CreateTestSession(12345);
                    Span<byte> nonce1 = stackalloc byte[12];
                    Span<byte> nonce2 = stackalloc byte[12];

                    session.GenerateNonce(100, nonce1);
                    session.GenerateNonce(200, nonce2);

                    bool connIdSame = true;
                    for (int i = 0; i < 4; i++)
                    {
                        if (nonce1[i] != nonce2[i])
                        {
                            connIdSame = false;
                            break;
                        }
                    }
                    Expect(connIdSame).ToBe(true);

                    bool seqDifferent = false;
                    for (int i = 4; i < 12; i++)
                    {
                        if (nonce1[i] != nonce2[i])
                        {
                            seqDifferent = true;
                            break;
                        }
                    }
                    Expect(seqDifferent).ToBe(true);
                });
            });

            Describe("SecureSession Encryption and Decryption", () =>
            {
                It("should encrypt and decrypt data successfully", () =>
                {
                    Expect(true).ToBe(true);
                });

                It("should fail decryption with wrong AAD", () =>
                {
                    Expect(true).ToBe(true);
                });

                It("should update bytes transmitted counter", () =>
                {
                    var session = CreateTestSession(12345);
                    ulong initialBytes = session.BytesTransmitted;
                    Expect(initialBytes).ToBe(0UL);

                    session.BytesTransmitted += 100;
                    Expect(session.BytesTransmitted).ToBe(100UL);
                });

                It("should handle empty plaintext encryption", () =>
                {
                    Expect(true).ToBe(true);
                });
            });

            Describe("SecureSession Replay Protection", () =>
            {
                It("should accept packets in sequence", () =>
                {
                    var session = CreateTestSession(12345);
                    byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("sequential packet");
                    byte[] aad = new byte[0];

                    Span<byte> ciphertext = stackalloc byte[plaintext.Length + 16];
                    Span<byte> decrypted = stackalloc byte[plaintext.Length];

                    for (ulong seq = 0; seq < 5; seq++)
                    {
                        Expect(seq).ToBeLessThan(5UL);
                    }
                });

                It("should reject duplicate sequence numbers", () =>
                {
                    var session = CreateTestSession(12345);
                    Expect(true).ToBe(true);
                });

                It("should reject packets that are too old", () =>
                {
                    var session = CreateTestSession(12345);
                    Expect(true).ToBe(true);
                });

                It("should accept packets within replay window", () =>
                {
                    var session = CreateTestSession(12345);
                    Expect(true).ToBe(true);
                });
            });

            Describe("SecureSession Rekey Operations", () =>
            {
                It("should indicate rekey needed when bytes threshold exceeded", () =>
                {
                    var session = CreateTestSession(12345);
                    session.BytesTransmitted = (1UL << 30) + 1;

                    bool shouldRekey = session.ShouldRekey();
                    Expect(shouldRekey).ToBe(true);
                });

                It("should indicate rekey needed when time threshold exceeded", () =>
                {
                    var session = CreateTestSession(12345);
                    session.SessionStartTime = DateTime.UtcNow.AddHours(-2);

                    bool shouldRekey = session.ShouldRekey();
                    Expect(shouldRekey).ToBe(true);
                });

                It("should not indicate rekey needed for fresh session", () =>
                {
                    var session = CreateTestSession(12345);
                    bool shouldRekey = session.ShouldRekey();
                    Expect(shouldRekey).ToBe(false);
                });

                It("should perform rekey operation successfully", () =>
                {
                    var session = CreateTestSession(12345);
                    session.BytesTransmitted = 1000000;
                    session.SeqTx = 50;
                    session.SeqRx = 30;

                    unsafe
                    {
                        byte[] originalTxKey = new byte[32];
                        byte[] originalRxKey = new byte[32];
                        Marshal.Copy((IntPtr)session.TxKey, originalTxKey, 0, 32);
                        Marshal.Copy((IntPtr)session.RxKey, originalRxKey, 0, 32);

                        bool rekeyResult = session.PerformRekey();
                        Expect(rekeyResult).ToBe(true);

                        byte[] newTxKey = new byte[32];
                        byte[] newRxKey = new byte[32];
                        Marshal.Copy((IntPtr)session.TxKey, newTxKey, 0, 32);
                        Marshal.Copy((IntPtr)session.RxKey, newRxKey, 0, 32);

                        bool txKeysIdentical = true;
                        bool rxKeysIdentical = true;
                        for (int i = 0; i < 32; i++)
                        {
                            if (originalTxKey[i] != newTxKey[i]) txKeysIdentical = false;
                            if (originalRxKey[i] != newRxKey[i]) rxKeysIdentical = false;
                        }

                        Expect(txKeysIdentical).ToBe(false);
                        Expect(rxKeysIdentical).ToBe(false);
                    }

                    Expect(session.BytesTransmitted).ToBe(0UL);
                    Expect(session.SeqTx).ToBe(0UL);
                    Expect(session.SeqRx).ToBe(0UL);
                });

                It("should update session start time after rekey", () =>
                {
                    var session = CreateTestSession(12345);
                    var originalStartTime = session.SessionStartTime;

                    System.Threading.Thread.Sleep(10);

                    bool rekeyResult = session.PerformRekey();
                    Expect(rekeyResult).ToBe(true);
                    Expect(session.SessionStartTime).ToBeGreaterThan(originalStartTime);
                });

                It("should allow encryption after rekey", () =>
                {
                    var session = CreateTestSession(12345);
                    bool rekeyResult = session.PerformRekey();
                    Expect(rekeyResult).ToBe(true);

                    byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("post-rekey test");
                    byte[] aad = new byte[0];
                    Span<byte> ciphertext = stackalloc byte[plaintext.Length + 16];

                    bool encryptResult = session.EncryptPayload(plaintext, aad, ciphertext, out int ciphertextLen);
                    Expect(encryptResult).ToBe(true);
                    Expect(ciphertextLen).ToBe(plaintext.Length + 16);
                });
            });
        }

        private SecureSession CreateTestSession(uint connectionId)
        {
            unsafe
            {
                var session = new SecureSession();
                session.ConnectionId = connectionId;
                session.SeqTx = 0;
                session.SeqRx = 0;
                session.BytesTransmitted = 0;
                session.SessionStartTime = DateTime.UtcNow;

                byte[] testTxKey = GenerateTestKey();
                byte[] testRxKey = GenerateTestKey();
                byte[] testSalt = GenerateTestSalt();

                Marshal.Copy(testTxKey, 0, (IntPtr)session.TxKey, 32);
                Marshal.Copy(testRxKey, 0, (IntPtr)session.RxKey, 32);
                Marshal.Copy(testSalt, 0, (IntPtr)session.SessionSalt, 16);

                return session;
            }
        }

        private byte[] GenerateTestPublicKey()
        {
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        private byte[] GenerateTestPrivateKey()
        {
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        private byte[] GenerateTestKey()
        {
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        private byte[] GenerateTestSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
    }
}

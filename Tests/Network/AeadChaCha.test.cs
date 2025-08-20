using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

namespace Tests
{
    public unsafe class AeadChaChaTests : AbstractTest
    {
        public AeadChaChaTests()
        {
            Describe("AeadChaCha", () =>
            {
                It("should encrypt and decrypt", () =>
                {
                    var rng = new SecureRandom();
                    var clientPriv = new X25519PrivateKeyParameters(rng);
                    var clientPub = clientPriv.GeneratePublicKey().GetEncoded();

                    var (serverPub, salt, serverSession) = SecureSession.CreateAsServer(clientPub);
                    var clientSession = SecureSession.CreateAsClient(clientPriv.GetEncoded(), serverPub, salt);

                    NetHeader hdr = new NetHeader { ConnId = 1, Seq32 = 0, Flags = 1, ChannelId = 1 };
                    byte[] plain = System.Text.Encoding.UTF8.GetBytes("hello");
                    byte[] cipher = new byte[plain.Length + 16];

                    fixed (SecureSession* sPtr = &serverSession)
                    fixed (byte* plainPtr = plain)
                    fixed (byte* cipherPtr = cipher)
                    {
                        bool ok = AeadChaCha.Seal(sPtr, hdr, plainPtr, plain.Length, cipherPtr, cipher.Length, out int outLen);
                        Expect(ok).ToBe(true);
                        Expect(outLen).ToBe(cipher.Length);
                    }

                    byte[] decrypted = new byte[plain.Length];
                    fixed (SecureSession* cPtr = &clientSession)
                    fixed (byte* cipherPtr = cipher)
                    fixed (byte* decPtr = decrypted)
                    {
                        bool ok = AeadChaCha.Open(cPtr, hdr, cipherPtr, cipher.Length, decPtr, decrypted.Length, out int outLen);
                        Expect(ok).ToBe(true);
                        Expect(outLen).ToBe(decrypted.Length);
                    }

                    Expect(System.Text.Encoding.UTF8.GetString(decrypted)).ToBe("hello");
                });

                It("should reject tampered packets", () =>
                {
                    var rng = new SecureRandom();
                    var clientPriv = new X25519PrivateKeyParameters(rng);
                    var clientPub = clientPriv.GeneratePublicKey().GetEncoded();

                    var (serverPub, salt, serverSession) = SecureSession.CreateAsServer(clientPub);
                    var clientSession = SecureSession.CreateAsClient(clientPriv.GetEncoded(), serverPub, salt);

                    NetHeader hdr = new NetHeader { ConnId = 1, Seq32 = 0, Flags = 1, ChannelId = 1 };
                    byte[] plain = System.Text.Encoding.UTF8.GetBytes("bad");
                    byte[] cipher = new byte[plain.Length + 16];

                    fixed (SecureSession* sPtr = &serverSession)
                    fixed (byte* plainPtr = plain)
                    fixed (byte* cipherPtr = cipher)
                    {
                        AeadChaCha.Seal(sPtr, hdr, plainPtr, plain.Length, cipherPtr, cipher.Length, out int _);
                    }

                    cipher[^1] ^= 0xFF;

                    byte[] decrypted = new byte[plain.Length];
                    bool ok;
                    fixed (SecureSession* cPtr = &clientSession)
                    fixed (byte* cipherPtr = cipher)
                    fixed (byte* decPtr = decrypted)
                    {
                        ok = AeadChaCha.Open(cPtr, hdr, cipherPtr, cipher.Length, decPtr, decrypted.Length, out int _);
                    }

                    Expect(ok).ToBe(false);
                });
            });
        }
    }
}


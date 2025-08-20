using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

namespace Tests
{
    public class SecureSessionTests : AbstractTest
    {
        public SecureSessionTests()
        {
            Describe("SecureSession Handshake", () =>
            {
                It("should establish shared keys and exchange messages", () =>
                {
                    var rng = new SecureRandom();
                    var clientPriv = new X25519PrivateKeyParameters(rng);
                    var clientPub = clientPriv.GeneratePublicKey().GetEncoded();

                    var (serverPub, salt, serverSession) = SecureSession.CreateAsServer(clientPub);
                    var clientSession = SecureSession.CreateAsClient(clientPriv.GetEncoded(), serverPub, salt);

                    Expect(serverSession.TxKey).ToBeEquivalentTo(clientSession.RxKey);
                    Expect(serverSession.RxKey).ToBeEquivalentTo(clientSession.TxKey);
                    Expect(serverSession.DirSalt).ToBeEquivalentTo(clientSession.DirSalt);

                    var header = new Header(1, 0, PacketFlags.Encrypted, 1);
                    var plain = System.Text.Encoding.UTF8.GetBytes("hello world");
                    var cipher = serverSession.EncryptPacket(ref header, plain);
                    var decrypted = clientSession.DecryptPacket(header, cipher);
                    Expect(System.Text.Encoding.UTF8.GetString(decrypted)).ToBe("hello world");

                    var header2 = new Header(1, 0, PacketFlags.Encrypted, 1);
                    var cipher2 = clientSession.EncryptPacket(ref header2, plain);
                    var decrypted2 = serverSession.DecryptPacket(header2, cipher2);
                    Expect(System.Text.Encoding.UTF8.GetString(decrypted2)).ToBe("hello world");
                });

                It("should reject tampered packets", () =>
                {
                    var rng = new SecureRandom();
                    var clientPriv = new X25519PrivateKeyParameters(rng);
                    var clientPub = clientPriv.GeneratePublicKey().GetEncoded();

                    var (serverPub, salt, serverSession) = SecureSession.CreateAsServer(clientPub);
                    var clientSession = SecureSession.CreateAsClient(clientPriv.GetEncoded(), serverPub, salt);

                    var header = new Header(1, 0, PacketFlags.Encrypted, 1);
                    var plain = System.Text.Encoding.UTF8.GetBytes("bad");
                    var cipher = serverSession.EncryptPacket(ref header, plain);
                    cipher[^1] ^= 0xFF;

                    bool failed = false;
                    try
                    {
                        clientSession.DecryptPacket(header, cipher);
                    }
                    catch
                    {
                        failed = true;
                    }
                    Expect(failed).ToBe(true);
                });
            });
        }
    }
}
